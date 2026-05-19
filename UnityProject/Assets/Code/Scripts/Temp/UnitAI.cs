using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// Temp 单位 AI。
/// 服务器负责移动、索敌、攻击。
/// 客户端只显示同步后的结果。
/// </summary>
public class UnitAI : NetworkBehaviour
{
    public enum OrderMode
    {
        Auto,
        Move,
        AttackUnit,
        AttackRadioTower,
        AttackBuilding
    }

    [Header("阵营")]
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color teamColor;

    [Header("选择反馈 - 仅本地显示")]
    public GameObject selectionIndicator;

    [Header("组件")]
    public NetworkHealth health;
    public NetworkVisionSource visionSource;

    [Header("移动")]
    public float moveStopDistance = 0.6f;
    public float manualMoveMaxSeconds = 8f;

    [Header("战斗")]
    public float attackRange = 3f;
    public float attackCooldown = 1f;
    public float attackDamageToUnit = 25f;
    public float attackDamageToRadioTower = 25f;
    public float attackDamageToBuilding = 25f;

    [Header("视野")]
    public float fallbackVisionRadius = 12f;

    [Header("通信延迟")]
    public float damagedTowerDelayThreshold = 0.5f;
    public float delayedCommandSeconds = 3f;

    private NavMeshAgent agent;
    private float lastAttackTime;

    private OrderMode currentOrder = OrderMode.Auto;
    private Vector3 manualMoveDestination;
    private UnitAI manualUnitTarget;
    private NetworkedRadioTower manualRadioTowerTarget;
    private BuildingControl manualBuildingTarget;
    private float manualOrderStartTime;

    private Coroutine pendingOrderCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<NetworkHealth>();

        if (visionSource == null)
            visionSource = GetComponent<NetworkVisionSource>();

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (visionSource != null)
            visionSource.ServerSetTeam(teamColor);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isServer && agent != null)
            agent.enabled = false;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    [Server]
    public void ServerSetTeam(Color color)
    {
        teamColor = color;

        if (visionSource != null)
            visionSource.ServerSetTeam(color);

        RpcApplyColor(color);
    }

    public void SetSelectedLocal(bool selected)
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(selected);
    }

    private void OnColorChanged(Color oldC, Color newC)
    {
        ApplyColor(newC);
    }

    [ClientRpc]
    private void RpcApplyColor(Color color)
    {
        ApplyColor(color);
    }

    private void ApplyColor(Color color)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer != null)
            renderer.material.color = color;
    }

    [ServerCallback]
    private void Update()
    {
        if (health != null && !health.IsAlive)
            return;

        if (agent == null)
            return;

        if (!agent.enabled)
            agent.enabled = true;

        if (!BattleCommandAuthority.ManualControlEnabled)
        {
            ReturnToAuto();
        }

        switch (currentOrder)
        {
            case OrderMode.Move:
                ServerUpdateManualMove();
                break;

            case OrderMode.AttackUnit:
                ServerUpdateAttackUnit();
                break;

            case OrderMode.AttackRadioTower:
                ServerUpdateAttackRadioTower();
                break;

            case OrderMode.AttackBuilding:
                ServerUpdateAttackBuilding();
                break;

            default:
                ServerUpdateAutoAI();
                break;
        }
    }

    [Server]
    public void ServerSetMoveOrder(Vector3 destination)
    {
        QueueManualOrder(() => ApplyMoveOrder(destination));
    }

    [Server]
    public void ServerSetAttackUnitOrder(UnitAI target)
    {
        QueueManualOrder(() => ApplyAttackUnitOrder(target));
    }

    [Server]
    public void ServerSetAttackRadioTowerOrder(NetworkedRadioTower tower)
    {
        QueueManualOrder(() => ApplyAttackRadioTowerOrder(tower));
    }

    [Server]
    public void ServerSetAttackBuildingOrder(BuildingControl building)
    {
        QueueManualOrder(() => ApplyAttackBuildingOrder(building));
    }

    [Server]
    private void QueueManualOrder(Action applyOrder)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (pendingOrderCoroutine != null)
        {
            StopCoroutine(pendingOrderCoroutine);
            pendingOrderCoroutine = null;
        }

        float delay = GetCurrentManualCommandDelay();

        if (delay <= 0f)
        {
            applyOrder?.Invoke();
            return;
        }

        pendingOrderCoroutine = StartCoroutine(DelayedManualOrder(delay, applyOrder));
        Debug.Log($"[UnitAI] 通信塔受损，手动命令延迟 {delay} 秒执行。");
    }

    private IEnumerator DelayedManualOrder(float delay, Action applyOrder)
    {
        yield return new WaitForSeconds(delay);

        if (BattleCommandAuthority.ManualControlEnabled)
            applyOrder?.Invoke();

        pendingOrderCoroutine = null;
    }

    [Server]
    private float GetCurrentManualCommandDelay()
    {
        NetworkedRadioTower[] towers = FindObjectsByType<NetworkedRadioTower>(FindObjectsSortMode.None);

        foreach (NetworkedRadioTower tower in towers)
        {
            if (tower == null || tower.IsDestroyed)
                continue;

            if (!IsSameTeam(tower.teamColor, teamColor))
                continue;

            if (tower.health == null)
                continue;

            if (tower.health.NormalizedHp < damagedTowerDelayThreshold)
                return delayedCommandSeconds;
        }

        return 0f;
    }

    [Server]
    private void ApplyMoveOrder(Vector3 destination)
    {
        currentOrder = OrderMode.Move;
        manualMoveDestination = destination;
        manualUnitTarget = null;
        manualRadioTowerTarget = null;
        manualBuildingTarget = null;
        manualOrderStartTime = Time.time;

        agent.isStopped = false;
        agent.stoppingDistance = moveStopDistance;
        agent.SetDestination(destination);
    }

    [Server]
    private void ApplyAttackUnitOrder(UnitAI target)
    {
        if (target == null || target == this)
            return;

        if (IsSameTeam(target.teamColor, teamColor))
            return;

        if (!IsTargetInVision(target.transform.position))
            return;

        currentOrder = OrderMode.AttackUnit;
        manualUnitTarget = target;
        manualRadioTowerTarget = null;
        manualBuildingTarget = null;
        manualOrderStartTime = Time.time;
    }

    [Server]
    private void ApplyAttackRadioTowerOrder(NetworkedRadioTower tower)
    {
        if (tower == null || tower.IsDestroyed)
            return;

        if (IsSameTeam(tower.teamColor, teamColor))
            return;

        if (!IsTargetInVision(tower.transform.position))
            return;

        currentOrder = OrderMode.AttackRadioTower;
        manualRadioTowerTarget = tower;
        manualUnitTarget = null;
        manualBuildingTarget = null;
        manualOrderStartTime = Time.time;
    }

    [Server]
    private void ApplyAttackBuildingOrder(BuildingControl building)
    {
        if (building == null)
            return;

        if (IsSameTeam(building.teamColor, teamColor))
            return;

        if (!IsTargetInVision(building.transform.position))
            return;

        currentOrder = OrderMode.AttackBuilding;
        manualBuildingTarget = building;
        manualUnitTarget = null;
        manualRadioTowerTarget = null;
        manualOrderStartTime = Time.time;
    }

    [Server]
    private void ServerUpdateManualMove()
    {
        if (Time.time - manualOrderStartTime > manualMoveMaxSeconds)
        {
            ReturnToAuto();
            return;
        }

        if (!agent.pathPending)
        {
            bool reachedByAgent = agent.hasPath && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, moveStopDistance);
            bool reachedByDistance = Vector3.Distance(transform.position, manualMoveDestination) <= moveStopDistance;

            if (reachedByAgent || reachedByDistance)
            {
                ReturnToAuto();
                return;
            }
        }

        agent.isStopped = false;
        agent.stoppingDistance = moveStopDistance;
        agent.SetDestination(manualMoveDestination);
    }

    [Server]
    private void ServerUpdateAttackUnit()
    {
        if (manualUnitTarget == null)
        {
            ReturnToAuto();
            return;
        }

        NetworkHealth targetHealth = manualUnitTarget.GetComponent<NetworkHealth>();

        if (targetHealth == null || !targetHealth.IsAlive)
        {
            ReturnToAuto();
            return;
        }

        float dist = Vector3.Distance(transform.position, manualUnitTarget.transform.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(manualUnitTarget.transform.position);
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            targetHealth.ServerTakeDamage(attackDamageToUnit, gameObject);
        }
    }

    [Server]
    private void ServerUpdateAttackRadioTower()
    {
        if (manualRadioTowerTarget == null || manualRadioTowerTarget.IsDestroyed)
        {
            ReturnToAuto();
            return;
        }

        float distToTowerSurface = manualRadioTowerTarget.GetDistanceTo(transform.position);

        if (distToTowerSurface > attackRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(manualRadioTowerTarget.GetClosestPoint(transform.position));
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            manualRadioTowerTarget.ServerTakeDamage(attackDamageToRadioTower, gameObject);
        }
    }

    [Server]
    private void ServerUpdateAttackBuilding()
    {
        if (manualBuildingTarget == null)
        {
            ReturnToAuto();
            return;
        }

        NetworkHealth targetHealth = manualBuildingTarget.GetComponent<NetworkHealth>();

        if (targetHealth == null || !targetHealth.IsAlive)
        {
            ReturnToAuto();
            return;
        }

        float dist = Vector3.Distance(transform.position, manualBuildingTarget.transform.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(manualBuildingTarget.transform.position);
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            targetHealth.ServerTakeDamage(attackDamageToBuilding, gameObject);
        }
    }

    [Server]
    private void ServerUpdateAutoAI()
    {
        UnitAI enemyUnit = FindClosestEnemyUnitInVision();

        if (enemyUnit != null)
        {
            AutoAttackUnit(enemyUnit);
            return;
        }

        BuildingControl enemyBuilding = FindClosestEnemyBuildingInVision();

        if (enemyBuilding != null)
        {
            AutoAttackBuilding(enemyBuilding);
            return;
        }

        NetworkedRadioTower enemyTower = FindClosestEnemyTowerInVision();

        if (enemyTower != null)
        {
            AutoAttackTower(enemyTower);
            return;
        }

        agent.ResetPath();
    }

    [Server]
    private void AutoAttackUnit(UnitAI target)
    {
        if (target == null)
            return;

        NetworkHealth targetHealth = target.GetComponent<NetworkHealth>();

        if (targetHealth == null || !targetHealth.IsAlive)
            return;

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(target.transform.position);
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            targetHealth.ServerTakeDamage(attackDamageToUnit, gameObject);
        }
    }

    [Server]
    private void AutoAttackBuilding(BuildingControl building)
    {
        if (building == null)
            return;

        NetworkHealth targetHealth = building.GetComponent<NetworkHealth>();

        if (targetHealth == null || !targetHealth.IsAlive)
            return;

        float dist = Vector3.Distance(transform.position, building.transform.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(building.transform.position);
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            targetHealth.ServerTakeDamage(attackDamageToBuilding, gameObject);
        }
    }

    [Server]
    private void AutoAttackTower(NetworkedRadioTower tower)
    {
        if (tower == null || tower.IsDestroyed)
            return;

        float dist = tower.GetDistanceTo(transform.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(tower.GetClosestPoint(transform.position));
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            tower.ServerTakeDamage(attackDamageToRadioTower, gameObject);
        }
    }

    [Server]
    private UnitAI FindClosestEnemyUnitInVision()
    {
        UnitAI[] allUnits = FindObjectsByType<UnitAI>(FindObjectsSortMode.None);

        UnitAI closest = null;
        float minDist = Mathf.Infinity;

        foreach (UnitAI unit in allUnits)
        {
            if (unit == null || unit == this)
                continue;

            if (IsSameTeam(unit.teamColor, teamColor))
                continue;

            NetworkHealth unitHealth = unit.GetComponent<NetworkHealth>();
            if (unitHealth == null || !unitHealth.IsAlive)
                continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);

            if (dist > GetVisionRadius())
                continue;

            if (dist < minDist)
            {
                minDist = dist;
                closest = unit;
            }
        }

        return closest;
    }

    [Server]
    private BuildingControl FindClosestEnemyBuildingInVision()
    {
        BuildingControl[] buildings = FindObjectsByType<BuildingControl>(FindObjectsSortMode.None);

        BuildingControl closest = null;
        float minDist = Mathf.Infinity;

        foreach (BuildingControl building in buildings)
        {
            if (building == null)
                continue;

            if (IsSameTeam(building.teamColor, teamColor))
                continue;

            NetworkHealth buildingHealth = building.GetComponent<NetworkHealth>();
            if (buildingHealth == null || !buildingHealth.IsAlive)
                continue;

            float dist = Vector3.Distance(transform.position, building.transform.position);

            if (dist > GetVisionRadius())
                continue;

            if (dist < minDist)
            {
                minDist = dist;
                closest = building;
            }
        }

        return closest;
    }

    [Server]
    private NetworkedRadioTower FindClosestEnemyTowerInVision()
    {
        NetworkedRadioTower[] towers = FindObjectsByType<NetworkedRadioTower>(FindObjectsSortMode.None);

        NetworkedRadioTower closest = null;
        float minDist = Mathf.Infinity;

        foreach (NetworkedRadioTower tower in towers)
        {
            if (tower == null || tower.IsDestroyed)
                continue;

            if (IsSameTeam(tower.teamColor, teamColor))
                continue;

            float dist = Vector3.Distance(transform.position, tower.transform.position);

            if (dist > GetVisionRadius())
                continue;

            if (dist < minDist)
            {
                minDist = dist;
                closest = tower;
            }
        }

        return closest;
    }

    [Server]
    private void ReturnToAuto()
    {
        currentOrder = OrderMode.Auto;
        manualUnitTarget = null;
        manualRadioTowerTarget = null;
        manualBuildingTarget = null;

        if (agent != null && agent.enabled)
            agent.stoppingDistance = moveStopDistance;
    }

    private bool IsTargetInVision(Vector3 targetPos)
    {
        return Vector3.Distance(transform.position, targetPos) <= GetVisionRadius();
    }

    private float GetVisionRadius()
    {
        if (visionSource != null)
            return visionSource.visionRadius;

        return fallbackVisionRadius;
    }

    public static bool IsSameTeam(Color a, Color b)
    {
        return NetworkVisionUtility.SameTeam(a, b);
    }
}
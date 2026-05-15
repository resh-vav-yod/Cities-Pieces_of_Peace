using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class UnitAI : NetworkBehaviour
{
    public enum OrderMode
    {
        Auto,
        Move,
        AttackUnit,
        AttackRadioTower
    }

    [Header("阵营")]
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color teamColor;

    [Header("选择反馈 - 仅本地显示")]
    public GameObject selectionIndicator;

    [Header("移动")]
    public float moveStopDistance = 0.6f;
    public float manualMoveMaxSeconds = 8f;

    [Header("战斗")]
    public float attackRange = 3f;
    public float attackCooldown = 1f;
    public float attackDamageToRadioTower = 25f;

    private NavMeshAgent agent;
    private float lastAttackTime;

    private OrderMode currentOrder = OrderMode.Auto;
    private Vector3 manualMoveDestination;
    private UnitAI manualUnitTarget;
    private NetworkedRadioTower manualRadioTowerTarget;
    private float manualOrderStartTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isServer && agent != null)
        {
            agent.enabled = false;
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    public void SetSelectedLocal(bool selected)
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(selected);
    }

    private void OnColorChanged(Color oldC, Color newC)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.material.color = newC;
    }

    [ServerCallback]
    private void Update()
    {
        if (agent == null)
            return;

        if (!agent.enabled)
            agent.enabled = true;

        if (!BattleCommandAuthority.ManualControlEnabled)
        {
            currentOrder = OrderMode.Auto;
            manualUnitTarget = null;
            manualRadioTowerTarget = null;
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

            default:
                ServerUpdateAutoAI();
                break;
        }
    }

    [Server]
    public void ServerSetMoveOrder(Vector3 destination)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        currentOrder = OrderMode.Move;
        manualMoveDestination = destination;
        manualUnitTarget = null;
        manualRadioTowerTarget = null;
        manualOrderStartTime = Time.time;

        agent.isStopped = false;
        agent.stoppingDistance = moveStopDistance;
        agent.SetDestination(destination);
    }

    [Server]
    public void ServerSetAttackUnitOrder(UnitAI target)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (target == null)
            return;

        if (IsSameTeam(target.teamColor, teamColor))
            return;

        currentOrder = OrderMode.AttackUnit;
        manualUnitTarget = target;
        manualRadioTowerTarget = null;
        manualOrderStartTime = Time.time;
    }

    [Server]
    public void ServerSetAttackRadioTowerOrder(NetworkedRadioTower tower)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (tower == null || tower.IsDestroyed)
            return;

        currentOrder = OrderMode.AttackRadioTower;
        manualRadioTowerTarget = tower;
        manualUnitTarget = null;
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
            NetworkServer.Destroy(manualUnitTarget.gameObject);
            ReturnToAuto();
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
            Vector3 attackPos = manualRadioTowerTarget.GetClosestPoint(transform.position);

            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(attackPos);
            return;
        }

        agent.ResetPath();

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            manualRadioTowerTarget.ServerTakeDamage(attackDamageToRadioTower);
        }
    }

    [Server]
    private void ServerUpdateAutoAI()
    {
        UnitAI target = FindClosestEnemy();

        if (target == null)
        {
            agent.ResetPath();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist <= attackRange)
        {
            agent.ResetPath();

            if (Time.time - lastAttackTime > attackCooldown)
            {
                lastAttackTime = Time.time;
                NetworkServer.Destroy(target.gameObject);
            }
        }
        else
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(target.transform.position);
        }
    }

    [Server]
    private void ReturnToAuto()
    {
        currentOrder = OrderMode.Auto;
        manualUnitTarget = null;
        manualRadioTowerTarget = null;

        if (agent != null && agent.enabled)
        {
            agent.stoppingDistance = moveStopDistance;
        }
    }

    [Server]
    private UnitAI FindClosestEnemy()
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

            float d = Vector3.Distance(transform.position, unit.transform.position);

            if (d < minDist)
            {
                minDist = d;
                closest = unit;
            }
        }

        return closest;
    }

    public static bool IsSameTeam(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f
            && Mathf.Abs(a.g - b.g) < 0.01f
            && Mathf.Abs(a.b - b.b) < 0.01f
            && Mathf.Abs(a.a - b.a) < 0.01f;
    }
}
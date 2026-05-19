using UnityEngine;
using UnityEngine.AI;

public class CommandableUnit : MonoBehaviour
{
    [Header("Team")]
    [Tooltip("如果本物体有 Damageable，则优先使用 Damageable.teamId。")]
    public int fallbackTeamId = 0;

    [Header("Selection")]
    public GameObject selectionIndicator;

    [Header("Movement")]
    public float fallbackMoveSpeed = 4f;
    public float stoppingDistance = 0.2f;

    [Header("Manual Attack")]
    public bool enableBuiltInManualAttack = true;
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.8f;

    private NavMeshAgent agent;
    private Damageable selfDamageable;

    private Vector3 fallbackDestination;
    private bool hasFallbackDestination;

    private Damageable attackTarget;
    private float nextAttackTime;

    public int TeamId => selfDamageable != null ? selfDamageable.teamId : fallbackTeamId;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfDamageable = GetComponent<Damageable>();

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    private void Update()
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
        {
            StopManualMovement();
            return;
        }

        UpdateManualAttack();
        UpdateFallbackMovement();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(selected);
    }

    public void MoveTo(Vector3 worldPosition)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        attackTarget = null;

        if (CanUseNavMeshAgent())
        {
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(worldPosition);
            hasFallbackDestination = false;
        }
        else
        {
            fallbackDestination = worldPosition;
            hasFallbackDestination = true;
        }
    }

    public void Attack(Damageable target)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (target == null || !target.IsAlive)
            return;

        if (target.teamId == TeamId)
            return;

        attackTarget = target;

        if (CanUseNavMeshAgent())
        {
            agent.isStopped = false;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.SetDestination(target.transform.position);
        }
    }

    public void ClearCommand()
    {
        attackTarget = null;
        StopManualMovement();
    }

    private void UpdateManualAttack()
    {
        if (!enableBuiltInManualAttack)
            return;

        if (attackTarget == null || !attackTarget.IsAlive)
            return;

        float distance = Vector3.Distance(transform.position, attackTarget.transform.position);

        if (distance > attackRange)
        {
            if (CanUseNavMeshAgent())
            {
                agent.isStopped = false;
                agent.SetDestination(attackTarget.transform.position);
            }
            else
            {
                fallbackDestination = attackTarget.transform.position;
                hasFallbackDestination = true;
            }

            return;
        }

        StopManualMovement();

        Vector3 lookDirection = attackTarget.transform.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDirection);

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            attackTarget.TakeDamage(attackDamage, gameObject);
        }
    }

    private void UpdateFallbackMovement()
    {
        if (!hasFallbackDestination)
            return;

        if (attackTarget != null)
            return;

        Vector3 current = transform.position;
        Vector3 target = fallbackDestination;
        target.y = current.y;

        float distance = Vector3.Distance(current, target);

        if (distance <= stoppingDistance)
        {
            hasFallbackDestination = false;
            return;
        }

        Vector3 direction = (target - current).normalized;
        transform.position += direction * fallbackMoveSpeed * Time.deltaTime;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void StopManualMovement()
    {
        hasFallbackDestination = false;

        if (CanUseNavMeshAgent())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private bool CanUseNavMeshAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }
}
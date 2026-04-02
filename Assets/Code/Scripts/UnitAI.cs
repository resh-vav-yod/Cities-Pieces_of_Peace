using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class UnitAI : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color teamColor;

    private NavMeshAgent agent;
    public float attackRange = 3f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // 【核心修复】当小兵在客户端生成时，直接关掉它的大脑！
    // 让它变成一个只听服务器发坐标的木偶，彻底杜绝发呆和乱跑！
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer) 
        {
            agent.enabled = false; 
        }
    }

    void OnColorChanged(Color oldC, Color newC)
    {
        GetComponent<MeshRenderer>().material.color = newC;
    }

    // 只有服务器会执行 Update 里的 AI 逻辑
    [ServerCallback]
    void Update()
    {
        // 确保服务器上的 Agent 是开启的
        if (!agent.enabled) agent.enabled = true;

        UnitAI target = FindClosestEnemy();

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);

            if (dist <= attackRange)
            {
                agent.ResetPath(); // 停下脚步准备攻击
                
                if (Time.time - lastAttackTime > attackCooldown)
                {
                    lastAttackTime = Time.time;
                    Debug.Log($"[{teamColor}] 的小兵击杀了敌人！");
                    NetworkServer.Destroy(target.gameObject); 
                }
            }
            else
            {
                // 距离不够，继续追击
                agent.SetDestination(target.transform.position);
            }
        }
    }

    // 索敌逻辑不变
    UnitAI FindClosestEnemy()
    {
        UnitAI[] allUnits = FindObjectsByType<UnitAI>(FindObjectsSortMode.None);
        UnitAI closest = null;
        float minDist = Mathf.Infinity;

        foreach (var u in allUnits)
        {
            if (u == this || u.teamColor == this.teamColor) continue;

            float d = Vector3.Distance(transform.position, u.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = u;
            }
        }
        return closest;
    }
}

/*
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class UnitAI : NetworkBehaviour
{
    [Header("阵营与组件")]
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color teamColor;
    
    private NavMeshAgent agent;
    
    [Header("战斗属性")]
    public float attackRange = 3f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // 变色
    void OnColorChanged(Color oldC, Color newC)
    {
        GetComponent<MeshRenderer>().material.color = newC;
    }

    // ServerCallback 确保这个 Update 里的逻辑只有服务器在算！客户端不执行！
    [ServerCallback]
    void Update()
    {
        // 1. 寻找最近的敌人
        UnitAI target = FindClosestEnemy();

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            
            // 2. 如果敌人在攻击范围内
            if (dist <= attackRange)
            {
                agent.ResetPath(); // 停下脚步
                
                // 3. 攻击判定 (这里做最简单的秒杀效果，简历 Demo 足够了)
                if (Time.time - lastAttackTime > attackCooldown)
                {
                    lastAttackTime = Time.time;
                    Debug.Log("发动攻击！摧毁敌方单位！");
                    NetworkServer.Destroy(target.gameObject); 
                }
            }
            else
            {
                // 4. 不在范围内，让 NavMeshAgent 自动寻路追击！
                agent.SetDestination(target.transform.position);
            }
        }
    }

    // 遍历全场寻找不同颜色的单位
    UnitAI FindClosestEnemy()
    {
        UnitAI[] allUnits = FindObjectsByType<UnitAI>(FindObjectsSortMode.None);
        UnitAI closest = null;
        float minDist = Mathf.Infinity;

        foreach (var u in allUnits)
        {
            // 如果是自己，或者是同色友军，跳过
            if (u == this || u.teamColor == this.teamColor) continue;

            float d = Vector3.Distance(transform.position, u.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = u;
            }
        }
        return closest;
    }
}
*/
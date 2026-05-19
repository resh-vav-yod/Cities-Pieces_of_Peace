using System;
using Mirror;
using UnityEngine;

/// <summary>
/// 网络生命值组件。
/// 单位、建筑、无线电塔都可以使用。
/// HP 只由服务器修改，客户端通过 SyncVar 接收结果。
/// </summary>
public class NetworkHealth : NetworkBehaviour
{
    [Header("生命值")]
    public float maxHp = 100f;

    [SyncVar(hook = nameof(OnHpChanged))]
    private float currentHp;

    [SyncVar]
    private bool isDead;

    [Header("死亡规则")]
    public bool destroyOnDeath = true;

    public float CurrentHp => currentHp;
    public bool IsAlive => !isDead && currentHp > 0f;
    public float NormalizedHp => maxHp <= 0f ? 0f : currentHp / maxHp;

    public GameObject LastDamageSource { get; private set; }

    public event Action<NetworkHealth> ServerDied;
    public event Action<float, float> ClientHpChanged;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (maxHp <= 0f)
            maxHp = 1f;

        currentHp = maxHp;
        isDead = false;
        LastDamageSource = null;
    }

    [Server]
    public void ServerTakeDamage(float amount, GameObject source = null)
    {
        if (isDead)
            return;

        if (amount <= 0f)
            return;

        LastDamageSource = source;

        currentHp = Mathf.Max(0f, currentHp - amount);

        if (currentHp <= 0f)
        {
            ServerDie();
        }
    }

    [Server]
    public void ServerHeal(float amount)
    {
        if (isDead)
            return;

        if (amount <= 0f)
            return;

        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    [Server]
    private void ServerDie()
    {
        if (isDead)
            return;

        isDead = true;
        currentHp = 0f;

        ServerDied?.Invoke(this);

        if (destroyOnDeath)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    private void OnHpChanged(float oldHp, float newHp)
    {
        ClientHpChanged?.Invoke(oldHp, newHp);
    }
}
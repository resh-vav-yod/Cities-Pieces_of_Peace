using System;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [Header("Team")]
    [Tooltip("0 = 玩家，1 = 敌人。之后可以换成 FactionId。")]
    public int teamId = 0;

    [Header("Health")]
    public float maxHp = 100f;
    [SerializeField] private float currentHp;

    public bool IsAlive => currentHp > 0f;
    public float CurrentHp => currentHp;
    public float NormalizedHp => maxHp <= 0f ? 0f : currentHp / maxHp;

    public event Action<Damageable> OnDied;
    public event Action<Damageable, float> OnDamaged;

    private bool hasDied;

    private void Awake()
    {
        if (maxHp <= 0f)
            maxHp = 1f;

        if (currentHp <= 0f)
            currentHp = maxHp;

        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
        hasDied = currentHp <= 0f;
    }

    public void TakeDamage(float amount, GameObject damageSource = null)
    {
        if (!IsAlive || amount <= 0f)
            return;

        currentHp = Mathf.Max(0f, currentHp - amount);
        OnDamaged?.Invoke(this, amount);

        if (currentHp <= 0f && !hasDied)
        {
            hasDied = true;
            OnDied?.Invoke(this);
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !IsAlive)
            return;

        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    public void ResetHealth()
    {
        currentHp = maxHp;
        hasDied = false;
    }

    [ContextMenu("Debug Kill")]
    private void DebugKill()
    {
        TakeDamage(maxHp + 9999f, gameObject);
    }

    private void OnValidate()
    {
        if (maxHp <= 0f)
            maxHp = 1f;

        if (!Application.isPlaying)
            currentHp = Mathf.Clamp(currentHp <= 0f ? maxHp : currentHp, 0f, maxHp);
    }
}
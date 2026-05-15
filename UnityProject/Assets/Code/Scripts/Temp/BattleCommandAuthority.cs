using System;
using UnityEngine;

public class BattleCommandAuthority : MonoBehaviour
{
    public static bool ManualControlEnabled { get; private set; } = true;
    public static event Action<bool> OnManualControlChanged;

    [Header("初始化")]
    public bool resetManualControlOnAwake = true;

    private void Awake()
    {
        if (resetManualControlOnAwake)
        {
            SetManualControl(true);
        }
    }

    public static void SetManualControl(bool enabled)
    {
        if (ManualControlEnabled == enabled)
            return;

        ManualControlEnabled = enabled;
        OnManualControlChanged?.Invoke(enabled);

        Debug.Log($"[BattleCommandAuthority] ManualControlEnabled = {enabled}");
    }
}
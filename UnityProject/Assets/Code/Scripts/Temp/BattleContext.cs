using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Earth 与 Battle 之间的临时运行时上下文。
/// 用于 5.20 版本：记录当前战斗区域、战斗结果、区域冷却。
/// 不写入磁盘，只在本次运行期间有效。
/// </summary>
public static class BattleContext
{
    public static int currentRegionId = 0;
    public static string currentRegionName = "";
    public static string currentOwner = "";
    public static string currentTerrain = "";

    public static bool hasActiveBattle = false;

    private static readonly Dictionary<int, BattleOutcome> outcomesByRegion = new Dictionary<int, BattleOutcome>();
    private static readonly Dictionary<int, string> ownerOverrideByRegion = new Dictionary<int, string>();
    private static readonly Dictionary<int, string> statusByRegion = new Dictionary<int, string>();
    private static readonly Dictionary<int, float> regionLockUntilTime = new Dictionary<int, float>();

    public static void PrepareBattleRegion(int regionId, string regionName, string owner, string terrain)
    {
        currentRegionId = regionId;
        currentRegionName = regionName;
        currentOwner = owner;
        currentTerrain = terrain;
        hasActiveBattle = regionId > 0;

        Debug.Log($"[BattleContext] 准备进入战斗：regionId={regionId}, name={regionName}, owner={owner}, terrain={terrain}");
    }

    public static void ReportCurrentBattleResult(BattleOutcome outcome, float lockSeconds = 0f)
    {
        if (!hasActiveBattle || currentRegionId <= 0)
        {
            Debug.LogWarning("[BattleContext] 没有有效的当前战斗区域，无法记录战斗结果。");
            return;
        }

        ReportBattleResult(currentRegionId, outcome, lockSeconds);
    }

    public static void ReportBattleResult(int regionId, BattleOutcome outcome, float lockSeconds = 0f)
    {
        if (regionId <= 0)
            return;

        outcomesByRegion[regionId] = outcome;

        switch (outcome)
        {
            case BattleOutcome.Victory:
                ownerOverrideByRegion[regionId] = "Player";
                statusByRegion[regionId] = "Victory";
                break;

            case BattleOutcome.Defeat:
                ownerOverrideByRegion[regionId] = "Enemy";
                statusByRegion[regionId] = "Defeat";
                break;

            case BattleOutcome.RadioLost:
                ownerOverrideByRegion[regionId] = "Enemy";
                statusByRegion[regionId] = "Radio Lost";
                break;

            case BattleOutcome.Retreat:
                statusByRegion[regionId] = "Retreat";
                break;

            default:
                statusByRegion[regionId] = "None";
                break;
        }

        if (lockSeconds > 0f)
        {
            regionLockUntilTime[regionId] = Time.realtimeSinceStartup + lockSeconds;
            statusByRegion[regionId] = $"Radio Tower Rebuilding";
        }

        Debug.Log($"[BattleContext] 写入战斗结果：regionId={regionId}, outcome={outcome}, lock={lockSeconds}s");
    }

    public static bool IsRegionLocked(int regionId)
    {
        if (!regionLockUntilTime.TryGetValue(regionId, out float until))
            return false;

        if (Time.realtimeSinceStartup < until)
            return true;

        regionLockUntilTime.Remove(regionId);

        if (statusByRegion.ContainsKey(regionId))
            statusByRegion[regionId] = "Ready";

        return false;
    }

    public static float GetRemainingLockSeconds(int regionId)
    {
        if (!regionLockUntilTime.TryGetValue(regionId, out float until))
            return 0f;

        float remaining = until - Time.realtimeSinceStartup;

        if (remaining <= 0f)
        {
            regionLockUntilTime.Remove(regionId);

            if (statusByRegion.ContainsKey(regionId))
                statusByRegion[regionId] = "Ready";

            return 0f;
        }

        return remaining;
    }

    public static bool TryGetOutcome(int regionId, out BattleOutcome outcome)
    {
        return outcomesByRegion.TryGetValue(regionId, out outcome);
    }

    public static bool TryGetOwnerOverride(int regionId, out string owner)
    {
        return ownerOverrideByRegion.TryGetValue(regionId, out owner);
    }

    public static bool TryGetStatus(int regionId, out string status)
    {
        return statusByRegion.TryGetValue(regionId, out status);
    }
}

public enum BattleOutcome
{
    None,
    Victory,
    Defeat,
    Retreat,
    RadioLost
}
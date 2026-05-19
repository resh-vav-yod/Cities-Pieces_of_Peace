using UnityEngine;

/// <summary>
/// Battle 通信状态工具。
/// 当前用于统一判断：某阵营无线电塔低血量时，玩家输入是否需要延迟。
/// </summary>
public static class BattleSignalUtility
{
    /// <summary>
    /// 如果某阵营任意己方无线电塔 HP 低于 threshold，则返回 delaySeconds。
    /// 否则返回 0。
    /// </summary>
    public static float GetManualCommandDelayForTeam(Color teamColor, float threshold = 0.5f, float delaySeconds = 3f)
    {
        NetworkedRadioTower[] towers = Object.FindObjectsByType<NetworkedRadioTower>(FindObjectsSortMode.None);

        foreach (NetworkedRadioTower tower in towers)
        {
            if (tower == null || tower.IsDestroyed)
                continue;

            if (!UnitAI.IsSameTeam(tower.teamColor, teamColor))
                continue;

            if (tower.health == null)
                continue;

            if (tower.health.NormalizedHp < threshold)
                return delaySeconds;
        }

        return 0f;
    }
}
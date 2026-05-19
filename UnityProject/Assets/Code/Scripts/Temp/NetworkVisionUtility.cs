using UnityEngine;

/// <summary>
/// 临时视野工具类。
/// 用静态方法集中做“是否同阵营”“某点是否在友方视野内”的判断。
/// </summary>
public static class NetworkVisionUtility
{
    public static bool SameTeam(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f
            && Mathf.Abs(a.g - b.g) < 0.01f
            && Mathf.Abs(a.b - b.b) < 0.01f
            && Mathf.Abs(a.a - b.a) < 0.01f;
    }

    /// <summary>
    /// 判断一个世界坐标是否在某阵营任意视野源范围内。
    /// 建造扩展限制会用这个。
    /// </summary>
    public static bool IsPointVisibleToTeam(Vector3 point, Color teamColor, bool requireBuildRange = false)
    {
        NetworkVisionSource[] sources = Object.FindObjectsByType<NetworkVisionSource>(FindObjectsSortMode.None);

        foreach (NetworkVisionSource source in sources)
        {
            if (source == null)
                continue;

            if (requireBuildRange && !source.contributesToBuildRange)
                continue;

            if (!SameTeam(source.teamColor, teamColor))
                continue;

            if (source.ContainsPoint(point))
                return true;
        }

        return false;
    }
}
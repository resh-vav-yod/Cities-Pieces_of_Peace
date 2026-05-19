using Mirror;
using UnityEngine;

/// <summary>
/// 临时视野源。
/// 单位、建筑、无线电塔都可以挂。
/// 作用：
/// 1. 单位只攻击自己视野内的敌人。
/// 2. 建筑只能建在友方视野范围内。
/// </summary>
public class NetworkVisionSource : NetworkBehaviour
{
    [Header("阵营")]
    [SyncVar]
    public Color teamColor = Color.white;

    [Header("视野")]
    public float visionRadius = 12f;

    [Tooltip("是否参与建造扩展范围判断。单位/建筑/塔都可以先勾上。")]
    public bool contributesToBuildRange = true;

    /// <summary>
    /// 服务器设置阵营颜色。
    /// 由于视野判断要按阵营区分，所以生成时必须调用。
    /// </summary>
    [Server]
    public void ServerSetTeam(Color color)
    {
        teamColor = color;
    }

    public bool ContainsPoint(Vector3 worldPos)
    {
        float dist = Vector3.Distance(transform.position, worldPos);
        return dist <= visionRadius;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
#endif
}
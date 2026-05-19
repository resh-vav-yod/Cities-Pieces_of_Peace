using UnityEngine;

/// <summary>
/// Temp 世界空间血条。
/// 用于 RTS 俯视角：作为单位/建筑/塔的子物体固定显示，不自动朝向摄像机。
/// 只读取 NetworkHealth，不参与网络同步。
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    [Header("引用")]
    public NetworkHealth health;
    public Transform fill;

    [Header("本地位置")]
    public Vector3 localOffset = new Vector3(0f, 2.2f, 0f);

    [Header("显示")]
    public bool alwaysShow = true;

    private Vector3 fillOriginalScale;
    private Vector3 fillOriginalLocalPosition;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<NetworkHealth>();

        if (fill != null)
        {
            fillOriginalScale = fill.localScale;
            fillOriginalLocalPosition = fill.localPosition;
        }

        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (health == null || fill == null)
            return;

        // 作为父物体的子对象固定在头顶。
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;

        if (!alwaysShow && health.NormalizedHp >= 0.999f)
        {
            SetChildrenVisible(false);
            return;
        }

        SetChildrenVisible(true);

        float ratio = Mathf.Clamp01(health.NormalizedHp);

        fill.localScale = new Vector3(
            fillOriginalScale.x * ratio,
            fillOriginalScale.y,
            fillOriginalScale.z
        );

        // 让血条从左向右减少，而不是从中间缩短。
        fill.localPosition = new Vector3(
            fillOriginalLocalPosition.x - fillOriginalScale.x * (1f - ratio) * 0.5f,
            fillOriginalLocalPosition.y,
            fillOriginalLocalPosition.z
        );
    }

    private void SetChildrenVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}
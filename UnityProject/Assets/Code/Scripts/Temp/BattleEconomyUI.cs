using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle 经济 UI。
/// 只显示当前本地玩家的钱数和建筑/单位费用。
/// 该 UI 不拦截鼠标点击。
/// </summary>
public class BattleEconomyUI : MonoBehaviour
{
    [Header("文本")]
    public TMP_Text creditsText;
    public TMP_Text buildingCostText;
    public TMP_Text unitCostText;
    public TMP_Text warningText;

    [Header("费用来源")]
    public PlayerBuilder playerBuilder;
    public BuildingControl sampleBuilding;

    private float nextFindTime = 0f;

    private void Awake()
    {
        DisableRaycastTargets();
    }

    private void Update()
    {
        RefreshPlayerReferenceSafely();
        RefreshTexts();
    }

    /// <summary>
    /// 安全寻找本地玩家。
    /// 不直接访问 NetworkClient.localPlayer，避免 Mirror 初始化/切场景阶段空引用。
    /// </summary>
    private void RefreshPlayerReferenceSafely()
    {
        if (playerBuilder != null)
            return;

        // 不要每帧 Find，降低报错/性能风险。
        if (Time.time < nextFindTime)
            return;

        nextFindTime = Time.time + 0.5f;

        if (PlayerBuilder.LocalPlayer != null)
        {
            playerBuilder = PlayerBuilder.LocalPlayer;
            return;
        }

        PlayerBuilder[] builders = FindObjectsByType<PlayerBuilder>(FindObjectsSortMode.None);

        foreach (PlayerBuilder builder in builders)
        {
            if (builder == null)
                continue;

            if (builder.isLocalPlayer)
            {
                playerBuilder = builder;
                return;
            }
        }

        // Host 测试兜底：如果只有一个 PlayerBuilder，就先拿它显示。
        if (builders != null && builders.Length == 1 && builders[0] != null)
        {
            playerBuilder = builders[0];
        }
    }

    private void RefreshTexts()
    {
        if (playerBuilder == null)
        {
            if (creditsText != null)
                creditsText.text = "Credits: waiting";

            if (buildingCostText != null)
                buildingCostText.text = "Building: -";

            if (unitCostText != null)
                unitCostText.text = "Unit: -";

            if (warningText != null)
                warningText.text = "";

            return;
        }

        if (creditsText != null)
            creditsText.text = $"Credits: {playerBuilder.credits}";

        if (buildingCostText != null)
            buildingCostText.text = $"Building: {playerBuilder.buildingCost}";

        if (unitCostText != null)
        {
            if (sampleBuilding != null)
                unitCostText.text = $"Unit: {sampleBuilding.unitCost}";
            else
                unitCostText.text = "Unit: -";
        }

        if (warningText != null)
        {
            if (playerBuilder.credits < playerBuilder.buildingCost)
                warningText.text = "Not enough credits.";
            else
                warningText.text = "";
        }
    }

    /// <summary>
    /// 经济面板只是显示信息，不应该挡住鼠标点击。
    /// </summary>
    private void DisableRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic != null)
                graphic.raycastTarget = false;
        }
    }
}
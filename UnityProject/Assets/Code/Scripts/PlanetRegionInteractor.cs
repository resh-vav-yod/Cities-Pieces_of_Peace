using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using UnityEngine.EventSystems;

public class PlanetRegionInteractor : MonoBehaviour
{
    [Header("UI 联动")]
    public GameObject regionInfoPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI ownerText;
    public TextMeshProUGUI resourceText;

    [Header("核心引用")]
    public Camera mainCam;
    public Collider planetCollider;
    public Texture2D idMap;
    public RegionRuntimeDatabase database;

    [Header("高亮层")]
    public Renderer highlightOverlayRenderer;

    [Header("战斗场景")]
    public string battleSceneName = "test-battle";

    private Color32[] cachedPixels;
    private Material highlightMaterial;

    private int selectedCellId = 0;
    private string selectedRegionName = "";
    private string selectedOwner = "";
    private string selectedTerrain = "";

    private void Start()
    {
        if (idMap != null)
        {
            cachedPixels = idMap.GetPixels32();
        }

        if (highlightOverlayRenderer != null)
        {
            highlightMaterial = highlightOverlayRenderer.material;
        }

        if (database != null)
        {
            database.ApplyAllBattleResultsFromContext();
        }
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TryPickRegion();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        if (regionInfoPanel != null && regionInfoPanel.activeSelf)
        {
            regionInfoPanel.SetActive(false);
            ClearHighlight();
            Debug.Log("通过 ESC 关闭了信息面板");
        }

        selectedCellId = 0;
        selectedRegionName = "";
        selectedOwner = "";
        selectedTerrain = "";
    }

    void TryPickRegion()
    {
        if (mainCam == null || planetCollider == null || idMap == null || database == null)
        {
            Debug.LogWarning("PlanetRegionInteractor 缺少引用，请检查 Inspector。");
            return;
        }

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (!planetCollider.Raycast(ray, out RaycastHit hit, 100000f))
            return;

        Vector2 uv = hit.textureCoord;

        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * idMap.width), 0, idMap.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * idMap.height), 0, idMap.height - 1);

        int index = y * idMap.width + x;
        if (cachedPixels == null || index < 0 || index >= cachedPixels.Length)
            return;

        Color32 pickedColor = cachedPixels[index];
        int cellId = DecodeId(pickedColor);

        Debug.Log($"点击 UV: {uv}, cellId: {cellId}");

        if (cellId == 0)
        {
            ClearHighlight();

            if (regionInfoPanel != null)
                regionInfoPanel.SetActive(false);

            selectedCellId = 0;
            selectedRegionName = "";
            selectedOwner = "";
            selectedTerrain = "";
            return;
        }

        database.TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        string nameResult = database.GetDisplayName(cellId);
        string ownerResult = database.GetOwner(cellId);
        string terrainResult = database.GetTerrain(cellId);
        string battleStatus = database.GetBattleStatus(cellId);

        float remainingLock = BattleContext.GetRemainingLockSeconds(cellId);
        if (remainingLock > 0f)
        {
            battleStatus = $"Tower Rebuilding: {Mathf.CeilToInt(remainingLock)}s";
        }

        selectedCellId = cellId;
        selectedRegionName = nameResult;
        selectedOwner = ownerResult;
        selectedTerrain = terrainResult;

        if (regionInfoPanel != null)
        {
            if (nameText != null)
                nameText.text = nameResult;

            if (ownerText != null)
                ownerText.text = $"Owner: {ownerResult}\nStatus: {battleStatus}";

            if (resourceText != null)
            {
                if (val != null && val.resources != null)
                {
                    resourceText.text =
                        $"food: {val.resources.food}\n" +
                        $"wood: {val.resources.wood}\n" +
                        $"iron: {val.resources.iron}\n" +
                        $"terrain: {terrainResult}";
                }
                else
                {
                    resourceText.text = $"terrain: {terrainResult}";
                }
            }

            regionInfoPanel.SetActive(true);
        }

        ApplyHighlight(pickedColor);
    }

    void ApplyHighlight(Color32 pickedColor)
    {
        if (highlightMaterial == null) return;

        Vector4 selectedId = new Vector4(
            pickedColor.r / 255f,
            pickedColor.g / 255f,
            pickedColor.b / 255f,
            1f
        );

        highlightMaterial.SetVector("_SelectedIdRGB", selectedId);
        highlightMaterial.SetFloat("_HighlightOpacity", 0.65f);
    }

    void ClearHighlight()
    {
        if (highlightMaterial == null) return;
        highlightMaterial.SetFloat("_HighlightOpacity", 0f);
    }

    int DecodeId(Color32 c)
    {
        return c.r | (c.g << 8) | (c.b << 16);
    }

    public void JumpToBattleScene()
    {
        if (selectedCellId <= 0)
        {
            Debug.LogWarning("[PlanetRegionInteractor] 还没有选择有效地球区域，不能进入 Battle。");
            return;
        }

        float remainingLock = BattleContext.GetRemainingLockSeconds(selectedCellId);

        if (remainingLock > 0f)
        {
            string message = $"Radio tower is rebuilding.\nWait {Mathf.CeilToInt(remainingLock)}s.";

            Debug.LogWarning($"[PlanetRegionInteractor] 当前区域暂时不能进入，还需等待 {remainingLock:F1} 秒。");

            if (ownerText != null)
                ownerText.text = $"Owner: {selectedOwner}\nStatus: {message}";

            return;
        }

        BattleContext.PrepareBattleRegion(
            selectedCellId,
            selectedRegionName,
            selectedOwner,
            selectedTerrain
        );

        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(battleSceneName);
        }
        else
        {
            Debug.LogWarning("[PlanetRegionInteractor] 当前不是 Server/Host，不能切换到 Battle 场景。");
        }
    }
}
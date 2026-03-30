using UnityEngine;
using UnityEngine.UI; // 必须引用 UI 命名空间
using TMPro;
using Mirror;         // 用于场景跳转
using UnityEngine.EventSystems;

public class PlanetRegionInteractor : MonoBehaviour
{
    [Header("UI 联动")]
    public GameObject regionInfoPanel; // 拖入你的右侧弹窗面板
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

    private Color32[] cachedPixels;
    private Material highlightMaterial;

    private void Start()
    {
        if (idMap != null)
        {
            cachedPixels = idMap.GetPixels32();
        }

        if (highlightOverlayRenderer != null)
        {
            // material 会自动实例化一份材质，不会污染原始材质
            highlightMaterial = highlightOverlayRenderer.material;
        }
    }

    private void Update()
    {
        // 如果鼠标当前指在 UI 上 (比如按钮、面板)，直接退出，不再往后执行地球点击判定
        if (EventSystem.current.IsPointerOverGameObject()) 
        {
            return; 
        }
        
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
            regionInfoPanel.SetActive(false); // 关闭面板
            ClearHighlight();                // 清除地球上的高亮
            Debug.Log("通过 ESC 关闭了信息面板");
        }
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

        // ... 之前的采样和解码代码保持不变 ...

        if (cellId == 0)
        {
            ClearHighlight();
            regionInfoPanel.SetActive(false); // 点到海洋时关闭面板
            return;
        }

        database.TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        // 1. 确定名称
        string nameResult = val != null && !string.IsNullOrEmpty(val.displayName)
            ? val.displayName
            : (gen != null ? gen.name : "Unknown Region");

        // 2. 确定归属
        string ownerResult = val != null ? val.owner : "None";

        // 3. 联动 UI 显示 [新增逻辑]
        if (regionInfoPanel != null)
        {
            nameText.text = nameResult;
            ownerText.text = "Owner: " + ownerResult;
            
            // 如果有详细资源数据，显示资源
            if (val != null && val.resources != null)
            {
                resourceText.text = $"food: {val.resources.food} | wood: {val.resources.wood} | iron: {val.resources.iron}";
            }
            
            regionInfoPanel.SetActive(true); // 激活面板
        }

        ApplyHighlight(pickedColor);

        /*
        if (cellId == 0)
        {
            ClearHighlight();
            Debug.Log("点到了空白区域或海洋。");
            return;
        }

        database.TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        string nameText = val != null && !string.IsNullOrEmpty(val.displayName)
            ? val.displayName
            : (gen != null ? gen.name : "未知地区");

        string ownerText = val != null ? val.owner : "无主";

        Debug.Log($"成功选中地区：ID={cellId}，名称={nameText}，归属={ownerText}");

        ApplyHighlight(pickedColor);
        */
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
        // 只有 Server/Host 才能发起场景切换
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene("test-battle");
        }
    }
}
/*
 * 这个脚本负责处理玩家点击地球时的交互逻辑。
 * 核心思路是：通过射线检测获取点击位置的 UV 坐标，然后从 ID Map 贴图中读取对应像素的颜色值，解码出 Cell ID，最后查询数据库获取区域信息并显示。
 * 
 * 使用步骤：
 * 1. 将这个脚本挂载到一个空 GameObject 上，比如叫 "PlanetRegionInteractor"。
 * 2. 在 Inspector 中赋值 mainCam（主摄像机）、planetCollider（地球的 SphereCollider）、idMap（ID Map 贴图）和 database（区域数据库 ScriptableObject）。
 * 3. 确保地球模型有正确的 UV 展开，并且 ID Map 贴图的分辨率和 UV 对应关系正确。

using UnityEngine;

public class PlanetRegionInteractor : MonoBehaviour
{
    [Header("核心引用")]
    public Camera mainCam;
    public Collider planetCollider; // 必须是 SphereCollider
    public Texture2D idMap;
    public RegionRuntimeDatabase database;

    private void Update()
    {
        // 鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            TryPickRegion();
        }
    }

    void TryPickRegion()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // 射线检测球体
        if (!planetCollider.Raycast(ray, out RaycastHit hit, 100000f))
            return;

        // 核心：获取 UV 坐标并转为对应像素颜色
        Vector2 uv = hit.textureCoord;
        int cellId = SampleCellId(uv);

        if (cellId == 0)
        {
            Debug.Log("💦 点到了无效区域或海洋！");
            return;
        }

        // 去数据库查数据
        database.TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        string display = val?.displayName ?? gen?.name ?? "未知区域";
        string owner = val?.owner ?? "无主之地";
        
        Debug.Log($"🎯 成功选中！Cell ID: {cellId} | 名称: {display} | 归属: {owner}");
    }

    int SampleCellId(Vector2 uv)
    {
        if (idMap == null) return 0;

        // 将 UV 映射到贴图像素分辨率上
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * idMap.width), 0, idMap.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * idMap.height), 0, idMap.height - 1);

        Color32 c = idMap.GetPixel(x, y);
        return DecodeId(c);
    }

    // 将 RGB 像素还原为整数 ID (对应 Python 里的位运算)
    int DecodeId(Color32 c)
    {
        return c.r | (c.g << 8) | (c.b << 16);
    }

    
}
*/
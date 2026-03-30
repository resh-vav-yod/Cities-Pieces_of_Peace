using UnityEngine;
using Mirror;

public class PlayerBuilder : NetworkBehaviour
{
    [Header("配置")]
    public LayerMask groundLayer;          // 选你刚才创建的 Ground Layer
    public GameObject previewPrefab;       // 拖入刚才做的 BuildingPreview 预制体
    public GameObject actualBuildingPrefab;// 拖入真正要生成的建筑预制体 (稍后做)
    
    [Header("材质颜色")]
    public Material previewMaterial;       // 关联预览预制体的材质
    public Color colorValid = new Color(0, 1, 0, 0.5f);   // 绿色半透明
    public Color colorInvalid = new Color(1, 0, 0, 0.5f); // 红色半透明

    private GameObject currentPreview;
    private bool isBuildMode = false;
    private int currentGridX, currentGridY;
    private bool canPlaceCurrently;

/*
    void Update()
    {
        if (!isLocalPlayer) return; // 只控制本地玩家的操作

        if (Camera.main == null || GridManager.Instance == null) return;

        // 按 B 键开关建造模式 (你可以后续改成 UI 按钮触发)
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("检测到按下 B 键！当前 isBuildMode: " + !isBuildMode);
            ToggleBuildMode(!isBuildMode);
        }

        if (isBuildMode)
        {
            if (Camera.main == null) { Debug.LogWarning("找不到主摄像机！"); return; }
            if (GridManager.Instance == null) { Debug.LogWarning("找不到 GridManager 实例！"); return; }
            HandlePreview();
            HandleClickToBuild();
        }
    }
*/

    void Update()
    {
        // 1. 权限检查
        if (!isLocalPlayer) return;

        // 2. 基础引用检查（如果这里报错，说明相机没标签，或者网格管理器没挂好）
        if (Camera.main == null) 
        {
            Debug.LogError("🚨 严重错误：找不到主摄像机！请选中战斗场景的相机，把 Inspector 顶部的 Tag 改为 MainCamera。");
            return;
        }
        if (GridManager.Instance == null) 
        {
            Debug.LogError("🚨 严重错误：GridManager 实例为空！请确认战斗场景中有 GridManager 物体，且挂载了脚本。");
            return;
        }

        // 3. 按键检测
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("✅ 成功检测到按下 B 键！切换建造模式状态。");
            ToggleBuildMode(!isBuildMode);
        }

        // 4. 射线检测排错
        if (isBuildMode)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // 我们画出这条射线，你在 Scene 窗口（不是 Game 窗口）里能看到一条红线
            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                HandlePreview();
                HandleClickToBuild();
            }
            else
            {
                // 如果你按了 B，控制台疯狂刷这条警告，说明射线穿透了地面！
                Debug.LogWarning("⚠️ 警告：建造模式已开启，但射线击穿了地面！请检查地面的 Layer 是否为 Ground，以及地面是否有 Collider。");
            }
        }
    }
    
    void ToggleBuildMode(bool state)
    {
        isBuildMode = state;
        if (isBuildMode)
        {
            if (currentPreview == null) currentPreview = Instantiate(previewPrefab);
            currentPreview.SetActive(true);
        }
        else
        {
            if (currentPreview != null) currentPreview.SetActive(false);
        }
    }

    void HandlePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            // 1. 获取鼠标所在的格子索引
            GridManager.Instance.GetXY(hit.point, out currentGridX, out currentGridY);
            
            // 2. 吸附到格子中心
            currentPreview.transform.position = GridManager.Instance.GetWorldPosition(currentGridX, currentGridY);
            
            // 3. 检查是否可建造并变色
            canPlaceCurrently = GridManager.Instance.CanPlaceBuilding(currentGridX, currentGridY);
            previewMaterial.color = canPlaceCurrently ? colorValid : colorInvalid;
        }
    }

    void HandleClickToBuild()
    {
        // 鼠标左键点击，且当前位置合法
        if (Input.GetMouseButtonDown(0) && canPlaceCurrently)
        {
            // 发送命令给服务器：我要在这里建东西！
            CmdPlaceBuilding(currentGridX, currentGridY);
            
            // 建造后可选退出建造模式
            // ToggleBuildMode(false); 
        }
    }

    // --- 网络命令 ---
    [Command]
    void CmdPlaceBuilding(int x, int y)
    {
        // 服务器做最后的二次验证，防止作弊或延迟冲突
        if (GridManager.Instance.CanPlaceBuilding(x, y))
        {
            // 1. 标记网格占用 (同步给所有人)
            GridManager.Instance.ServerPlaceBuilding(x, y);

            // 2. 生成真正的建筑
            Vector3 spawnPos = GridManager.Instance.GetWorldPosition(x, y);
            GameObject newBuilding = Instantiate(actualBuildingPrefab, spawnPos, Quaternion.identity);
            
            // 3. 通过 Mirror 广播给所有客户端
            NetworkServer.Spawn(newBuilding);
        }
    }
}
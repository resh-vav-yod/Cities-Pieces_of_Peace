using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// 玩家建造控制。
/// 负责本地建造预览、服务器生成建筑、玩家经济。
/// 当前版本：建造建筑也会受到无线电塔低血量造成的输入延迟影响。
/// </summary>
public class PlayerBuilder : NetworkBehaviour
{
    [Header("配置")]
    public LayerMask groundLayer;
    public GameObject previewPrefab;
    public GameObject actualBuildingPrefab;

    [Header("材质颜色")]
    public Material previewMaterial;
    public Color colorValid = new Color(0f, 1f, 0f, 0.5f);
    public Color colorInvalid = new Color(1f, 0f, 0f, 0.5f);

    [Header("经济")]
    public int startingCredits = 500;
    public int buildingCost = 100;

    [SyncVar]
    public int credits;

    [Header("通信延迟")]
    public float damagedTowerDelayThreshold = 0.5f;
    public float delayedCommandSeconds = 3f;

    private GameObject currentPreview;
    private bool isBuildMode = false;
    private int currentGridX;
    private int currentGridY;
    private bool canPlaceCurrently;
    private bool pendingBuildCommand = false;

    public bool IsBuildMode => isBuildMode;

    [SyncVar]
    public Color myTeamColor;

    public static PlayerBuilder LocalPlayer { get; private set; }

    private static int colorIndex = 0;
    private static readonly Color[] teamColors =
    {
        Color.blue,
        Color.red,
        Color.yellow,
        Color.green
    };

    public override void OnStartServer()
    {
        if (myTeamColor.a <= 0.01f)
        {
            myTeamColor = teamColors[colorIndex % teamColors.Length];
            colorIndex++;
        }

        // 服务器初始化玩家资金。
        // 如果你发现 UI 一直是 0，优先检查 startingCredits 是否在 Inspector 里被设成 0。
        if (credits <= 0)
            credits = startingCredits;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayer = this;
    }

    private void OnDestroy()
    {
        if (LocalPlayer == this)
            LocalPlayer = null;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Camera.main == null || GridManager.Instance == null)
            return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode(!isBuildMode);
        }

        if (isBuildMode)
        {
            HandlePreview();
            HandleClickToBuild();
        }
    }

    private void ToggleBuildMode(bool state)
    {
        isBuildMode = state;

        if (isBuildMode)
        {
            if (currentPreview == null)
                currentPreview = Instantiate(previewPrefab);

            currentPreview.SetActive(true);
        }
        else
        {
            if (currentPreview != null)
                currentPreview.SetActive(false);
        }
    }

    private void HandlePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            return;

        GridManager.Instance.GetXY(hit.point, out currentGridX, out currentGridY);

        Vector3 previewPos = GridManager.Instance.GetWorldPosition(currentGridX, currentGridY);
        currentPreview.transform.position = previewPos;

        bool gridValid = GridManager.Instance.CanPlaceBuilding(currentGridX, currentGridY);
        bool visionValid = NetworkVisionUtility.IsPointVisibleToTeam(previewPos, myTeamColor, true);
        bool moneyValid = credits >= buildingCost;
        bool noPending = !pendingBuildCommand;

        canPlaceCurrently = gridValid && visionValid && moneyValid && noPending;

        if (previewMaterial != null)
            previewMaterial.color = canPlaceCurrently ? colorValid : colorInvalid;
    }

    private void HandleClickToBuild()
    {
        if (Input.GetMouseButtonDown(0) && canPlaceCurrently)
        {
            CmdPlaceBuilding(currentGridX, currentGridY);
        }
    }

    /// <summary>
    /// 客户端请求建造。
    /// 服务器收到后，如果通信塔低血量，则延迟 3 秒再真正执行。
    /// </summary>
    [Command]
    private void CmdPlaceBuilding(int x, int y)
    {
        if (pendingBuildCommand)
            return;

        float delay = BattleSignalUtility.GetManualCommandDelayForTeam(
            myTeamColor,
            damagedTowerDelayThreshold,
            delayedCommandSeconds
        );

        if (delay > 0f)
        {
            pendingBuildCommand = true;
            StartCoroutine(ServerDelayedPlaceBuilding(x, y, delay));
            Debug.Log($"[PlayerBuilder] 通信塔受损，建造命令延迟 {delay} 秒执行。");
            return;
        }

        ServerTryPlaceBuilding(x, y);
    }

    [Server]
    private IEnumerator ServerDelayedPlaceBuilding(int x, int y, float delay)
    {
        yield return new WaitForSeconds(delay);

        ServerTryPlaceBuilding(x, y);
        pendingBuildCommand = false;
    }

    /// <summary>
    /// 服务器实际建造逻辑。
    /// 延迟结束后也会重新验证钱、视野、格子，避免延迟期间状态变化造成问题。
    /// </summary>
    [Server]
    private void ServerTryPlaceBuilding(int x, int y)
    {
        if (GridManager.Instance == null)
            return;

        if (actualBuildingPrefab == null)
        {
            Debug.LogError("[PlayerBuilder] actualBuildingPrefab 未设置。");
            return;
        }

        Vector3 spawnPos = GridManager.Instance.GetWorldPosition(x, y);

        if (!NetworkVisionUtility.IsPointVisibleToTeam(spawnPos, myTeamColor, true))
        {
            Debug.Log("[PlayerBuilder] 建造失败：目标格子不在友方视野范围内。");
            return;
        }

        if (!GridManager.Instance.CanPlaceBuilding(x, y))
        {
            Debug.Log("[PlayerBuilder] 建造失败：目标格子已被占用或越界。");
            return;
        }

        if (!ServerTrySpendCredits(buildingCost))
        {
            Debug.Log("[PlayerBuilder] 建造失败：资金不足。");
            return;
        }

        GridManager.Instance.ServerPlaceBuilding(x, y);

        GameObject newBuilding = Instantiate(actualBuildingPrefab, spawnPos, Quaternion.identity);

        BuildingControl buildingControl = newBuilding.GetComponent<BuildingControl>();
        if (buildingControl != null)
        {
            buildingControl.teamColor = myTeamColor;
            buildingControl.gridX = x;
            buildingControl.gridY = y;
            buildingControl.hasGridPosition = true;
        }

        NetworkVisionSource vision = newBuilding.GetComponent<NetworkVisionSource>();
        if (vision != null)
        {
            vision.teamColor = myTeamColor;
        }

        NetworkServer.Spawn(newBuilding, connectionToClient);

        if (buildingControl != null)
            buildingControl.ServerInitBuilding(myTeamColor, x, y);

        if (vision != null)
            vision.ServerSetTeam(myTeamColor);
    }

    [Server]
    public bool ServerTrySpendCredits(int amount)
    {
        if (amount <= 0)
            return true;

        if (credits < amount)
            return false;

        credits -= amount;
        return true;
    }

    [Server]
    public void ServerAddCredits(int amount)
    {
        if (amount <= 0)
            return;

        credits += amount;
        Debug.Log($"[PlayerBuilder] 获得资金：+{amount}，当前资金={credits}");
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// 建筑控制脚本。
/// 负责阵营颜色、点击生产、生产单位、记录网格坐标、死亡释放格子。
/// 当前版本：生产单位也会受到无线电塔低血量造成的输入延迟影响。
/// </summary>
public class BuildingControl : NetworkBehaviour
{
    [Header("生产")]
    public GameObject unitPrefab;

    [Header("经济")]
    public int unitCost = 50;

    [Header("通信延迟")]
    public float damagedTowerDelayThreshold = 0.5f;
    public float delayedCommandSeconds = 3f;

    [Header("阵营")]
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color teamColor;

    [Header("网格坐标")]
    [SyncVar]
    public int gridX = -1;

    [SyncVar]
    public int gridY = -1;

    [SyncVar]
    public bool hasGridPosition = false;

    [Header("组件")]
    public NetworkHealth health;
    public NetworkVisionSource visionSource;

    private bool gridReleased = false;
    private bool pendingProduceCommand = false;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<NetworkHealth>();

        if (visionSource == null)
            visionSource = GetComponent<NetworkVisionSource>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (health != null)
            health.ServerDied += HandleBuildingDied;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (health != null)
            health.ServerDied -= HandleBuildingDied;
    }

    [Server]
    public void ServerInitBuilding(Color ownerColor, int placedGridX, int placedGridY)
    {
        teamColor = ownerColor;

        gridX = placedGridX;
        gridY = placedGridY;
        hasGridPosition = true;
        gridReleased = false;

        if (visionSource != null)
            visionSource.ServerSetTeam(ownerColor);
    }

    [Server]
    private void HandleBuildingDied(NetworkHealth deadHealth)
    {
        if (gridReleased)
            return;

        if (hasGridPosition && GridManager.Instance != null)
        {
            GridManager.Instance.ServerRemoveBuilding(gridX, gridY);
            gridReleased = true;

            Debug.Log($"[BuildingControl] 建筑死亡，释放格子：{gridX}, {gridY}");
        }
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r.transform.name.Contains("HpBar") ||
                r.transform.name.Contains("Background") ||
                r.transform.name.Contains("Fill"))
            {
                continue;
            }

            r.material.color = newColor;
        }
    }

    private void OnMouseDown()
    {
        Debug.Log("🖱️ 点击到了建筑。");

        if (isOwned)
        {
            if (SimpleUIManager.Instance != null)
            {
                SimpleUIManager.Instance.ShowProductionMenu(this);
            }
            else
            {
                Debug.LogError("[BuildingControl] 找不到 SimpleUIManager。");
            }
        }
        else
        {
            Debug.LogWarning("[BuildingControl] 这不是你的建筑，不能打开生产菜单。");
        }
    }

    /// <summary>
    /// UI 按钮调用。
    /// 服务器收到生产请求后，如果通信塔低血量，则延迟执行。
    /// </summary>
    [Command]
    public void CmdProduceUnit()
    {
        if (pendingProduceCommand)
            return;

        PlayerBuilder ownerPlayer = GetOwnerPlayer();

        if (ownerPlayer == null)
        {
            Debug.LogWarning("[BuildingControl] 找不到建筑拥有者，无法生产。");
            return;
        }

        float delay = BattleSignalUtility.GetManualCommandDelayForTeam(
            teamColor,
            damagedTowerDelayThreshold,
            delayedCommandSeconds
        );

        if (delay > 0f)
        {
            pendingProduceCommand = true;
            StartCoroutine(ServerDelayedProduceUnit(ownerPlayer, delay));
            Debug.Log($"[BuildingControl] 通信塔受损，生产命令延迟 {delay} 秒执行。");
            return;
        }

        ServerTryProduceUnit(ownerPlayer);
    }

    [Server]
    private IEnumerator ServerDelayedProduceUnit(PlayerBuilder ownerPlayer, float delay)
    {
        yield return new WaitForSeconds(delay);

        ServerTryProduceUnit(ownerPlayer);
        pendingProduceCommand = false;
    }

    [Server]
    private PlayerBuilder GetOwnerPlayer()
    {
        if (connectionToClient != null && connectionToClient.identity != null)
        {
            PlayerBuilder owner = connectionToClient.identity.GetComponent<PlayerBuilder>();
            if (owner != null)
                return owner;
        }

        PlayerBuilder[] players = FindObjectsByType<PlayerBuilder>(FindObjectsSortMode.None);

        foreach (PlayerBuilder player in players)
        {
            if (player != null && UnitAI.IsSameTeam(player.myTeamColor, teamColor))
                return player;
        }

        return null;
    }

    [Server]
    private void ServerTryProduceUnit(PlayerBuilder ownerPlayer)
    {
        if (ownerPlayer == null)
            return;

        if (unitPrefab == null)
        {
            Debug.LogError("[BuildingControl] unitPrefab 未设置。");
            return;
        }

        if (!ownerPlayer.ServerTrySpendCredits(unitCost))
        {
            Debug.Log("[BuildingControl] 生产失败：资金不足。");
            return;
        }

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float spawnDistance = Random.Range(3.5f, 5.5f);

        Vector3 randomPos = transform.position + new Vector3(
            randomDir.x * spawnDistance,
            0f,
            randomDir.y * spawnDistance
        );

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas))
        {
            GameObject newUnit = Instantiate(unitPrefab, hit.position, Quaternion.identity);

            UnitAI unitAI = newUnit.GetComponent<UnitAI>();
            if (unitAI != null)
            {
                unitAI.teamColor = teamColor;
            }

            NetworkVisionSource unitVision = newUnit.GetComponent<NetworkVisionSource>();
            if (unitVision != null)
            {
                unitVision.teamColor = teamColor;
            }

            NetworkServer.Spawn(newUnit);

            if (unitAI != null)
                unitAI.ServerSetTeam(teamColor);

            if (unitVision != null)
                unitVision.ServerSetTeam(teamColor);
        }
        else
        {
            ownerPlayer.ServerAddCredits(unitCost);
            Debug.LogWarning("[BuildingControl] 周围没有合法 NavMesh 位置，生产失败，已退钱。");
        }
    }
}
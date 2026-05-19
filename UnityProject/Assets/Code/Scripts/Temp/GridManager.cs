using UnityEngine;
using Mirror;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance;

    [Header("网格设置")]
    public int width = 100;
    public int height = 100;
    public float cellSize = 1f;

    [Header("建筑设置")]
    public int buildingWidth = 5;
    public int buildingHeight = 5;

    [Tooltip("建筑生成高度。普通 Cube 以中心为轴时建议 0.5；如果你的建筑 pivot 在底部，就设 0。")]
    public float buildingPlacementY = 0.5f;

    // 网格左下角起点。100x100 平面中心在 0,0 时，左下角就是 (-50, -50)。
    private Vector3 originPosition = new Vector3(-50f, 0f, -50f);

    // true = 已占用；false = 空闲
    private bool[,] gridArray;

    private void Awake()
    {
        Instance = this;
        gridArray = new bool[width, height];
    }

    /// <summary>
    /// 世界坐标转换为网格索引。
    /// </summary>
    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
        y = Mathf.FloorToInt((worldPosition.z - originPosition.z) / cellSize);
    }

    /// <summary>
    /// 网格索引转换为世界坐标。
    /// 注意：这里不再使用 y=2.5，否则建筑会飞起来。
    /// </summary>
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(
            x * cellSize + originPosition.x + cellSize / 2f,
            buildingPlacementY,
            y * cellSize + originPosition.z + cellSize / 2f
        );
    }

    public bool IsValidIndex(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    /// <summary>
    /// 检查 5x5 建筑范围是否可以放置。
    /// </summary>
    public bool CanPlaceBuilding(int startX, int startY)
    {
        for (int x = startX; x < startX + buildingWidth; x++)
        {
            for (int y = startY; y < startY + buildingHeight; y++)
            {
                if (!IsValidIndex(x, y) || gridArray[x, y])
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 服务器：占用建筑格子。
    /// </summary>
    [Server]
    public void ServerPlaceBuilding(int startX, int startY)
    {
        if (!CanPlaceBuilding(startX, startY))
            return;

        SetBuildingArea(startX, startY, true);
        RpcSetBuildingArea(startX, startY, true);
    }

    /// <summary>
    /// 服务器：释放建筑格子。
    /// 建筑被摧毁时调用。
    /// </summary>
    [Server]
    public void ServerRemoveBuilding(int startX, int startY)
    {
        SetBuildingArea(startX, startY, false);
        RpcSetBuildingArea(startX, startY, false);
    }

    /// <summary>
    /// 服务器/客户端共用：设置一片 5x5 区域是否占用。
    /// </summary>
    private void SetBuildingArea(int startX, int startY, bool occupied)
    {
        for (int x = startX; x < startX + buildingWidth; x++)
        {
            for (int y = startY; y < startY + buildingHeight; y++)
            {
                if (IsValidIndex(x, y))
                    gridArray[x, y] = occupied;
            }
        }
    }

    /// <summary>
    /// 客户端同步格子占用/释放。
    /// </summary>
    [ClientRpc]
    private void RpcSetBuildingArea(int startX, int startY, bool occupied)
    {
        SetBuildingArea(startX, startY, occupied);
    }
}
using UnityEngine;
using Mirror;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance;

    [Header("网格设置")]
    public int width = 100;
    public int height = 100;
    public float cellSize = 1f;
    
    // 网格的左下角起点（对应 100x100 的平面，中心在 0,0）
    private Vector3 originPosition = new Vector3(-50f, 0, -50f); 
    
    // 逻辑数组，true 表示已占用，false 表示空闲
    private bool[,] gridArray;

    private void Awake()
    {
        Instance = this;
        gridArray = new bool[width, height];
    }

    // 核心工具 1：将世界坐标转换为网格索引 (x, y)
    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
        y = Mathf.FloorToInt((worldPosition.z - originPosition.z) / cellSize);
    }

    // 核心工具 2：将网格索引转换回世界坐标（取格子中心点）
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, 0, y) * cellSize + originPosition + new Vector3(cellSize / 2, 0, cellSize / 2);
    }

    // 检查索引是否越界
    public bool IsValidIndex(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    // 检查是否可以放置（未越界且未被占用）
    public bool CanPlaceBuilding(int x, int y)
    {
        if (!IsValidIndex(x, y)) return false;
        return !gridArray[x, y];
    }

    // --- 网络同步部分 ---

    // 服务器调用：标记格子被占用
    [Server]
    public void ServerPlaceBuilding(int x, int y)
    {
        if (IsValidIndex(x, y))
        {
            gridArray[x, y] = true;
            RpcUpdateGrid(x, y); // 通知所有客户端更新他们的本地数组
        }
    }

    // 客户端接收：更新本地网格状态
    [ClientRpc]
    private void RpcUpdateGrid(int x, int y)
    {
        if (IsValidIndex(x, y))
        {
            gridArray[x, y] = true;
        }
    }
}
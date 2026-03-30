using UnityEngine;
using Mirror;

public class BuildingControl : NetworkBehaviour
{
    public GameObject unitPrefab; // 拖入你的胶囊体小兵预制体

    // 在 BuildingControl.cs 中加入这个同步变量
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color teamColor;

    // 当颜色从服务器同步过来时，改变建筑自身的材质颜色
    void OnColorChanged(Color oldC, Color newC)
    {
        GetComponent<MeshRenderer>().material.color = newC;
    }

    /*
    // 只有拥有权限（也就是造它的那个玩家）点击，才会触发
    void OnMouseDown()
    {
        if (isOwned) 
        {
            Debug.Log("点击了自己的建筑，呼出生产菜单！");
            // 通知 UI 显示，并把当前建筑的引用传过去
            SimpleUIManager.Instance.ShowProductionMenu(this);
        }
        else
        {
            Debug.Log("这是别人的建筑或无权限！");
        }
    }
    */
    void OnMouseDown()
    {
        // 只要你的鼠标真真切切地点到了这个模型，这句绝对会输出！
        Debug.Log("🖱️ 物理点击成功！(说明 Collider 没问题)");

        if (isOwned) 
        {
            Debug.Log("✅ 权限验证通过！正在呼出 UI...");
            if (SimpleUIManager.Instance == null)
            {
                Debug.LogError("🚨 糟了，找不到 SimpleUIManager 实例！请检查场景里有没有挂载这个脚本的物体。");
            }
            else
            {
                SimpleUIManager.Instance.ShowProductionMenu(this);
            }
        }
        else
        {
            Debug.LogWarning("❌ 权限失败！鼠标点到了，但 isOwned 为 false。说明服务器没有把这个建筑分配给你。");
        }
    }

    // 由 UI 按钮调用的生产指令 (必须是 Command，向服务器申请)
    [Command]
    public void CmdProduceUnit()
    {
        // 【新增】随机偏移坐标，防止小兵挤在同一个点！
        Vector2 randomOffset = Random.insideUnitCircle * 3f;
        // 在建筑旁边生成小兵 (X轴偏移个 5 米，高度 1 米防止卡地里)
        Vector3 spawnPos = transform.position + new Vector3(5f, 1f, 0);
        GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        
        // 【新增】把建筑的颜色传给生出来的小兵
        newUnit.GetComponent<UnitAI>().teamColor = this.teamColor;
        
        // 生成并把小兵的控制权也给你
        NetworkServer.Spawn(newUnit, connectionToClient); 
    }
}
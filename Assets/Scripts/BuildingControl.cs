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
       // 1. 【核心修改】只取随机方向，不取内部的点
        Vector2 randomDir = Random.insideUnitCircle.normalized; 
        
        // 2. 强制把距离设定在建筑外围（比如 3.5 米 到 5.5 米 之间的圆环地带）
        float spawnDistance = Random.Range(3.5f, 5.5f); 
        
        // 3. 计算出最终的目标点
        Vector3 randomPos = transform.position + new Vector3(randomDir.x * spawnDistance, 0, randomDir.y * spawnDistance);

        // 2. 【核心修复】让 AI 导航网格去判定这个点能不能站人！
        UnityEngine.AI.NavMeshHit hit;
        // 在 randomPos 附近 5 米内，找一个绝对合法的、不重叠的地面坐标
        if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            // 用系统找到的合法坐标 hit.position 生成小兵
            GameObject newUnit = Instantiate(unitPrefab, hit.position, Quaternion.identity);
            
            // 传递颜色
            newUnit.GetComponent<UnitAI>().teamColor = this.teamColor;

            // 3. 【极度关键】小兵是由服务器 AI 控制的，绝对不要把控制权(connectionToClient)给客户端！
            // 删掉逗号后面的参数，让服务器拥有绝对的支配权！
            NetworkServer.Spawn(newUnit); 
        }
        else
        {
            Debug.LogWarning("周围没有合法的空地，生成失败！");
        }
    }

    /*
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
    */
}
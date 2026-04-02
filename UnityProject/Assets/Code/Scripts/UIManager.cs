using UnityEngine;
using Mirror;

public class SimpleUIManager : MonoBehaviour
{
    public static SimpleUIManager Instance;

    public GameObject productionMenu; // 拖入你的 Panel
    private BuildingControl currentSelectedBuilding; // 记录当前选中了哪个建筑

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // 如果按下了 ESC 键，并且当前 UI 是打开的
        if (Input.GetKeyDown(KeyCode.Escape) && productionMenu.activeSelf)
        {
            CloseProductionMenu();
        }
    }
    
    public void CloseProductionMenu()
    {
        currentSelectedBuilding = null;
        productionMenu.SetActive(false);
    }

    // 显示菜单并绑定建筑
    public void ShowProductionMenu(BuildingControl building)
    {
        currentSelectedBuilding = building;
        productionMenu.SetActive(true);
    }

    // 绑定给 UI 按钮的 OnClick 事件
    public void ClickProduceButton()
    {
        if (currentSelectedBuilding != null)
        {
            currentSelectedBuilding.CmdProduceUnit(); // 让当前选中的建筑去产兵
            //productionMenu.SetActive(false);          // 产完关闭 UI (可选)
        }
    }

    public void ReturnToEarth()
    {
        // 只有房主(服务器)有权限切换场景
        if (NetworkServer.active)
        {
            // 场景名必须和 Build Settings 里的地球场景名一模一样！
            NetworkManager.singleton.ServerChangeScene("Earth"); 
        }
        else
        {
            Debug.Log("只有房主可以带大家返回地球！");
        }
    }
}
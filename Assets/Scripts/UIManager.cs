using UnityEngine;

public class SimpleUIManager : MonoBehaviour
{
    public static SimpleUIManager Instance;

    public GameObject productionMenu; // 拖入你的 Panel
    private BuildingControl currentSelectedBuilding; // 记录当前选中了哪个建筑

    private void Awake()
    {
        Instance = this;
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
            productionMenu.SetActive(false);          // 产完关闭 UI (可选)
        }
    }
}
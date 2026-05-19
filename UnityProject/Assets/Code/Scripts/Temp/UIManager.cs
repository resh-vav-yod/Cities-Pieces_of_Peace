using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Temp 战斗场景 UI 管理器。
/// 负责建筑生产菜单显示/隐藏。
/// 当前版本禁止中途主动退出 Battle。
/// </summary>
public class SimpleUIManager : MonoBehaviour
{
    public static SimpleUIManager Instance;

    [Header("生产菜单")]
    public GameObject productionMenu;

    [Header("返回按钮")]
    public GameObject returnToEarthButton;
    public bool allowManualReturnToEarth = false;

    private BuildingControl currentSelectedBuilding;

    private void Awake()
    {
        Instance = this;

        if (productionMenu != null)
            productionMenu.SetActive(false);

        if (returnToEarthButton != null && !allowManualReturnToEarth)
            returnToEarthButton.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseProductionMenu();
            return;
        }

        HandleClickOutsideProductionMenu();
    }

    public void ShowProductionMenu(BuildingControl building)
    {
        currentSelectedBuilding = building;

        if (productionMenu != null)
            productionMenu.SetActive(true);
    }

    public void CloseProductionMenu()
    {
        currentSelectedBuilding = null;

        if (productionMenu != null)
            productionMenu.SetActive(false);
    }

    public void ClickProduceButton()
    {
        if (currentSelectedBuilding == null)
            return;

        currentSelectedBuilding.CmdProduceUnit();
    }

    private void HandleClickOutsideProductionMenu()
    {
        if (productionMenu == null || !productionMenu.activeSelf)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = Camera.main;

        if (cam == null)
        {
            CloseProductionMenu();
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            BuildingControl clickedBuilding = hit.collider.GetComponentInParent<BuildingControl>();

            if (clickedBuilding != null && clickedBuilding == currentSelectedBuilding)
                return;
        }

        CloseProductionMenu();
    }

    public void ReturnToEarth()
    {
        Debug.LogWarning("[SimpleUIManager] 当前版本不允许中途退出 Battle。请摧毁无线电塔或完成战斗结果。");
    }
}
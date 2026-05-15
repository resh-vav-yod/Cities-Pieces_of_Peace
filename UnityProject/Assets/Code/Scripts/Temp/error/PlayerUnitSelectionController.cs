using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerUnitSelectionController : MonoBehaviour
{
    [Header("Refs")]
    public Camera mainCamera;

    [Header("Player")]
    public int playerTeamId = 0;

    [Header("Raycast")]
    public LayerMask selectableMask = ~0;
    public LayerMask commandMask = ~0;
    public float rayDistance = 1000f;

    [Header("Formation")]
    public float formationSpacing = 1.2f;

    private readonly List<CommandableUnit> selectedUnits = new List<CommandableUnit>();

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
            return;

        if (IsPointerOverUI())
            return;

        if (Input.GetMouseButtonDown(0))
            TrySelect();

        if (Input.GetMouseButtonDown(1))
            TryCommand();
    }

    private void TrySelect()
    {
        bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, selectableMask))
        {
            if (!additive)
                ClearSelection();

            return;
        }

        CommandableUnit unit = hit.collider.GetComponentInParent<CommandableUnit>();

        if (unit == null || unit.TeamId != playerTeamId)
        {
            if (!additive)
                ClearSelection();

            return;
        }

        if (!additive)
            ClearSelection();

        AddSelection(unit);
    }

    private void TryCommand()
    {
        CleanupSelectionList();

        if (selectedUnits.Count == 0)
            return;

        if (!BattleCommandAuthority.ManualControlEnabled)
        {
            Debug.Log("[PlayerUnitSelectionController] 无线电通信失效，玩家无法继续手动指挥单位。");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, commandMask))
            return;

        Damageable target = hit.collider.GetComponentInParent<Damageable>();

        if (target != null && target.IsAlive && target.teamId != playerTeamId)
        {
            foreach (CommandableUnit unit in selectedUnits)
                unit.Attack(target);

            return;
        }

        Vector3 basePosition = hit.point;

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            Vector3 formationPosition = GetFormationPosition(basePosition, i, selectedUnits.Count);
            selectedUnits[i].MoveTo(formationPosition);
        }
    }

    private Vector3 GetFormationPosition(Vector3 center, int index, int count)
    {
        if (count <= 1)
            return center;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int row = index / columns;
        int col = index % columns;

        float offsetX = (col - (columns - 1) * 0.5f) * formationSpacing;
        float offsetZ = (row - (columns - 1) * 0.5f) * formationSpacing;

        return center + new Vector3(offsetX, 0f, offsetZ);
    }

    private void AddSelection(CommandableUnit unit)
    {
        if (selectedUnits.Contains(unit))
            return;

        selectedUnits.Add(unit);
        unit.SetSelected(true);
    }

    private void ClearSelection()
    {
        foreach (CommandableUnit unit in selectedUnits)
        {
            if (unit != null)
                unit.SetSelected(false);
        }

        selectedUnits.Clear();
    }

    private void CleanupSelectionList()
    {
        selectedUnits.RemoveAll(unit => unit == null);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
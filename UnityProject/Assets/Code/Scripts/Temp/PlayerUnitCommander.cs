using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerUnitCommander : NetworkBehaviour
{
    [Header("引用")]
    public PlayerBuilder playerBuilder;
    public Camera mainCamera;

    [Header("射线")]
    public LayerMask raycastMask = ~0;
    public float rayDistance = 1000f;

    [Header("编队")]
    public float formationSpacing = 1.2f;

    [Header("框选")]
    public float dragThreshold = 8f;

    private readonly List<UnitAI> selectedUnits = new List<UnitAI>();

    private Vector2 dragStartPos;
    private Vector2 dragCurrentPos;
    private bool isDragging;

    private void Awake()
    {
        if (playerBuilder == null)
            playerBuilder = GetComponent<PlayerBuilder>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (playerBuilder != null && playerBuilder.IsBuildMode)
            return;

        HandleSelectionInput();
        HandleCommandInput();
    }

    private void HandleSelectionInput()
    {
        if (IsPointerOverUI())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Input.mousePosition;
            dragCurrentPos = dragStartPos;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            dragCurrentPos = Input.mousePosition;

            if (Vector2.Distance(dragStartPos, dragCurrentPos) >= dragThreshold)
                isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (isDragging)
                SelectUnitsInDragBox(additive);
            else
                TrySelectSingleUnit(additive);

            isDragging = false;
        }
    }

    private void HandleCommandInput()
    {
        if (IsPointerOverUI())
            return;

        if (!Input.GetMouseButtonDown(1))
            return;

        CleanupSelection();

        if (selectedUnits.Count == 0)
            return;

        if (!BattleCommandAuthority.ManualControlEnabled)
        {
            Debug.Log("[PlayerUnitCommander] 无线电通信失效，无法继续手动指挥。");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, raycastMask))
            return;

        NetworkedRadioTower tower = hit.collider.GetComponentInParent<NetworkedRadioTower>();
        if (tower != null && tower.netIdentity != null)
        {
            foreach (UnitAI unit in selectedUnits)
            {
                if (unit != null && unit.netIdentity != null)
                    CmdAttackRadioTower(unit.netIdentity.netId, tower.netIdentity.netId);
            }

            return;
        }

        UnitAI targetUnit = hit.collider.GetComponentInParent<UnitAI>();
        if (targetUnit != null && playerBuilder != null && !UnitAI.IsSameTeam(targetUnit.teamColor, playerBuilder.myTeamColor))
        {
            foreach (UnitAI unit in selectedUnits)
            {
                if (unit != null && unit.netIdentity != null && targetUnit.netIdentity != null)
                    CmdAttackUnit(unit.netIdentity.netId, targetUnit.netIdentity.netId);
            }

            return;
        }

        Vector3 destination = hit.point;

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            UnitAI unit = selectedUnits[i];

            if (unit == null || unit.netIdentity == null)
                continue;

            Vector3 offsetDestination = GetFormationPosition(destination, i, selectedUnits.Count);
            CmdMoveUnit(unit.netIdentity.netId, offsetDestination);
        }
    }

    private void TrySelectSingleUnit(bool additive)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, raycastMask))
        {
            if (!additive)
                ClearSelection();

            return;
        }

        UnitAI unit = hit.collider.GetComponentInParent<UnitAI>();

        if (unit == null || playerBuilder == null)
        {
            if (!additive)
                ClearSelection();

            return;
        }

        if (!UnitAI.IsSameTeam(unit.teamColor, playerBuilder.myTeamColor))
        {
            if (!additive)
                ClearSelection();

            return;
        }

        if (!additive)
            ClearSelection();

        AddSelection(unit);
    }

    private void SelectUnitsInDragBox(bool additive)
    {
        if (!additive)
            ClearSelection();

        Rect selectionRect = GetScreenRect(dragStartPos, dragCurrentPos);
        UnitAI[] allUnits = FindObjectsByType<UnitAI>(FindObjectsSortMode.None);

        foreach (UnitAI unit in allUnits)
        {
            if (unit == null || playerBuilder == null)
                continue;

            if (!UnitAI.IsSameTeam(unit.teamColor, playerBuilder.myTeamColor))
                continue;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPos.z < 0)
                continue;

            Vector2 guiPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);

            if (selectionRect.Contains(guiPoint))
                AddSelection(unit);
        }
    }

    [Command]
    private void CmdMoveUnit(uint unitNetId, Vector3 destination)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (!TryGetOwnedUnitOnServer(unitNetId, out UnitAI unit))
            return;

        unit.ServerSetMoveOrder(destination);
    }

    [Command]
    private void CmdAttackUnit(uint unitNetId, uint targetNetId)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (!TryGetOwnedUnitOnServer(unitNetId, out UnitAI unit))
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            return;

        UnitAI target = targetIdentity.GetComponent<UnitAI>();

        if (target == null)
            return;

        if (UnitAI.IsSameTeam(target.teamColor, playerBuilder.myTeamColor))
            return;

        unit.ServerSetAttackUnitOrder(target);
    }

    [Command]
    private void CmdAttackRadioTower(uint unitNetId, uint towerNetId)
    {
        if (!BattleCommandAuthority.ManualControlEnabled)
            return;

        if (!TryGetOwnedUnitOnServer(unitNetId, out UnitAI unit))
            return;

        if (!NetworkServer.spawned.TryGetValue(towerNetId, out NetworkIdentity towerIdentity))
            return;

        NetworkedRadioTower tower = towerIdentity.GetComponent<NetworkedRadioTower>();

        if (tower == null)
            return;

        unit.ServerSetAttackRadioTowerOrder(tower);
    }

    [Server]
    private bool TryGetOwnedUnitOnServer(uint unitNetId, out UnitAI unit)
    {
        unit = null;

        if (playerBuilder == null)
            return false;

        if (!NetworkServer.spawned.TryGetValue(unitNetId, out NetworkIdentity identity))
            return false;

        unit = identity.GetComponent<UnitAI>();

        if (unit == null)
            return false;

        if (!UnitAI.IsSameTeam(unit.teamColor, playerBuilder.myTeamColor))
            return false;

        return true;
    }

    private void AddSelection(UnitAI unit)
    {
        if (unit == null)
            return;

        if (selectedUnits.Contains(unit))
            return;

        selectedUnits.Add(unit);
        unit.SetSelectedLocal(true);
    }

    private void ClearSelection()
    {
        foreach (UnitAI unit in selectedUnits)
        {
            if (unit != null)
                unit.SetSelectedLocal(false);
        }

        selectedUnits.Clear();
    }

    private void CleanupSelection()
    {
        selectedUnits.RemoveAll(unit => unit == null);
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

    private Rect GetScreenRect(Vector2 screenPosition1, Vector2 screenPosition2)
    {
        Vector2 p1 = new Vector2(screenPosition1.x, Screen.height - screenPosition1.y);
        Vector2 p2 = new Vector2(screenPosition2.x, Screen.height - screenPosition2.y);

        Vector2 topLeft = Vector2.Min(p1, p2);
        Vector2 bottomRight = Vector2.Max(p1, p2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    private void OnGUI()
    {
        if (!isLocalPlayer)
            return;

        if (!isDragging)
            return;

        Rect rect = GetScreenRect(dragStartPos, dragCurrentPos);
        GUI.Box(rect, "");
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
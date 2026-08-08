using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static TacticalQuery;


public class InputHandler : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private HexGrid _grid;

    [Header("Units Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private UnitEventSO _policeSelectedEvent;
    [SerializeField] private GameEventSO _policeDeselectedEvent;
    [SerializeField] private ActionEventSO _actionSelectedEvent;
    [SerializeField] private ItemEventSO _itemSelectedEvent;

    [Header("UI Events")]
    [SerializeField] private StringEventSO _alertEvent;
    [SerializeField] private GameEventSO _endTurnButtonClickedEvent;
    [SerializeField] private ActionEventSO _actionButtonClickedEvent;

    private bool _isExecutingAction = false;
    private bool _isValid;

    private TurnManager _turnManager;
    private InputSystem_Actions _inputSystem;

    private PoliceRuntime _lastAttackedPolice;
    private SpezzoneRuntime _selectedSpezzone;

    private ActionType _selectedAction = ActionType.None;
    private ItemSO _selectedItem;

    private HexCell _pendingDestination;
    private PoliceRuntime _pendingTarget;

    //Costanti utili per la gestione dello stato seduto in modo che se volessi aggiungere un'azione non dovrei riscriverla più volte
    private const string SeatedAlert = "you are sitting, can only stand up or chant";
    private static readonly ActionType SeatedActions =
    ActionType.None | ActionType.SitStand | ActionType.Chant;

    //Check for blocking player input, for example when the police are executing their turn or when an action is being executed
    private bool CanAcceptPlayerInput =>
    !_isExecutingAction
    && _lvlManager != null && _lvlManager.IsGameActive
    && _turnManager != null && !_turnManager.IsPoliceTurn;

    private void Awake()
    {
        _inputSystem = new InputSystem_Actions();

        if (_lvlManager == null || _actionButtonClickedEvent == null
            || _endTurnButtonClickedEvent == null || _itemSelectedEvent == null)
        {
            Debug.LogWarning("InputHandler: riferimenti mancanti");
            return;
        }

        _turnManager = _lvlManager.TurnManager;
        _isValid = true;
    }

    private void OnEnable()
    {
        if(!_isValid) return;

        _inputSystem.UI.LeftClick.performed += OnLeftClick;
        _inputSystem.UI.RightClick.performed += OnRightClick;

        _actionButtonClickedEvent.Subscribe(OnActionButtonClicked);
        _endTurnButtonClickedEvent.Subscribe(OnEndTurnButtonClicked);

        _inputSystem.Game.EndTurn.performed += OnEndTurn;
        _inputSystem.Game.Charge.performed += OnChargeKey;
        _inputSystem.Game.Throw.performed += OnThrowKey;
        _inputSystem.Game.Barricade.performed += OnBarricadeKey;
        _inputSystem.Game.Chant.performed += OnChantKey;
        _inputSystem.Game.SitStand.performed += OnSitStandKey;

        _itemSelectedEvent.Subscribe(OnItemSelected);

        _inputSystem.Enable();
    }

    private void OnDisable()
    {
        if(!_isValid) return;

        _inputSystem.UI.LeftClick.performed -= OnLeftClick;
        _inputSystem.UI.RightClick.performed -= OnRightClick;

        _actionButtonClickedEvent.Unsubscribe(OnActionButtonClicked);
        _endTurnButtonClickedEvent.Unsubscribe(OnEndTurnButtonClicked);


        _inputSystem.Game.EndTurn.performed -= OnEndTurn;
        _inputSystem.Game.Charge.performed -= OnChargeKey;
        _inputSystem.Game.Throw.performed -= OnThrowKey;
        _inputSystem.Game.Barricade.performed -= OnBarricadeKey;
        _inputSystem.Game.Chant.performed -= OnChantKey;
        _inputSystem.Game.SitStand.performed -= OnSitStandKey;

        _itemSelectedEvent.Unsubscribe(OnItemSelected);

        _inputSystem.Disable();
    }

    private void OnLeftClick(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;

        if (IsPointerOverUI()) return;

        Vector2 screenPos = _inputSystem.Game.MousePosition.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        HexCoordinates clickCoordinates = _grid.WorldToGrid(worldPos);

        HexCell clickCell;
        _grid.TryGetCell(clickCoordinates, out clickCell);
        if (clickCell == null)
        {
            Debug.Log($"No cell at {clickCoordinates}");
            return;
        }


        if (_selectedAction != ActionType.None)
        {
            HandleActionClick(clickCell);
            return;
        }
        // Stato: nessuno selezionato
        if (_selectedSpezzone == null)
        {
            if (clickCell.OccupiedBy is SpezzoneRuntime spezzone)
            {
                _policeDeselectedEvent?.Raise();
                _selectedSpezzone = spezzone;
                _unitSelectedEvent?.Raise(_selectedSpezzone);
                AuraBonus aura = TacticalQuery.GetAuraBonus(_selectedSpezzone, _grid);
            }
            else if (clickCell.OccupiedBy is PoliceRuntime police)
            {
                _policeSelectedEvent?.Raise(police);
            }
            return;
        }

        // Seduto: nessuna azione implicita (movimento/scontro) mentre non stai già rialzandoti
        if (_selectedSpezzone.IsSeated)
        {
            _alertEvent?.Raise(SeatedAlert);
            return;
        }

        // Stato: spezzone selezionato
        if (_pendingDestination != null)
        {
            if (clickCell == _pendingDestination)
            {
                ConfirmMovement();
            }
            else
            {
                _pendingDestination = null;
                TrySetPendingDestination(clickCell);
            }
            return;
        }
        // Stato: spezzone selezionato, c'è un pending bersaglio 
        if (_pendingTarget != null)
        {
            if (clickCell.OccupiedBy == _pendingTarget)
            {
                Debug.Log("Attack confirmed, executing");
                ConfirmAttack();
            }
            else
            {
                _pendingTarget = null;
                _policeDeselectedEvent?.Raise();
                Debug.Log("Attack cancelled");
            }
            return;
        }

        // Stato: spezzone selezionato, nessun pending 
        if (clickCell.OccupiedBy == null)
        {
            TrySetPendingDestination(clickCell);
        }
        else if (clickCell.OccupiedBy is PoliceRuntime police)
        {
            _pendingTarget = police;
            _policeSelectedEvent?.Raise(police);
            FlipSelectedUnit(_grid.GridToWorld(police.PositionCell.Coordinates));
            Debug.Log($"Target marked: {clickCell.Coordinates}");
        }
        else if (clickCell.OccupiedBy is SpezzoneRuntime other)
        {
            _selectedSpezzone = other;
            SetSelectedAction(ActionType.None);
            _unitSelectedEvent?.Raise(_selectedSpezzone);

        }
    }

    private void OnRightClick(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;

        // if in action mode, cancel the action and keep the spezzone selected
        if (_selectedAction != ActionType.None)
        {
            SetSelectedAction(ActionType.None);
            _pendingDestination = null;
            _pendingTarget = null;
            _policeDeselectedEvent?.Raise();
            return;
        }

        _selectedSpezzone = null;
        _pendingDestination = null;
        _pendingTarget = null;
        _unitDeselectedEvent?.Raise();
        _policeDeselectedEvent?.Raise();
    }

    private void OnEndTurn(InputAction.CallbackContext ctx)
    {
        TryEndTurn();
    }

    private void OnEndTurnButtonClicked()
    {
        TryEndTurn();
    }

    private void TryEndTurn()
    {
        if (_isExecutingAction) return;
        if (_lvlManager == null || !_lvlManager.IsGameActive) return;

        _selectedSpezzone = null;
        SetSelectedAction(ActionType.None);
        _pendingDestination = null;
        _pendingTarget = null;
        _unitDeselectedEvent?.Raise();

        if (_turnManager != null)
        {
            _turnManager.EndTurn();
        }
    }

    private void FlipSelectedUnit(Vector3 targetWorldPos)
    {
        if (_selectedSpezzone == null) return;

        GameObject go = _lvlManager.Renderer.GetGameObject(_selectedSpezzone);
        if (go == null) return;

        UnitMovement movement = go.GetComponent<UnitMovement>();
        if (movement == null) return;

        movement.FlipTowards(targetWorldPos);
    }

    private void ConfirmMovement()
    {
        _isExecutingAction = true;

        HexCoordinates start = _selectedSpezzone.PositionCell.Coordinates;
        HexCoordinates end = _pendingDestination.Coordinates;

        List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(start, end, _grid);

        if (pathCoords.Count == 0)
        {
            _alertEvent?.Raise("No path found");
            _pendingDestination = null;
            _isExecutingAction = false;
            return;
        }

        List<HexCell> path = new List<HexCell>();
        for (int i = 1; i < pathCoords.Count; i++)
        {
            if (_grid.TryGetCell(pathCoords[i], out HexCell cell))
                path.Add(cell);
        }

        // USA IL NUOVO ExecuteMovement CON CALLBACK
        bool success = _turnManager.ExecuteMovement(_selectedSpezzone, path, () =>
        {
            OnActionComplete();
        });

        if (!success)
        {
            Debug.Log("Movement not executed");
        }
    }

    private void ConfirmAttack()
    {
        _lastAttackedPolice = _pendingTarget;
        HexCoordinates atkCoord = _selectedSpezzone.PositionCell.Coordinates;
        HexCoordinates defCoord = _pendingTarget.PositionCell.Coordinates;
        int distance = atkCoord.Distance(defCoord);


        bool success;
        if (distance == 1)
        {
            _isExecutingAction = true;
            _turnManager.StartSkirmish(_selectedSpezzone, _pendingTarget, () => OnActionComplete());
            return;
        }
        else
        {
            var option = TacticalQuery.GetAttackOption(atkCoord, defCoord, _selectedSpezzone.ActionPoints, _grid);

            if (!option.IsValid)
            {
                _alertEvent?.Raise("No path to target");
                success = false;
            }
            else
            {
                List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(atkCoord, option.MoveDestination, _grid);
                List<HexCell> path = new List<HexCell>();
                for (int i = 1; i < pathCoords.Count; i++)
                {
                    if (_grid.TryGetCell(pathCoords[i], out HexCell cell))
                        path.Add(cell);
                }

                success = true;
                _isExecutingAction = true;
                _turnManager.ExecuteMovement(_selectedSpezzone, path, () =>
                {
                    if (_pendingTarget != null && _pendingTarget.IsAlive)
                        _turnManager.StartSkirmish(_selectedSpezzone, _pendingTarget, () => OnActionComplete());
                    else
                        OnActionComplete();
                });
                return;
            }
        }
        if (!success)
        {
            Debug.Log("Attack not executed");
            OnActionComplete();
            return;
        }

        OnActionComplete();

    }

    private void TrySetPendingDestination(HexCell cell)
    {
        var reachable = TacticalQuery.GetReachable(
            _selectedSpezzone.PositionCell.Coordinates, _selectedSpezzone.ActionPoints, _grid);

        if (!reachable.ContainsKey(cell.Coordinates))
        {
            _alertEvent?.Raise("Out of range");
            return;
        }

        _pendingDestination = cell;
        FlipSelectedUnit(_grid.GridToWorld(cell.Coordinates));
    }
    private void OnChargeKey(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;

        SetSelectedAction(_selectedAction == ActionType.Charge ? ActionType.None : ActionType.Charge);
        _pendingDestination = null;
        _pendingTarget = null;
    }

    private void OnThrowKey(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;
        SetSelectedAction(_selectedAction == ActionType.Throw ? ActionType.None : ActionType.Throw);
        _pendingDestination = null;
        _pendingTarget = null;
    }

    private void OnBarricadeKey(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;
        SetSelectedAction(_selectedAction == ActionType.Barricade ? ActionType.None : ActionType.Barricade);
        _pendingDestination = null;
        _pendingTarget = null;
    }

    private void OnChantKey(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;
        SetSelectedAction(_selectedAction == ActionType.Chant ? ActionType.None : ActionType.Chant);
        _pendingDestination = null;
        _pendingTarget = null;
    }

    private void OnSitStandKey(InputAction.CallbackContext ctx)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;
        SetSelectedAction(_selectedAction == ActionType.SitStand ? ActionType.None : ActionType.SitStand);
        _pendingDestination = null;
        _pendingTarget = null;
    }

    private void OnItemSelected(ItemSO item)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;
        _selectedItem = item;
        SetSelectedAction(item.Action);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
    private void OnActionButtonClicked(ActionType action)
    {
        if (!CanAcceptPlayerInput) return;
        if (_selectedSpezzone == null) return;
        SetSelectedAction(_selectedAction == action ? ActionType.None : action);
        _pendingDestination = null;
        _pendingTarget = null;
    }

    private void OnActionComplete()
    {
        if (_selectedSpezzone != null && !_selectedSpezzone.IsAlive)
        {
            _selectedSpezzone = null;
            SetSelectedAction(ActionType.None);
            _unitDeselectedEvent?.Raise();
        }
        else if (_selectedSpezzone != null && _selectedSpezzone.ActionPoints <= 0)
        {

            _selectedSpezzone = null;
            SetSelectedAction(ActionType.None);
            _unitDeselectedEvent?.Raise();
        }
        else if (_selectedSpezzone != null)
        {
            _unitSelectedEvent?.Raise(_selectedSpezzone);
        }

        if (_lastAttackedPolice != null)
        {
            if (!_lastAttackedPolice.IsAlive)
                _policeDeselectedEvent?.Raise();
            else
            {
                _policeSelectedEvent?.Raise(_lastAttackedPolice);
            }
            _lastAttackedPolice = null;
        }
        _pendingDestination = null;
        _pendingTarget = null;
        _isExecutingAction = false;
    }
    private void HandleActionClick(HexCell clickCell)
    {
        var validTargets = TacticalQuery.GetValidTargets(
            _selectedSpezzone, _selectedAction, _selectedItem, _lvlManager.Map);

        if (!validTargets.Contains(clickCell.Coordinates))
        {
            _alertEvent?.Raise(DescribeInvalidTarget(clickCell));
            return;
        }

        _isExecutingAction = true;
        switch (_selectedAction)
        {
            case ActionType.Charge:
                PoliceRuntime chargeTarget = clickCell.OccupiedBy as PoliceRuntime;
                _lastAttackedPolice = chargeTarget;
                SetSelectedAction(ActionType.None);
                _turnManager.StartCharge(_selectedSpezzone, chargeTarget, () => OnActionComplete());
                return;

            case ActionType.Throw:
                if (_selectedItem == null)
                {
                    _alertEvent?.Raise("select a throw object");
                    _isExecutingAction = false;
                    return;
                }
                PoliceRuntime police = clickCell.OccupiedBy as PoliceRuntime;
                _turnManager.ExecuteThrow(_selectedSpezzone, police, _selectedItem as ThrowItemSO);
                _lastAttackedPolice = police;
                break;

            case ActionType.Barricade:
                if (_selectedItem == null)
                {
                    _alertEvent?.Raise("Select a Barricade");
                    _isExecutingAction = false;
                    return;
                }
                bool placed = _turnManager.ExecuteBarricade(_selectedSpezzone, clickCell, _selectedItem as BarricadeSO);
                if (!placed) { _isExecutingAction = false; return; }
                break;

            case ActionType.Chant:
                _turnManager.ExecuteChant(_selectedSpezzone);
                break;

            case ActionType.SitStand:
                _turnManager.ExecuteSitStand(_selectedSpezzone);
                break;
        }
        SetSelectedAction(ActionType.None);
        OnActionComplete();
    }

    private string DescribeInvalidTarget(HexCell clickCell)
    {
        if (_selectedSpezzone == null) return "No unit selected";

        int ap = _selectedSpezzone.ActionPoints;

        switch (_selectedAction)
        {
            case ActionType.SitStand:
                int sitCost = TacticalQuery.GetSitStandCost(_selectedSpezzone);
                if (ap < sitCost) return $"Not enough AP, {sitCost} needed";
                return "Click on your own unit";

            case ActionType.Chant:
                if (ap < TacticalQuery.ChantCost) return $"Not enough AP, {TacticalQuery.ChantCost} needed";
                return "Click on your own unit";

            case ActionType.Charge:
                if (ap < TacticalQuery.ChargeCost) return $"Not enough AP, {TacticalQuery.ChargeCost} needed";
                if (clickCell.OccupiedBy is not PoliceRuntime chargeTarget) return "No target there";
                if (chargeTarget.IsSeated) return "That unit is sitting: cannot be charged";
                return "No clear charge line to that target";

            case ActionType.Throw:
                if (_selectedItem is not ThrowItemSO) return "Select a throw object";
                if (!_selectedSpezzone.Inventory.HasItem(_selectedItem)) return "No throw objects left";
                if (ap < _selectedItem.ActionPointCost) return $"Not enough AP, {_selectedItem.ActionPointCost} needed";
                if (clickCell.OccupiedBy is not PoliceRuntime) return "No target there";
                if (_selectedSpezzone.PositionCell.Coordinates.Distance(clickCell.Coordinates) != TacticalQuery.ThrowRange)
                    return "Target out of range";
                return "No clear throw path";

            case ActionType.Barricade:
                if (_selectedItem is not BarricadeSO) return "Select a barricade";
                if (!_selectedSpezzone.Inventory.HasItem(_selectedItem)) return "No barricades left";
                if (ap < _selectedItem.ActionPointCost) return $"Not enough AP, {_selectedItem.ActionPointCost} needed";
                if (clickCell.Type.IsObjective) return "Cannot barricade an objective";
                return "Not a free adjacent cell";
        }

        return "Not a valid target";
    }
    private void SetSelectedAction(ActionType action)
    {
        bool allowedWhileSeated = (SeatedActions & action) != 0 || action == ActionType.None;

        if (!allowedWhileSeated && _selectedSpezzone != null && _selectedSpezzone.IsSeated)
        {
            _alertEvent?.Raise(SeatedAlert);
            return;
        }

        if (action != ActionType.None
            && _selectedSpezzone != null
            && !_selectedSpezzone.CanPerformAction(action))
        {
            _alertEvent?.Raise("this unit can not doing this action");
            return;
        }
        _selectedAction = action;

        if (_selectedItem != null && _selectedItem.Action != action)
            _selectedItem = null;

        _actionSelectedEvent?.Raise(action);
    }
}

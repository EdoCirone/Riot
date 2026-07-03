using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputHandler : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private HexGrid _grid;

    [Header("Units Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private ActionEventSO _actionSelectedEvent;


    private bool _isExecutingAction = false;

    private TurnManager _turnManager;
    private InputSystem_Actions _inputSystem;

    private SpezzoneRuntime _selectedSpezzone;
    private ActionType _selectedAction = ActionType.None;
    private HexCell _pendingDestination;
    private PoliceRuntime _pendingTarget;

    private void Awake()
    {
        _inputSystem = new InputSystem_Actions();

        if (_lvlManager == null)
        {
            Debug.LogWarning("LVL manager not assigned in InputHandler");
            return;
        }

        _turnManager = _lvlManager.TurnManager;
    }

    private void OnEnable()
    {
        _inputSystem.UI.LeftClick.performed += OnLeftClick;
        _inputSystem.UI.RightClick.performed += OnRightClick;
        _inputSystem.Game.EndTurn.performed += OnEndTurn;
        _inputSystem.Game.Charge.performed += OnChargeKey;
        _inputSystem.Game.Throw.performed += OnThrowKey;
        _inputSystem.Enable();
    }

    private void OnDisable()
    {
        _inputSystem.UI.LeftClick.performed -= OnLeftClick;
        _inputSystem.UI.RightClick.performed -= OnRightClick;
        _inputSystem.Game.EndTurn.performed -= OnEndTurn;
        _inputSystem.Game.Charge.performed -= OnChargeKey;
        _inputSystem.Game.Throw.performed -= OnThrowKey;
        _inputSystem.Disable();
    }

    private void OnLeftClick(InputAction.CallbackContext ctx)
    {
        if (_isExecutingAction) return;

        if (_lvlManager == null || !_lvlManager.IsGameActive) return;

        Vector2 screenPos = _inputSystem.Game.MousePosition.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        HexCoordinates clickCoordinates = HexCoordinates.FromWorldPosition(worldPos, _grid.CellSize);

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
                _selectedSpezzone = spezzone;
                _unitSelectedEvent?.Raise(_selectedSpezzone);
                Debug.Log($"Selezionato spezzone su {clickCell.Coordinates}");
            }
            return;
        }

        // Stato: spezzone selezionato
        if (_pendingDestination != null)
        {
            if (clickCell == _pendingDestination)
            {
                Debug.Log("Destinazione confermata eseguo");
                ConfirmMovement();
            }
            else
            {
                _pendingDestination = null;
                Debug.Log("Movimento annullato");
            }
            return;
        }

        // Stato: spezzone selezionato, c'è un pending bersaglio 
        if (_pendingTarget != null)
        {
            if (clickCell.OccupiedBy == _pendingTarget)
            {
                Debug.Log("Attacco confermato,Eseguo");
                ConfirmAttack();
            }
            else
            {
                _pendingTarget = null;
                Debug.Log("Attacco annullato");
            }
            return;
        }

        // Stato: spezzone selezionato, nessun pending 
        if (clickCell.OccupiedBy == null)
        {
            if (!clickCell.Type.IsWalkable)
            {
                Debug.Log("Cella non percorribile");
                return;
            }
            _pendingDestination = clickCell;

            Vector3 cellWorldPos = _grid.transform.position + clickCell.Coordinates.ToWorldPosition(_grid.CellSize);
            FlipSelectedUnit(cellWorldPos);


            Debug.Log($"Destinazione marcata: {clickCell.Coordinates}");
        }
        else if (clickCell.OccupiedBy is PoliceRuntime police)
        {
            _pendingTarget = police;

            FlipSelectedUnit(_grid.transform.position + police.PositionCell.Coordinates.ToWorldPosition(_grid.CellSize));
            Debug.Log($"Bersaglio marcato: {clickCell.Coordinates}");
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
        if (_isExecutingAction) return;

        // if in action mode, cancel the action and keep the spezzone selected
        if (_selectedAction != ActionType.None)
        {
            SetSelectedAction(ActionType.None);
            _pendingDestination = null;
            _pendingTarget = null;
            Debug.Log("Azione annullata, spezzone ancora selezionato");
            return;
        }

        _selectedSpezzone = null;
        _pendingDestination = null;
        _pendingTarget = null;
        _unitDeselectedEvent?.Raise();
        Debug.Log("Selezione annullata");
    }

    private void OnEndTurn(InputAction.CallbackContext ctx)
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
            Debug.Log("Nessun percorso trovato verso la destinazione");
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
            OnActionComplete();
            Debug.Log("Movimento non eseguito");
        }
    }

    private void ConfirmAttack()
    {
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
            // Muovi verso il bersaglio e poi attacca
            HexCoordinates? bestAdjacent = _turnManager.FindBestAdjacentCell(atkCoord, defCoord);
            if (bestAdjacent == null)
            {
                Debug.Log("Nessuna cella adiacente libera al bersaglio");
                success = false;
            }
            else
            {
                List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(atkCoord, bestAdjacent.Value, _grid);
                if (pathCoords.Count == 0)
                {
                    Debug.Log("Nessun percorso verso il bersaglio");
                    success = false;
                }
                else
                {
                    List<HexCell> path = new List<HexCell>();
                    for (int i = 1; i < pathCoords.Count; i++)
                    {
                        if (_grid.TryGetCell(pathCoords[i], out HexCell cell))
                            path.Add(cell);
                    }

                    int moveCost = path.Count;
                    if (_selectedSpezzone.ActionPoints < moveCost + 1)
                    {
                        Debug.Log("PA insufficienti per muoversi e attaccare");
                        success = false;
                    }
                    else
                    {
                        success = true;
                        _isExecutingAction = true;
                        _turnManager.ExecuteMovement(_selectedSpezzone, path, () =>
                        {
                            if (_pendingTarget != null && _pendingTarget.Status != UnitsStatus.Disperse)
                                _turnManager.StartSkirmish(_selectedSpezzone, _pendingTarget, () => OnActionComplete());
                            else
                                OnActionComplete();
                        });
                        return;
                    }
                }
            }
        }

        if (!success)
        {
            Debug.Log("Attacco non eseguito");
            OnActionComplete();
            return;
        }

        OnActionComplete();

    }

    private void OnActionComplete()
    {
        // Se lo spezzone è disperso, deseleziona tutto
        if (_selectedSpezzone != null && _selectedSpezzone.Status == UnitsStatus.Disperse)
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
            // Mantieni la selezione sullo spezzone
            _unitSelectedEvent?.Raise(_selectedSpezzone);
        }

        _pendingDestination = null;
        _pendingTarget = null;
        _isExecutingAction = false;
    }

    private void OnChargeKey(InputAction.CallbackContext ctx)
    {
        if (_isExecutingAction) return;
        if (_selectedSpezzone == null) return;

        SetSelectedAction(_selectedAction == ActionType.Charge ? ActionType.None : ActionType.Charge);
        _pendingDestination = null;
        _pendingTarget = null;
        Debug.Log($"Azione selezionata: {_selectedAction}");
    }

    private void OnThrowKey(InputAction.CallbackContext ctx)
    {
        if (_isExecutingAction) return;
        if (_selectedSpezzone == null) return;
        SetSelectedAction(_selectedAction == ActionType.Throw ? ActionType.None : ActionType.Throw);
        _pendingDestination = null;
        _pendingTarget = null;
        Debug.Log($"Azione selezionata: {_selectedAction}");
    }

    private void HandleActionClick(HexCell clickCell)
    {
        var validTargets = TacticalQuery.GetValidTargets(
            _selectedSpezzone.PositionCell.Coordinates, _selectedSpezzone.ActionPoints,
            _selectedAction, _lvlManager.Map);

        if (!validTargets.Contains(clickCell.Coordinates))
        {
            Debug.Log("Bersaglio non valido");
            return;
        }

        PoliceRuntime police = clickCell.OccupiedBy as PoliceRuntime;
        if (police == null)
        {
            Debug.LogWarning("Cella valida ma senza police — incoerenza");
            return;
        }

        _isExecutingAction = true;
        switch (_selectedAction)
        {
            case ActionType.Charge: _turnManager.ExecuteCharge(_selectedSpezzone, police); break;
            case ActionType.Throw: _turnManager.ExecuteThrow(_selectedSpezzone, police); break;
        }
        SetSelectedAction(ActionType.None);
        OnActionComplete();
    }


    private void SetSelectedAction(ActionType action)
    {
        _selectedAction = action;
        _actionSelectedEvent?.Raise(action);
    }
}

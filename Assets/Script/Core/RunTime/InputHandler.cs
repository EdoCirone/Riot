using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputHandler : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private HexGrid _grid;

    [Header("UI Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;

    private TurnManager _turnManager;
    private InputSystem_Actions _inputSystem;

    private SpezzoneRuntime _selectedSpezzone;
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
        _inputSystem.Enable();
    }

    private void OnDisable()
    {
        _inputSystem.UI.LeftClick.performed -= OnLeftClick;
        _inputSystem.UI.RightClick.performed -= OnRightClick;
        _inputSystem.Game.EndTurn.performed -= OnEndTurn;
        _inputSystem.Disable();
    }

    private void OnLeftClick(InputAction.CallbackContext ctx)
    {
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
            Debug.Log($"Destinazione marcata: {clickCell.Coordinates}");
        }
        else if (clickCell.OccupiedBy is PoliceRuntime police)
        {
            _pendingTarget = police;
            Debug.Log($"Bersaglio marcato: {clickCell.Coordinates}");
        }
        else if (clickCell.OccupiedBy is SpezzoneRuntime other)
        {
            _selectedSpezzone = other;
            _unitSelectedEvent?.Raise(_selectedSpezzone);
            Debug.Log($"Cambiata selezione su {clickCell.Coordinates}");
        }
    }

    private void OnRightClick(InputAction.CallbackContext ctx)
    {
        _selectedSpezzone = null;
        _pendingDestination = null;
        _pendingTarget = null;
        _unitDeselectedEvent?.Raise();
        Debug.Log("Selezione annullata");
    }

    private void OnEndTurn(InputAction.CallbackContext ctx)
    {
        if (_lvlManager == null || !_lvlManager.IsGameActive) return;

        if (_turnManager != null)
        {

            _turnManager.EndTurn();

        }
    }

    private void ConfirmMovement()
    {
        HexCoordinates start = _selectedSpezzone.PositionCell.Coordinates;
        HexCoordinates end = _pendingDestination.Coordinates;

        List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(start, end, _grid);

        if (pathCoords.Count == 0)
        {
            Debug.Log("Nessun percorso trovato verso la destinazione");
            _pendingDestination = null;
            return;
        }

        // FindPath include anche la cella di partenza: la togliamo, il costo si conta solo sulle celle da percorrere
        List<HexCell> path = new List<HexCell>();
        for (int i = 1; i < pathCoords.Count; i++)
        {
            if (_grid.TryGetCell(pathCoords[i], out HexCell cell))
                path.Add(cell);
        }

        bool success = _turnManager.ExecuteMovement(_selectedSpezzone, path);
        if (!success)
        {

            Debug.Log("Movimento non eseguito");
        }

        if (success)
            _unitSelectedEvent?.Raise(_selectedSpezzone);

        _pendingDestination = null;
    }

    private void ConfirmAttack()
    {
        HexCoordinates atkCoord = _selectedSpezzone.PositionCell.Coordinates;
        HexCoordinates defCoord = _pendingTarget.PositionCell.Coordinates;
        int distance = atkCoord.Distance(defCoord);

        bool success;
        if (distance == 1)
        {
            success = _turnManager.ExecuteSkirmish(_selectedSpezzone, _pendingTarget);
        }
        else if (distance == 3)
        {
            success = _turnManager.ExecuteCharge(_selectedSpezzone, _pendingTarget);
        }
        else
        {
            Debug.Log("Distanza non valida per nessuna azione");
            success = false;
        }

        if (!success)
        {
            Debug.Log("Attacco non eseguito");
        }

        if (success && _selectedSpezzone.Status != UnitsStatus.Disperse)
            _unitSelectedEvent?.Raise(_selectedSpezzone);
        else if (_selectedSpezzone.Status == UnitsStatus.Disperse)
        {
            _selectedSpezzone = null;
            _unitDeselectedEvent?.Raise();
        }

        _pendingTarget = null;
    }
}

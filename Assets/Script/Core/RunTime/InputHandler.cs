using UnityEngine;
using UnityEngine.InputSystem;


public class InputHandler : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager; 
    [SerializeField] private HexGrid _grid;

    private TurnManager _turnManager;
    private InputSystem_Actions _inputSystem;
    private SpezzoneRuntime _selectedSpezzone;

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
        _inputSystem.UI.Click.performed += OnClick;
        _inputSystem.Game.EndTurn.performed += OnEndTurn;
        _inputSystem.Enable();
    }

    private void OnDisable()
    {
        _inputSystem.UI.Click.performed -= OnClick;
        _inputSystem.Game.EndTurn.performed -= OnEndTurn;
        _inputSystem.Disable();
    }

    private void OnClick(InputAction.CallbackContext ctx)
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

        if (_selectedSpezzone == null)
        {
            if (clickCell.OccupiedBy is SpezzoneRuntime spezzone)
            {
                _selectedSpezzone = spezzone;
                Debug.Log($"Selezionato spezzone su {clickCell.Coordinates}");
            }

            else
            {
                Debug.Log("Select a Spezzone");
            }
        }
        else
        {
            if (clickCell.OccupiedBy == null)
            {
                if (!clickCell.Type.IsWalkable)
                {
                    Debug.Log("NotWalkable Cell");
                    return;
                }

                int distance = _selectedSpezzone.PositionCell.Coordinates.Distance(clickCell.Coordinates);
                if (distance > _selectedSpezzone.Mov)
                    Debug.Log("Casella irraggiungibile — troppo distante");
                else
                {
                    _turnManager.AddMovementOrder(new MovementOrder(_selectedSpezzone, clickCell));
                    _selectedSpezzone = null;
                }
            }
            else if (clickCell.OccupiedBy is PoliceRuntime police)
            {
                if (_selectedSpezzone.PositionCell.Coordinates.Distance(clickCell.Coordinates) == 1)
                {
                    _turnManager.AddAttackOrder(new AttackOrder(_selectedSpezzone, police));
                    _selectedSpezzone = null;
                }
            }
            else if (clickCell.OccupiedBy is SpezzoneRuntime other)
            {
                _selectedSpezzone = other;
            }
        }

    }

    private void OnEndTurn(InputAction.CallbackContext ctx)
    {
        if (_lvlManager == null || !_lvlManager.IsGameActive) return;

        if (_turnManager != null)
        {

            _turnManager.ExecuteResolution();

        }
    }
}

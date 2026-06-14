using UnityEngine;
using UnityEngine.InputSystem;


public class InputHandler : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private InputSystem_Actions _inputSystem;
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private HexGrid _grid;

    private SpezzoneRuntime _selectedSpezzone;

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

        Vector2 screenPos = ctx.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));

        HexCoordinates clickCoordinates = HexCoordinates.FromWorldPosition(worldPos, _grid.CellSize);

        HexCell clickCell;

        _grid.TryGetCell(clickCoordinates, out clickCell);

        if (_selectedSpezzone == null)
        {
            if (clickCell.OccupiedBy is SpezzoneRuntime spezzone)
            {
                _selectedSpezzone = spezzone;
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
                _turnManager.AddMovementOrder(new MovementOrder(_selectedSpezzone, clickCell));
                _selectedSpezzone = null;
            }
            else if (clickCell.OccupiedBy is PoliceRuntime police)
            {
                if (_selectedSpezzone.PositionCell.Coordinates.Distance(clickCell.Coordinates) == 1)
                {
                    _turnManager.AddAttackOrder(new AttackOrder(_selectedSpezzone, police));
                    _selectedSpezzone = null;
                }
            }
            else if (clickCell.OccupiedBy is SpezzoneRuntime)
            {
                _selectedSpezzone = (SpezzoneRuntime)clickCell.OccupiedBy;
            }
        }

    }

    private void OnEndTurn(InputAction.CallbackContext ctx)
    {

        if (_turnManager != null)
        {

            _turnManager.ExecuteResolution();

        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class OrderPreviewRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridRenderer _hexGridRenderer;
    [SerializeField] private HexGrid _grid;

    [Header("Colors")]
    [SerializeField] private Color _reachableColor = new(0.3f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color _attackableColor = Color.red;
    [SerializeField] private Color _chargeColor = Color.yellow;
    [SerializeField] private Color _throwColor = new(1f, 0.5f, 0f, 0.8f);
    [SerializeField] private Color _barricadeColor = new(0.55f, 0.35f, 0.15f, 0.8f);
    [SerializeField] private Color _chantColor = new(1f, 0.85f, 0f, 0.8f);
    [SerializeField] private Color _chantAreaColor = new(1f, 0.85f, 0f, 0.35f);
    [SerializeField] private Color _sitStandColor = new(0.6f, 0.6f, 0.6f, 0.8f);

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private ActionEventSO _actionSelectedEvent;
    [SerializeField] private ItemEventSO _itemSelectedEvent;

    private bool _isValid = false;

    private ItemSO _selectedItem;
    private AbstractUnitsRunTime _selectedUnit;

    private ActionType _currentAction = ActionType.None;
    private List<HexCoordinates> _highlightedCells = new();

    private void Awake()
    {
        if (_hexGridRenderer == null
            || _grid == null
            || _unitSelectedEvent == null
            || _unitDeselectedEvent == null
            || _actionSelectedEvent == null
            || _itemSelectedEvent == null)
        {
            Debug.LogWarning("OrderPreviewRenderer: missing References");
            return;
        }

        _isValid = true;
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Subscribe(OnUnitSelected);
        _unitDeselectedEvent.Subscribe(OnUnitDeselected);
        _actionSelectedEvent.Subscribe(OnActionSelected);
        _itemSelectedEvent.Subscribe(OnItemSelected);
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
        _actionSelectedEvent.Unsubscribe(OnActionSelected);
        _itemSelectedEvent.Unsubscribe(OnItemSelected);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        ClearHighlight();
        _currentAction = ActionType.None;
        _selectedItem = null;

        if (unit is not SpezzoneRuntime)
        {
            _selectedUnit = null;
            return;
        }

        _selectedUnit = unit;

        if (unit.IsSeated) return;

        var visited = TacticalQuery.GetReachable(unit.PositionCell.Coordinates, unit.ActionPoints, _grid);
        HighlightReachable(unit, visited);
        HighlightAttackable(unit, visited);
    }

    private void OnUnitDeselected()
    {
        _selectedUnit = null;
        ClearHighlight();
    }

    private void HighlightReachable(AbstractUnitsRunTime unit, Dictionary<HexCoordinates, int> visited)
    {
        HexCoordinates start = unit.PositionCell.Coordinates;
        foreach (HexCoordinates coord in visited.Keys)
        {
            if (coord == start) continue;
            _hexGridRenderer.SetCellColor(coord, _reachableColor);
            _highlightedCells.Add(coord);
        }
    }

    private void HighlightAttackable(AbstractUnitsRunTime unit, Dictionary<HexCoordinates, int> visited)
    {
        if (unit.ActionPoints <= 0) return;
        int budget = unit.ActionPoints;

        foreach (HexCell cell in _grid.GetAllCells())
        {
            if (cell.OccupiedBy is not PoliceRuntime) continue;

            var option = TacticalQuery.GetAttackOption(
                unit.PositionCell.Coordinates, cell.Coordinates, budget, _grid, visited);

            if (option.IsValid)
            {
                _hexGridRenderer.SetCellColor(cell.Coordinates, _attackableColor);
                _highlightedCells.Add(cell.Coordinates);
            }
        }
    }

    private void OnActionSelected(ActionType action)
    {
        _currentAction = action;
        if (_selectedItem != null && _selectedItem.Action != action)
            _selectedItem = null;
        RefreshActionHighlight();
    }
    private void OnItemSelected(ItemSO item)
    {
        _selectedItem = item;

        if (item != null && item.Action == _currentAction)
            RefreshActionHighlight();
    }

    private void RefreshActionHighlight()
    {
        ClearHighlight();

        if (_currentAction == ActionType.None)
        {
            if (_selectedUnit != null && _selectedUnit.IsAlive && !_selectedUnit.IsSeated)
            {
                var visited = TacticalQuery.GetReachable(
                    _selectedUnit.PositionCell.Coordinates, _selectedUnit.ActionPoints, _grid);
                HighlightReachable(_selectedUnit, visited);
                HighlightAttackable(_selectedUnit, visited);
            }
            return;
        }

        if (_selectedUnit == null) return;

        var targets = TacticalQuery.GetValidTargets(_selectedUnit, _currentAction, _selectedItem, _grid);

        Color color = _currentAction switch
        {
            ActionType.Charge => _chargeColor,
            ActionType.Throw => _throwColor,
            ActionType.Barricade => _barricadeColor,
            ActionType.Chant => _chantColor,
            ActionType.SitStand => _sitStandColor,
            _ => _chargeColor
        };

        foreach (var coord in targets)
        {
            _hexGridRenderer.SetCellColor(coord, color);
            _highlightedCells.Add(coord);
        }

        if (_currentAction == ActionType.Chant)
            HighlightChantArea(_selectedUnit.PositionCell.Coordinates);
    }

    private void HighlightChantArea(HexCoordinates from)
    {
        foreach (HexCoordinates n in from.GetNeighbors())
        {
            if (!_grid.TryGetCell(n, out HexCell cell)) continue;
            if (cell.OccupiedBy is not SpezzoneRuntime spezzone) continue;
            if (!spezzone.IsAlive) continue;

            _hexGridRenderer.SetCellColor(n, _chantAreaColor);
            _highlightedCells.Add(n);
        }
    }

    private void ClearHighlight()
    {
        foreach (HexCoordinates coord in _highlightedCells)
            _hexGridRenderer.ResetCellColor(coord);
        _highlightedCells.Clear();
    }
}

using System.Collections.Generic;
using UnityEngine;

public class OrderPreviewRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private HexGridRenderer _hexGridRenderer;
    [SerializeField] private HexGrid _grid;

    [Header("Colors")]
    [SerializeField] private Color _reachableColor = new Color(0.3f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color _attackableColor = Color.red;
    [SerializeField] private Color _chargeColor = Color.yellow;

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private ActionEventSO _actionSelectedEvent;

    private List<HexCoordinates> _highlightedCells = new();
    private AbstractUnitsRunTime _selectedUnit;
    private bool _isValid = false;

    private void Awake()
    {
        if (_hexGridRenderer == null || _grid == null || _turnManager == null ||
       _unitSelectedEvent == null || _unitDeselectedEvent == null)
        {
            Debug.LogWarning("OrderPreviewRenderer: riferimenti mancanti");
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
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
        _actionSelectedEvent.Unsubscribe(OnActionSelected);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        _selectedUnit = unit;
        ClearHighlight();
        var visited = TacticalQuery.GetReachable(
            unit.PositionCell.Coordinates, unit.ActionPoints, _grid);
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
            if (cell.OccupiedBy is not PoliceRuntime police) continue;

            bool skirmish = unit.PositionCell.Coordinates.Distance(cell.Coordinates) == 1
                 && budget >= 1;
            bool charge = _turnManager.CanCharge(unit, police) && budget >= 4;
            bool moveAndAttack = !skirmish && CanMoveAndSkirmish(cell.Coordinates, visited, budget);

            if (skirmish)
            {
                _hexGridRenderer.SetCellColor(cell.Coordinates, _attackableColor);
                _highlightedCells.Add(cell.Coordinates);
            }
            else if (moveAndAttack)
            {
                _hexGridRenderer.SetCellColor(cell.Coordinates, _attackableColor);
                _highlightedCells.Add(cell.Coordinates);
            }
        }
    }

    private void OnActionSelected(ActionType action)
    {
        ClearHighlight();
        if (action == ActionType.None)
        {
            if (_selectedUnit != null && _selectedUnit.Status == UnitsStatus.Alive)
            {
                var visited = TacticalQuery.GetReachable(
                    _selectedUnit.PositionCell.Coordinates, _selectedUnit.ActionPoints, _grid);
                HighlightReachable(_selectedUnit, visited);
                HighlightAttackable(_selectedUnit, visited);
            }
            return;
        }
        if (_selectedUnit == null) return;
        var targets = TacticalQuery.GetValidTargets(
            _selectedUnit.PositionCell.Coordinates, _selectedUnit.ActionPoints, action, _grid);
        foreach (var coord in targets)
        {

            _hexGridRenderer.SetCellColor(coord, _chargeColor);
            _highlightedCells.Add(coord);
        }
    }


    private bool CanMoveAndSkirmish(HexCoordinates policeCoord,
                                Dictionary<HexCoordinates, int> visited,
                                int budget)
    {
        foreach (HexCoordinates n in policeCoord.GetNeighbors())
            if (visited.TryGetValue(n, out int cost) && cost + 1 <= budget)
                return true;
        return false;
    }

    private void ClearHighlight()
    {
        foreach (HexCoordinates coord in _highlightedCells)
            _hexGridRenderer.ResetCellColor(coord);
        _highlightedCells.Clear();
    }
}
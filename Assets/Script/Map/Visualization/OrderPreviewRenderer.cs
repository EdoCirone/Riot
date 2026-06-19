using System.Collections.Generic;
using UnityEngine;

public class OrderPreviewRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridRenderer _hexGridRenderer;
    [SerializeField] private HexGrid _grid;

    [Header("Colors")]
    [SerializeField] private Color _reachableColor = new Color(0.3f, 0.8f, 1f, 0.8f);

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;

    private List<HexCoordinates> _highlightedCells = new();
    private bool _isValid = false;

    private void Awake()
    {
        if (_hexGridRenderer == null || _grid == null ||
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
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        ClearHighlight();
        HighlightReachable(unit);
    }

    private void OnUnitDeselected()
    {
        ClearHighlight();
    }

    private void HighlightReachable(AbstractUnitsRunTime unit)
    {
        // Stesse regole di PathFinder: solo celle walkable e libere
        HexCoordinates start = unit.PositionCell.Coordinates;
        int budget = unit.ActionPoints;

        Dictionary<HexCoordinates, int> visited = new();
        Queue<(HexCoordinates coord, int cost)> queue = new();

        visited[start] = 0;
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (current, cost) = queue.Dequeue();

            foreach (HexCoordinates dir in HexCoordinates.Directions)
            {
                HexCoordinates neighbor = current + dir;
                int newCost = cost + 1;

                if (newCost > budget) continue;
                if (visited.ContainsKey(neighbor)) continue;

                if (!_grid.TryGetCell(neighbor, out HexCell cell)) continue;
                if (!cell.Type.IsWalkable) continue;
                if (cell.OccupiedBy != null) continue;

                visited[neighbor] = newCost;
                queue.Enqueue((neighbor, newCost));
            }
        }

        // Colora tutte le celle raggiungibili tranne la cella di partenza
        foreach (HexCoordinates coord in visited.Keys)
        {
            if (coord == start) continue;
            _hexGridRenderer.SetCellColor(coord, _reachableColor);
            _highlightedCells.Add(coord);
        }
    }

    private void ClearHighlight()
    {
        foreach (HexCoordinates coord in _highlightedCells)
            _hexGridRenderer.ResetCellColor(coord);
        _highlightedCells.Clear();
    }
}
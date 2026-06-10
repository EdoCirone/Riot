using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private HexMapSO _hexMapData;

    [Header("Grid Settings")]
    [SerializeField] private float _cellSize = 1f;

    [Header("Gizmos")]
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _gizmoColor = Color.cyan;

    [Header("Authoring")]
    [SerializeField] private HexTypeSO[] _paintPalette; //for the editor, i can't serialize in CustomEditor so i put it here
    [SerializeField] private HexTypeSO _initDefaultType;

    public HexTypeSO[] PaintPalette => _paintPalette;
    public HexTypeSO InitDefaultType => _initDefaultType;

    public HexMapSO HexMapData => _hexMapData;
    public float CellSize => _cellSize;

    Dictionary<HexCoordinates, HexCell> _cells = new Dictionary<HexCoordinates, HexCell>();

    private void Awake()
    {
        if (_hexMapData == null) return;

        GenerateGrid();
    }


    public void GenerateGrid()
    {
        if (_hexMapData == null) return;
        _cells.Clear();
        for (int col = 0; col < _hexMapData.Width; col++)
        {
            int parity = col & 1;
            for (int row = 0; row < _hexMapData.Height; row++)
            {
                int q = col;
                int r = row - (col - parity) / 2;
                HexCoordinates coords = new HexCoordinates(q, r);
                HexTypeSO type = _hexMapData.GetCellType(col, row);
                _cells[coords] = new HexCell(coords, type);
            }
        }
    }

    public bool IsCellWalkable(HexCoordinates coords)
    {
        if (_cells.TryGetValue(coords, out HexCell cell))
        {
            return cell.Type.IsWalkable;
        }
        return false; // If the cell doesn't exist, consider it not walkable
    }

    public bool TryGetCell(HexCoordinates coords, out HexCell cell)
        => _cells.TryGetValue(coords, out cell);

    public IEnumerable<HexCell> GetAllCells() => _cells.Values;

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;
        if (_hexMapData == null) return;
        if (_hexMapData.Width <= 0 || _hexMapData.Height <= 0 || _cellSize <= 0f) return;

        Gizmos.color = _gizmoColor;

        for (int col = 0; col < _hexMapData.Width; col++)
        {
            int parity = col & 1;
            for (int row = 0; row < _hexMapData.Height; row++)
            {
                int q = col;
                int r = row - (col - parity) / 2;
                HexCoordinates coords = new HexCoordinates(q, r);
                Vector3 center = transform.position + coords.ToWorldPosition(_cellSize);

                HexTypeSO type = _hexMapData.GetCellType(col, row);
                Color cellColor = (type != null && type.Color.a > 0f) ? type.Color : _gizmoColor;
                Gizmos.color = cellColor;
                DrawHexGizmo(center, _cellSize);
            }
        }
    }

    private void DrawHexGizmo(Vector3 center, float size)
    {
        Vector3 prev = HexCorner(center, size, 0);
        for (int i = 1; i <= 6; i++)
        {
            Vector3 next = HexCorner(center, size, i);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private Vector3 HexCorner(Vector3 center, float size, int index)
    {
        float angleRad = Mathf.Deg2Rad * 60f * index;
        return center + new Vector3(size * Mathf.Cos(angleRad), size * Mathf.Sin(angleRad), 0f);
    }
}

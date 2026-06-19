using System.Collections.Generic;
using UnityEngine;

public class HexGridRenderer : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private HexGrid _grid;

    [Header("Type-based visuals")]
    [SerializeField] private HexTypeSO _defaultHexType;

    private Dictionary<HexCoordinates, GameObject> _cellObjects = new();
    private void Start()
    {
        if (_grid == null) return;
        if (_defaultHexType == null) return;

        foreach (HexCell cell in _grid.GetAllCells())
        {
            HexTypeSO type = cell.Type ?? _defaultHexType;

            if (type.Prefab == null) continue;

            GameObject go = Instantiate(type.Prefab,
                 _grid.transform.position + cell.Coordinates.ToWorldPosition(_grid.CellSize),
                 Quaternion.identity, transform);

            _cellObjects[cell.Coordinates] = go;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = type.Color;
        }
    }

    public void SetCellColor(HexCoordinates coords, Color color)
    {
        if (!_cellObjects.TryGetValue(coords, out GameObject go)) return;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    public void ResetCellColor(HexCoordinates coords)
    {
        if (!_cellObjects.TryGetValue(coords, out GameObject go)) return;
        HexCell cell;
        if (!_grid.TryGetCell(coords, out cell)) return;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = cell.Type.Color;
    }

}
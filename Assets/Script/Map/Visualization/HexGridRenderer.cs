using System.Collections.Generic;
using UnityEngine;

public class HexGridRenderer : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private HexGrid _grid;

    [Header("Type-based visuals")]
    [SerializeField] private HexTypeSO _defaultHexType;

    [Header("Objective feedback")]
    [SerializeField] private float _occupiedDarkening = 0.6f;
    [SerializeField] private Color _claimedColor = new Color(0.45f, 1f, 0.45f, 1f);

    [Header("Events")]
    [SerializeField] private GameEventSO _boardChangedEvent;

    private Dictionary<HexCoordinates, GameObject> _cellObjects = new();
    private bool _isValid;

    private void Awake()
    {
        _isValid = _grid != null && _defaultHexType != null;
        if (!_isValid) Debug.LogWarning("Reference missing in HexGridRenderer");
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _boardChangedEvent?.Subscribe(RefreshObjectiveCells);
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _boardChangedEvent?.Unsubscribe(RefreshObjectiveCells);
    }

    private void Start()
    {
        if (!_isValid) return;

        foreach (HexCell cell in _grid.GetAllCells())
        {
            HexTypeSO type = cell.Type ?? _defaultHexType;
            if (type.Prefab == null) continue;

            GameObject go = Instantiate(type.Prefab,
                _grid.GridToWorld(cell.Coordinates), Quaternion.identity, transform);

            _cellObjects[cell.Coordinates] = go;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = BaseColorOf(cell);
        }
    }

    private Color BaseColorOf(HexCell cell)
    {
        HexTypeSO type = cell.Type ?? _defaultHexType;
        Color color = type.Color;

        ObjectiveRuntime objective = cell.Objective;
        if (objective == null) return color;

        if (objective.IsClaimed) return _claimedColor;

        if (cell.OccupiedBy is SpezzoneRuntime spezzone && spezzone.IsAlive)
        {
            float a = color.a;                 // il prodotto scurirebbe anche l'alpha
            color *= _occupiedDarkening;
            color.a = a;
        }

        return color;
    }

    /// <summary>Riallinea il colore delle sole celle obiettivo. Chiamato su BoardChanged,
    /// cioè ovunque cambi posizione o stato di un'unità.</summary>
    private void RefreshObjectiveCells()
    {
        foreach (ObjectiveRuntime objective in _grid.Objectives)
            foreach (HexCell cell in objective.Cells)
            {
                if (!_cellObjects.TryGetValue(cell.Coordinates, out GameObject go)) continue;
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = BaseColorOf(cell);
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
        if (!_grid.TryGetCell(coords, out HexCell cell)) return;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = BaseColorOf(cell);
    }
}

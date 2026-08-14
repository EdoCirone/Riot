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
    [SerializeField] private bool _showObjectiveCoordinates = true;
    [SerializeField] private bool _showObjectiveNames = false;

    [Header("Authoring")]
    [SerializeField] private HexTypeSO[] _paintPalette; //for the editor, i can't serialize in CustomEditor so i put it here
    [SerializeField] private HexTypeSO _initDefaultType;

    public HexTypeSO[] PaintPalette => _paintPalette;
    public HexTypeSO InitDefaultType => _initDefaultType;

    public HexMapSO HexMapData => _hexMapData;
    public float CellSize => _cellSize;

    private Bounds _worldBounds;
    public Bounds WorldBounds => _worldBounds;

    private readonly List<ObjectiveRuntime> _objectives = new List<ObjectiveRuntime>();
   
    public IReadOnlyList<ObjectiveRuntime> Objectives => _objectives;
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

        RecalculateWorldBounds();
        BindObjectives();
    }

    private void RecalculateWorldBounds()
    {
        if (_cells.Count == 0)
        {
            _worldBounds = new Bounds(Vector3.zero, Vector3.zero);
            return;
        }
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var cell in _cells.Values)
        {
            Vector3 worldPos = GridToWorld(cell.Coordinates);
            min = Vector3.Min(min, worldPos);
            max = Vector3.Max(max, worldPos);
        }

        Vector3 padding = new Vector3(_cellSize, _cellSize, 0f);
        min -= padding;
        max += padding;

        _worldBounds = new Bounds((min + max) / 2f, max - min);
    }

    public bool TryGetCell(HexCoordinates coords, out HexCell cell)
        => _cells.TryGetValue(coords, out cell);

    /// <summary>
    /// Coordinata → posizione nel mondo, tenendo conto di dove sta la griglia in scena.
    /// ⚠ È l'UNICO posto dove `transform.position` va sommato. Chi lo fa a mano prima o
    /// poi se lo dimentica: è già successo in UnitsRenderer.UpdateView, ed era invisibile
    /// solo perché la griglia sta a (0,0,0).
    /// </summary>
    public Vector3 GridToWorld(HexCoordinates coordinates)
        => transform.position + coordinates.ToWorldPosition(_cellSize);

    /// <summary>Posizione nel mondo → coordinata. Inversa esatta di GridToWorld.</summary>
    public HexCoordinates WorldToGrid(Vector3 worldPosition)
        => HexCoordinates.FromWorldPosition(worldPosition - transform.position, _cellSize);

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
                Vector3 center = GridToWorld(coords);

                HexTypeSO type = _hexMapData.GetCellType(col, row);
                Color cellColor = (type != null && type.Color.a > 0f) ? type.Color : _gizmoColor;

                // Se la griglia è già stata generata, le celle di un obiettivo si tingono
                // di un colore proprio: il raggruppamento si vede senza doverlo leggere.
                ObjectiveRuntime objective = null;
                if (_cells.TryGetValue(coords, out HexCell generatedCell))
                    objective = generatedCell.Objective;

                if (objective != null)
                    cellColor = ObjectiveGizmoColor(_objectives.IndexOf(objective));

                Gizmos.color = cellColor;
                DrawHexGizmo(center, _cellSize);

#if UNITY_EDITOR
                if (_showObjectiveCoordinates && type != null && type.IsObjectiveGround)
                    UnityEditor.Handles.Label(center, $"{coords.Q},{coords.R}", CoordStyle);

                if (_showObjectiveNames && objective != null && objective.Cells[0] == generatedCell)
                    UnityEditor.Handles.Label(center + Vector3.up * _cellSize * 0.55f,
                                              objective.ToString(), CoordStyle);
#endif
#if UNITY_EDITOR
                // Etichetta Q,R solo sulle celle obiettivo: sono poche, e sono le uniche
                // di cui serve conoscere la coordinata per dichiarare un ObjectiveSO.
                if (_showObjectiveCoordinates && type != null && type.IsObjectiveGround)
                {
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(center, $"{coords.Q},{coords.R}", CoordStyle);
                }
#endif
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

    /// <summary>
    /// Costruisce gli ObjectiveRuntime dalle ancore dichiarate in HexMapSO. Le celle di un
    /// obiettivo sono il gruppo CONNESSO di celle dipinte come obiettivo che contiene
    /// l'ancora: si scrive una coordinata sola per obiettivo.
    /// ⚠ Conseguenza voluta: due obiettivi distinti non possono essere adiacenti, altrimenti
    /// si fonderebbero in uno solo.
    /// </summary>
    private void BindObjectives()
    {
        _objectives.Clear();
        foreach (HexCell cell in _cells.Values) cell.BindObjective(null);

        if (_hexMapData.Objectives == null) return;

        foreach (ObjectiveSO data in _hexMapData.Objectives)
        {
            if (data == null) continue;

            if (!_cells.TryGetValue(data.Anchor, out HexCell anchor))
            {
                Debug.LogError($"[OBJ] {data.name}: anchor {data.Anchor} is outside the map");
                continue;
            }
            if (anchor.Type == null || !anchor.Type.IsObjectiveGround)
            {
                Debug.LogError($"[OBJ] {data.name}: anchor {data.Anchor} is not painted as an objective cell");
                continue;
            }
            if (anchor.IsObjective)
            {
                Debug.LogError($"[OBJ] {data.name}: anchor {data.Anchor} already belongs to {anchor.Objective}: two objectives cannot touch");
                continue;
            }

            List<HexCell> group = FloodObjective(anchor);
            ObjectiveRuntime runtime = new ObjectiveRuntime(data, group);
            foreach (HexCell cell in group) cell.BindObjective(runtime);
            _objectives.Add(runtime);

            Debug.Log($"[OBJ] {data.name}: {group.Count} cell(s) from anchor {data.Anchor}");
        }

        int orphans = 0;
        foreach (HexCell cell in _cells.Values)
            if (cell.Type != null && cell.Type.IsObjectiveGround && !cell.IsObjective) orphans++;

        if (orphans > 0)
            Debug.LogWarning($"[OBJ] {orphans} cell(s) painted as objective belong to NO objective: " +
                             $"they will not score and will not block the push. Add an anchor or repaint them.");
    }

    /// <summary>Gruppo connesso di celle dipinte come obiettivo, a partire dall'ancora.</summary>
    private List<HexCell> FloodObjective(HexCell start)
    {
        List<HexCell> group = new List<HexCell>();
        HashSet<HexCoordinates> seen = new HashSet<HexCoordinates>();
        Queue<HexCell> queue = new Queue<HexCell>();

        queue.Enqueue(start);
        seen.Add(start.Coordinates);

        while (queue.Count > 0)
        {
            HexCell current = queue.Dequeue();
            group.Add(current);

            foreach (HexCoordinates n in current.Coordinates.GetNeighbors())
            {
                if (seen.Contains(n)) continue;
                if (!_cells.TryGetValue(n, out HexCell neighbor)) continue;
                if (neighbor.Type == null || !neighbor.Type.IsObjectiveGround) continue;
                seen.Add(n);
                queue.Enqueue(neighbor);
            }
        }

        return group;
    }

    /// <summary>Un colore distinto per obiettivo, derivato dall'indice. Serve solo ai gizmo.</summary>
    private static Color ObjectiveGizmoColor(int index)
    {
        if (index < 0) return Color.white;
        return Color.HSVToRGB((index * 0.37f) % 1f, 0.75f, 1f);
    }

#if UNITY_EDITOR
    private static GUIStyle _coordStyle;
    private static GUIStyle CoordStyle
    {
        get
        {
            if (_coordStyle == null)
            {
                _coordStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
                _coordStyle.alignment = TextAnchor.MiddleCenter;
                _coordStyle.normal.textColor = Color.white;
            }
            return _coordStyle;
        }
    }
#endif
}

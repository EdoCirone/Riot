using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HexGrid))]
public class HexGridEditor : Editor
{
    private int _selectedTypeIndex = 0;
    private int _initWidth = 10;
    private int _initHeight = 10;
    private HexTypeSO _defaultType;


    private void OnEnable()
    {
        HexGrid hexGrid = (HexGrid)target;
        if (hexGrid == null) return;
        if (hexGrid.HexMapData != null)
        {
            _initWidth = hexGrid.HexMapData.Width;
            _initHeight = hexGrid.HexMapData.Height;
        }
        _defaultType = hexGrid.InitDefaultType;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        HexGrid hexGrid = (HexGrid)target;

        if (GUILayout.Button("Generate Grid"))
        {
            if (hexGrid.HexMapData == null) { Debug.LogError("..."); return; }
            hexGrid.GenerateGrid();
        }


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Initialize Map", EditorStyles.boldLabel);
        _initWidth = EditorGUILayout.IntField("Width", _initWidth);
        _initHeight = EditorGUILayout.IntField("Height", _initHeight);
        _defaultType = (HexTypeSO)EditorGUILayout.ObjectField("Default Type", _defaultType, typeof(HexTypeSO), false);

        if (GUILayout.Button("Initialize Map"))
        {
            if (EditorUtility.DisplayDialog(
                   "Initialize map",
                   "Are you sure? this operation reset the map settings",
                   "Yes", "Cancel"))

                if (hexGrid.HexMapData != null && _defaultType != null)
                {
                    Undo.RecordObject(hexGrid.HexMapData, "Initialize Map");
                    hexGrid.HexMapData.Initialize(_initWidth, _initHeight, _defaultType);
                    EditorUtility.SetDirty(hexGrid.HexMapData);
                    hexGrid.GenerateGrid();
                }
        }

        //My palette
        HexTypeSO[] palette = hexGrid.PaintPalette;
        if (palette != null)
        {
            for (int i = 0; i < palette.Length; i++)
            {
                if (palette[i] == null) continue;
                string label = string.IsNullOrEmpty(palette[i].DisplayType)
                    ? palette[i].name
                    : palette[i].DisplayType;
                if (GUILayout.Button(label))
                    _selectedTypeIndex = i;
            }
        }
    }

    public void OnSceneGUI()
    {
        Event e = Event.current;
        HexGrid hexGrid = (HexGrid)target;


        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            e.Use(); // Consume the event so it doesn't propagate further   

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            Plane plane = new Plane(Vector3.forward, Vector3.zero);

            if (!plane.Raycast(ray, out float distance))
                return;

            Vector3 worldPos = ray.GetPoint(distance);

            // Convert world position to local position relative to the hex grid
            float localX = worldPos.x - hexGrid.transform.position.x;
            float localY = worldPos.y - hexGrid.transform.position.y;

            int col = Mathf.RoundToInt(localX / (hexGrid.CellSize * 1.5f));
            int row = Mathf.RoundToInt(localY / (hexGrid.CellSize * Mathf.Sqrt(3)) - 0.5f * (col & 1));


            if (hexGrid.HexMapData == null) return;
            if (col < 0 || col >= hexGrid.HexMapData.Width) return;
            if (row < 0 || row >= hexGrid.HexMapData.Height) return;

            Debug.Log($"Mouse clicked at world position: {worldPos}");

            HexTypeSO[] palette = hexGrid.PaintPalette;
            if (palette == null || palette.Length == 0) return;
            HexTypeSO selectedType = palette[_selectedTypeIndex];
            if (selectedType == null) return;

            Undo.RecordObject(hexGrid.HexMapData, "Paint Hex Cell");
            hexGrid.HexMapData.SetCellType(col, row, selectedType);
            hexGrid.GenerateGrid();
            EditorUtility.SetDirty(hexGrid.HexMapData);

            Debug.Log($"Painted ({col}, {row}) whit {selectedType.name}");
            SceneView.RepaintAll();

        }


    }

}

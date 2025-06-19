using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;

    [SerializeField] private Tilemap obstacleTilemap; // ← collegata nell’Inspector

    private Dictionary<Vector2Int, Node> grid = new Dictionary<Vector2Int, Node>();

    void Awake()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                Vector3Int tilemapPos = new Vector3Int(x, y, 0);
                Vector3 worldPos = new Vector3(x * cellSize, y * cellSize, 0);

                // Qui controlliamo se nella tilemap c’è qualcosa
                bool walkable = obstacleTilemap.GetTile(tilemapPos) == null;

                Node node = new Node(coordinates, worldPos, walkable);
                grid.Add(coordinates, node);

#if UNITY_EDITOR
                Debug.DrawLine(worldPos, worldPos + Vector3.right * 0.9f, walkable ? Color.green : Color.red, 100f);
                Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.9f, walkable ? Color.green : Color.red, 100f);
#endif
            }
        }

        Debug.Log("Griglia generata con ostacoli letti da Tilemap.");
    }
}

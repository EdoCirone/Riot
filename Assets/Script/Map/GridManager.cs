using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;

    [SerializeField] private Tilemap obstacleTilemap;

    private Dictionary<Vector2Int, Node> grid = new Dictionary<Vector2Int, Node>();

    void Awake()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        int halfWidth = width / 2;
        int halfHeight = height / 2;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int y = -halfHeight; y < halfHeight; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                Vector3Int tilemapPos = new Vector3Int(x, y, 0);
                Vector3 worldPos = new Vector3(x * cellSize + cellSize / 2f, y * cellSize + cellSize / 2f, 0);

                bool walkable = obstacleTilemap.GetTile(tilemapPos) == null;

                Node node = new Node(coordinates, worldPos, walkable);
                grid.Add(coordinates, node);

#if UNITY_EDITOR
                Debug.DrawLine(worldPos, worldPos + Vector3.right * 0.9f, walkable ? Color.green : Color.red, 100f);
                Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.9f, walkable ? Color.green : Color.red, 100f);
#endif
            }
        }

        Debug.Log("🟩 Griglia centrata generata.");
    }

    public Node GetNodeAt(Vector2Int coords)
    {
        grid.TryGetValue(coords, out Node node);
        return node;
    }

    public IEnumerable<Node> GetAllNodes() => grid.Values;

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int checkCoords = node.coordinates + dir;
            if (grid.TryGetValue(checkCoords, out Node neighbor))
            {
                neighbours.Add(neighbor);
            }
        }

        return neighbours;
    }

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
}

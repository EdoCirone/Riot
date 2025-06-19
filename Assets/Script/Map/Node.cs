using UnityEngine;

public class Node
{
    public Vector2Int coordinates;    // coordinate nella griglia (x, y)
    public Vector3 worldPosition;     // posizione nel mondo Unity
    public bool isWalkable;           // può passarci sopra?
    public int cost;                  // per il pathfinding (default 1)

    public Node(Vector2Int coordinates, Vector3 worldPosition, bool isWalkable, int cost = 1)
    {
        this.coordinates = coordinates;
        this.worldPosition = worldPosition;
        this.isWalkable = isWalkable;
        this.cost = cost;
    }
}
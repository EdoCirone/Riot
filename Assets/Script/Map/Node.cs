using UnityEngine;

public class Node
{
    public Vector2Int coordinates;
    public Vector3 worldPosition;
    public bool isWalkable;
    public int cost;

    // A* specific
    public int gCost; // costo dal punto di partenza
    public int hCost; // costo stimato fino all’arrivo
    public int fCost => gCost + hCost;
    public Node cameFrom;

    public Node(Vector2Int coordinates, Vector3 worldPosition, bool isWalkable, int cost = 1)
    {
        this.coordinates = coordinates;
        this.worldPosition = worldPosition;
        this.isWalkable = isWalkable;
        this.cost = cost;
    }
}
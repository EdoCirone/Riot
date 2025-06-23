using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    public List<Node> FindPath(Vector2Int startCoords, Vector2Int endCoords)
    {
        Node startNode = gridManager.GetNodeAt(startCoords);
        Node endNode = gridManager.GetNodeAt(endCoords);

        if (startNode == null || endNode == null || !startNode.isWalkable || !endNode.isWalkable)
            return null;

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();

        foreach (var node in gridManager.GetAllNodes())
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.cameFrom = null;
        }

        startNode.gCost = 0;
        startNode.hCost = GetHeuristic(startNode, endNode);

        while (openSet.Count > 0)
        {
            Node current = GetLowestFCostNode(openSet);

            if (current == endNode)
                return ReconstructPath(endNode);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in gridManager.GetNeighbours(current))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;

                int tentativeGCost = current.gCost + neighbor.cost;

                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.cameFrom = current;
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = GetHeuristic(neighbor, endNode);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null; // nessun percorso trovato
    }

    private int GetHeuristic(Node a, Node b)
    {
        return Mathf.Abs(a.coordinates.x - b.coordinates.x) + Mathf.Abs(a.coordinates.y - b.coordinates.y);
    }

    private Node GetLowestFCostNode(List<Node> nodes)
    {
        Node best = nodes[0];
        foreach (var node in nodes)
        {
            if (node.fCost < best.fCost || (node.fCost == best.fCost && node.hCost < best.hCost))
                best = node;
        }
        return best;
    }

    private List<Node> ReconstructPath(Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;

        while (current != null)
        {
            path.Add(current);
            current = current.cameFrom;
        }

        path.Reverse();
        return path;
    }
}
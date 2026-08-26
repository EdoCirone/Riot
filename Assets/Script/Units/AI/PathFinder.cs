using UnityEngine;
using System.Collections.Generic;

public class PathFinder : MonoBehaviour
{
    public List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates end, HexGrid grid)
    {

        //A* Algorithm implementation goes here

        Dictionary<HexCoordinates, int> gCost = new();
        Dictionary<HexCoordinates, HexCoordinates> cameFrom = new();

        gCost[start] = 0;

        List<HexCoordinates> foundCell = new();
        List<HexCoordinates> checkedCell = new();

        foundCell.Add(start);
        bool pathFound = false;

        List<HexCoordinates> path = new();
        while (foundCell.Count > 0)
        {
            // FoundMinimumF restituisce sempre un valore: parte da foundcells[0] ed è
            // chiamata solo qui, dove la lista è garantita non vuota dal while.
            HexCoordinates minFCell = FoundMinimumF(foundCell, end, gCost);

            checkedCell.Add(minFCell);
            foundCell.Remove(minFCell);

            foreach (HexCoordinates neighbor in minFCell.GetNeighbors())
            {
                if (checkedCell.Contains(neighbor)) continue;
                if (!grid.TryGetCell(neighbor, out HexCell neighborCell)) continue;
                if (!TacticalQuery.IsCellAvailable(neighborCell)) continue;

                int tentativeGCost = gCost[minFCell] + 1;
                if (!foundCell.Contains(neighbor))
                {
                    gCost[neighbor] = tentativeGCost;
                    cameFrom[neighbor] = minFCell;
                    foundCell.Add(neighbor);
                }
                else if (tentativeGCost < gCost[neighbor])
                {
                    gCost[neighbor] = tentativeGCost;
                    cameFrom[neighbor] = minFCell;
                }
            }

            if (minFCell.Equals(end))
            {
                HexCoordinates current = end;
                while (!current.Equals(start))
                {
                    current = cameFrom[current];
                    path.Add(current);
                }
                pathFound = true;
                break;
            }
        }
        if (pathFound)
        {
            path.Reverse();
            path.Add(end);
        }
        return path;
    }

    private HexCoordinates FoundMinimumF(List<HexCoordinates> foundcells, HexCoordinates end, Dictionary<HexCoordinates, int> gCost)
    {
        HexCoordinates bestCell = foundcells[0];
        int minF = int.MaxValue;
        foreach (HexCoordinates cell in foundcells)
        {
            int f = gCost[cell] + Heuristic(cell, end);
            if (f < minF)
            {
                minF = f;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    private int Heuristic(HexCoordinates a, HexCoordinates b)
    {
        return a.Distance(b);
    }
}

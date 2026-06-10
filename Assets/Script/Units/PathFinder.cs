using UnityEngine;
using System.Collections.Generic;

public class PathFinder : MonoBehaviour
{
    public List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates end, HexGrid grid)
    {

        //A* Algorithm implementation goes here

        Dictionary<HexCoordinates, int> gCost = new Dictionary<HexCoordinates, int>();
        Dictionary<HexCoordinates, HexCoordinates> cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();

        gCost[start] = 0;

        List<HexCoordinates> foundCell = new List<HexCoordinates>();
        List<HexCoordinates> checkedCell = new List<HexCoordinates>();

        foundCell.Add(start);

        List<HexCoordinates> path = new List<HexCoordinates>();
        while (foundCell.Count > 0)
        {

            HexCoordinates minFCell = FoundMinimumF(foundCell, end, gCost);
            if (minFCell != null)
            {
                checkedCell.Add(minFCell);
                foundCell.Remove(minFCell);

                foreach (HexCoordinates neighbor in minFCell.GetNeighbors())
                {
                    if (checkedCell.Contains(neighbor) || !grid.IsCellWalkable(neighbor))
                    {
                        continue;
                    }
                    int tentativeGCost = gCost[minFCell] + 1; // Assuming uniform cost for moving to a neighbor
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

                    break;
                }
            }
        }
        path.Add(start);
        path.Reverse();
        path.Add(end);
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



using System.Collections.Generic;

public static class TacticalQuery
{


    public static Dictionary<HexCoordinates, int> GetReachable(
       HexCoordinates start, int budget, HexGrid map)
    {
        Dictionary<HexCoordinates, int> visited = new();
        Queue<(HexCoordinates coord, int cost)> queue = new();

        visited[start] = 0;
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (current, cost) = queue.Dequeue();
            foreach (HexCoordinates dir in HexCoordinates.Directions)
            {
                HexCoordinates neighbor = current + dir;
                int newCost = cost + 1;
                if (newCost > budget) continue;
                if (visited.ContainsKey(neighbor)) continue;
                if (!map.TryGetCell(neighbor, out HexCell cell)) continue;
                if (!IsCellAvailable(cell)) continue;
                visited[neighbor] = newCost;
                queue.Enqueue((neighbor, newCost));
            }
        }

        return visited;   
    }
    public static bool IsCellAvailable(HexCell cell)
    {
        if (cell == null) return false;
        if (cell.OccupiedBy != null) return false;
        return cell.Type.IsWalkable;
    }

    public static bool HasChargeRoom(HexCoordinates atkCoord, HexCoordinates defCoord,
                                 HexGrid map, out HexCoordinates chargeDestination)
    {
        chargeDestination = default;
        if (map == null) return false;

        int distance = atkCoord.Distance(defCoord);
        if (distance != 3) return false;

        HexCoordinates? dir = HexDirectionFinder.FindDirection(atkCoord, defCoord);
        if (dir == null) return false;

        HexCoordinates dirValue = dir.Value;
        HexCoordinates firstStep = new HexCoordinates(atkCoord.Q + dirValue.Q, atkCoord.R + dirValue.R);
        HexCoordinates secondStep = new HexCoordinates(atkCoord.Q + dirValue.Q * 2, atkCoord.R + dirValue.R * 2);

        if (!map.TryGetCell(firstStep, out HexCell firstCell) || !IsCellAvailable(firstCell)) return false;
        if (!map.TryGetCell(secondStep, out HexCell secondCell) || !IsCellAvailable(secondCell)) return false;

        chargeDestination = secondStep;
        return true;
    }
}


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

    public static List<HexCoordinates> GetValidTargets(
    HexCoordinates from, int budget, ActionType action, HexGrid map)
    {
        List<HexCoordinates> targets = new();

        switch (action)
        {
            case ActionType.Charge:
                if (budget < 4) break;

                foreach (HexCell cell in map.GetAllCells())
                {
                    if (cell.OccupiedBy is PoliceRuntime
                        && HasChargeRoom(from, cell.Coordinates, map, out _))
                    {
                        targets.Add(cell.Coordinates);
                    }
                }
                break;

            case ActionType.Throw:
                if (budget < 2) break;
                foreach (HexCell cell in map.GetAllCells())
                {
                    if (cell.OccupiedBy is PoliceRuntime
                        && from.Distance(cell.Coordinates) == 2
                        && HasThrowPath(from, cell.Coordinates, map))
                    {
                        targets.Add(cell.Coordinates);
                    }
                }
                break;

            case ActionType.Barricade:                         
                foreach (HexCoordinates dir in HexCoordinates.Directions)
                {
                    HexCoordinates neighbor = from + dir;        
                    if (map.TryGetCell(neighbor, out HexCell cell)
                        && IsCellAvailable(cell))               
                    {
                        targets.Add(neighbor);
                    }
                }
                break;
        }

        return targets;
    }
    public static bool IsCellAvailable(HexCell cell)
    {
        if (cell == null) return false;
        if (cell.OccupiedBy != null) return false;
        if (cell.Barricade != null) return false;
        return cell.Type.IsWalkable;
    }

    public struct AttackOption
    {
        public bool IsValid;
        public bool RequiresMovement;
        public HexCoordinates MoveDestination;
        public int MoveCost;
    }

    public static AttackOption GetAttackOption(HexCoordinates from, HexCoordinates targetCoord, int budget, HexGrid map,
     Dictionary<HexCoordinates, int> precomputedVisited = null)
    {
        if (budget < 1) return new AttackOption { IsValid = false };

        if (from.Distance(targetCoord) == 1)
            return new AttackOption { IsValid = true, RequiresMovement = false };

        Dictionary<HexCoordinates, int> visited = precomputedVisited ?? GetReachable(from, budget, map);

        bool found = false;
        HexCoordinates bestNeighbor = default;
        int bestCost = int.MaxValue;

        foreach (HexCoordinates neighbor in targetCoord.GetNeighbors())
        {
            if (!visited.TryGetValue(neighbor, out int cost)) continue;
            if (cost + 1 > budget) continue;
            if (cost < bestCost)
            {
                bestCost = cost;
                bestNeighbor = neighbor;
                found = true;
            }
        }

        if (!found) return new AttackOption { IsValid = false };

        return new AttackOption
        {
            IsValid = true,
            RequiresMovement = true,
            MoveDestination = bestNeighbor,
            MoveCost = bestCost
        };
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
    private static bool HasThrowPath(HexCoordinates from, HexCoordinates target, HexGrid map)
    {
        foreach (HexCoordinates dir in HexCoordinates.Directions)
        {
            HexCoordinates neighbor = from + dir;
            if (neighbor.Distance(target) != 1) continue;
            if (map.TryGetCell(neighbor, out HexCell cell) && cell.Type.IsWalkable)
                return true;
        }
        return false;
    }
}

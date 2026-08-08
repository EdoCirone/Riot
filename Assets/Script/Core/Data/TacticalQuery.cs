
using System.Collections.Generic;

public static class TacticalQuery
{

    public const int ChargeCost = 4;
    public const int ChantCost = 3;
    public const int ThrowRange = 2;

    private const int SitCost = 1;
    private const int StandCost = 2;

    public static int GetSitStandCost(AbstractUnitsRunTime unit)
    => unit != null && unit.IsSeated ? StandCost : SitCost;

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
        AbstractUnitsRunTime unit, ActionType action, ItemSO item, HexGrid map)
    {
        List<HexCoordinates> targets = new();
        if (unit == null || unit.PositionCell == null || map == null) return targets;

        HexCoordinates from = unit.PositionCell.Coordinates;
        int budget = unit.ActionPoints;

        switch (action)
        {
            case ActionType.Charge:
                if (budget < ChargeCost) break;
                foreach (HexCell cell in map.GetAllCells())
                {
                    if (cell.OccupiedBy is PoliceRuntime police
                        && police.IsAlive && !police.IsSeated
                        && HasChargeRoom(from, cell.Coordinates, map, out _))
                    {
                        targets.Add(cell.Coordinates);
                    }
                }
                break;

            case ActionType.Throw:
                if (unit is not SpezzoneRuntime thrower) break;
                if (item is not ThrowItemSO throwItem) break;
                foreach (HexCell cell in map.GetAllCells())
                {
                    if (CanThrow(thrower, cell, throwItem, map))
                        targets.Add(cell.Coordinates);
                }
                break;

            case ActionType.Barricade:
                if (unit is not SpezzoneRuntime builder) break;
                if (item is not BarricadeSO barricade) break;
                foreach (HexCoordinates dir in HexCoordinates.Directions)
                {
                    if (map.TryGetCell(from + dir, out HexCell cell)
                        && CanPlaceBarricade(builder, cell, barricade))
                    {
                        targets.Add(cell.Coordinates);
                    }
                }
                break;

            case ActionType.Chant:
                if (budget < ChantCost) break;
                targets.Add(from);
                break;

            case ActionType.SitStand:
                if (budget < GetSitStandCost(unit)) break;
                targets.Add(from);
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

    public struct AuraBonus
    {
        public int Atk;
        public int Def;
        public int Mor;
    }

    public static AuraBonus GetAuraBonus(AbstractUnitsRunTime unit, HexGrid map)
    {
        AuraBonus total = new AuraBonus();
        if (unit == null || unit.PositionCell == null || map == null) return total;

        foreach (HexCoordinates dir in HexCoordinates.Directions)
        {
            HexCoordinates neighborCoord = unit.PositionCell.Coordinates + dir;
            if (!map.TryGetCell(neighborCoord, out HexCell cell)) continue;

            AbstractUnitsRunTime neighbor = cell.OccupiedBy;
            if (neighbor == null) continue;
            if (neighbor.Status != UnitsStatus.Alive) continue;
            if (neighbor.IsPanicked) continue;

            // l'aura passa solo fra unità della stessa parte
            if (unit is SpezzoneRuntime && neighbor is not SpezzoneRuntime) continue;
            if (unit is PoliceRuntime && neighbor is not PoliceRuntime) continue;

            total.Atk += neighbor.AuraAtk;
            total.Def += neighbor.AuraDef;
            total.Mor += neighbor.AuraMor;
        }

        return total;
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

    /// <summary>
    /// Legalità completa del lancio. La chiamano SIA l'highlight SIA l'esecutore:
    /// è ciò che impedisce alla divergenza di riformarsi.
    /// </summary>
    public static bool CanThrow(SpezzoneRuntime unit, HexCell target, ThrowItemSO item, HexGrid map)
    {
        if (unit == null || target == null || item == null || map == null) return false;
        if (!unit.IsAlive) return false;
        if (!unit.Inventory.HasItem(item)) return false;
        if (unit.ActionPoints < item.ActionPointCost) return false;

        if (target.OccupiedBy is not PoliceRuntime police || !police.IsAlive) return false;

        HexCoordinates from = unit.PositionCell.Coordinates;
        if (from.Distance(target.Coordinates) != ThrowRange) return false;

        return HasThrowPath(from, target.Coordinates, map);
    }

    public static bool CanPlaceBarricade(SpezzoneRuntime unit, HexCell target, BarricadeSO item)
    {
        if (unit == null || target == null || item == null) return false;
        if (!unit.IsAlive) return false;
        if (!unit.Inventory.HasItem(item)) return false;
        if (unit.ActionPoints < item.ActionPointCost) return false;

        if (unit.PositionCell.Coordinates.Distance(target.Coordinates) != 1) return false;
        if (target.Type.IsObjective) return false;

        return IsCellAvailable(target);
    }
}

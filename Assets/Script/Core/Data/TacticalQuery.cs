
using System.Collections.Generic;

public static class TacticalQuery
{

    public const int ChargeCost = 4;
    public const int ChantCost = 3;
    public const int ThrowRange = 2;

    private const int SitCost = 1;
    private const int StandCost = 2;

    public const int PanicSteps = 2;
    public const int PanicTurnsCorteo = 3;
    public const int PanicTurnsPolice = 1;

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

        // ⚠ La maschera _allowedActions decide QUI, non nell'InputHandler. Prima viveva solo
        // in SetSelectedAction, cioè era un filtro dell'interfaccia e non una regola:
        // qualunque azione invocata da codice — PoliceAI in testa — la scavalcava, e la
        // maschera della polizia non aveva alcun effetto.
        // Mettendola nel cancello condiviso, highlight ed esecuzione non possono divergere.
        if (!unit.CanPerformAction(action)) return targets;

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

        if (unit.IsPanicked) return total;

        foreach (HexCoordinates dir in HexCoordinates.Directions)
        {
            HexCoordinates neighborCoord = unit.PositionCell.Coordinates + dir;
            if (!map.TryGetCell(neighborCoord, out HexCell cell)) continue;

            AbstractUnitsRunTime neighbor = cell.OccupiedBy;
            if (neighbor == null) continue;
            if (!neighbor.IsAlive) continue;

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
        if (!unit.CanPerformAction(ActionType.Throw)) return false;
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
        if (!unit.CanPerformAction(ActionType.Barricade)) return false;
        if (!unit.Inventory.HasItem(item)) return false;
        if (unit.ActionPoints < item.ActionPointCost) return false;

        if (unit.PositionCell.Coordinates.Distance(target.Coordinates) != 1) return false;
        if (target.IsObjective) return false;

        return IsCellAvailable(target);
    }

    /// <summary>
    /// L'onda di panico: parte da una cella e si propaga PER CONTATTO attraverso le unità
    /// della stessa parte. Il decadimento si misura in passi attraverso la folla, non in
    /// distanza esagonale — è quello che fa contare la forma del corteo.
    /// L'origine è una CELLA e non un'unità perché chi ha subito la carica può essere già
    /// uscito di gioco: il corteo l'ha visto cadere lo stesso.
    /// Non muta niente: restituisce chi è coinvolto e a che passo.
    /// </summary>
    public static List<(AbstractUnitsRunTime unit, int steps)> GetPanicWave(
        HexCell origin, AbstractUnitsRunTime epicentre, HexGrid map)
    {
        List<(AbstractUnitsRunTime, int)> wave = new();
        if (origin == null || epicentre == null || map == null) return wave;

        bool policeSide = epicentre is PoliceRuntime;

        HashSet<HexCoordinates> visited = new() { origin.Coordinates };
        Queue<(HexCoordinates coord, int steps)> queue = new();
        queue.Enqueue((origin.Coordinates, 0));

        // L'epicentro entra nell'onda solo se è ancora in gioco: se il -1 di Morale
        // l'ha ucciso, l'onda parte lo stesso dalla sua cella ma lui non c'è più.
        if (epicentre.IsAlive && !epicentre.IsSeated)
            wave.Add((epicentre, 0));

        while (queue.Count > 0)
        {
            var (current, steps) = queue.Dequeue();
            if (steps >= PanicSteps) continue;

            foreach (HexCoordinates dir in HexCoordinates.Directions)
            {
                HexCoordinates neighborCoord = current + dir;
                if (visited.Contains(neighborCoord)) continue;
                if (!map.TryGetCell(neighborCoord, out HexCell cell)) continue;

                AbstractUnitsRunTime unit = cell.OccupiedBy;
                if (unit == null) continue;                        // il panico viaggia fra le persone
                if (!unit.IsAlive) continue;
                if (unit.IsSeated) continue;                       // frangifuoco: non entra e non trasmette
                if ((unit is PoliceRuntime) != policeSide) continue;

                visited.Add(neighborCoord);
                wave.Add((unit, steps + 1));
                queue.Enqueue((neighborCoord, steps + 1));
            }
        }

        return wave;
    }
}

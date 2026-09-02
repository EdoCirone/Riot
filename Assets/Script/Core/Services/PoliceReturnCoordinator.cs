using System.Collections.Generic;

public sealed class PoliceReturnCoordinator
{
    private readonly HashSet<PoliceRuntime> _waiting = new();
    public IReadOnlyList<PoliceRuntime> ProcessTurnStart(
    IReadOnlyList<PoliceRuntime> policeUnits,
    HexGrid map)
    {
        List<PoliceRuntime> returned = new();

        if (policeUnits == null || map == null)
            return returned;

        // Primo passaggio: tenta il rientro di chi era già in attesa.
        foreach (PoliceRuntime police in policeUnits)
        {
            if (police == null || !_waiting.Contains(police))
                continue;

            if (police.Status != UnitsStatus.Disperse)
            {
                _waiting.Remove(police);
                continue;
            }

            HexCell destination = FindReturnCell(police, map);

            if (destination == null)
                continue;

            if (!police.TryReturnToBoard(destination))
                continue;

            _waiting.Remove(police);
            returned.Add(police);
        }

        // Secondo passaggio: registra chi è disperso adesso.
        foreach (PoliceRuntime police in policeUnits)
        {
            if (police != null
                && police.Status == UnitsStatus.Disperse)
            {
                _waiting.Add(police);
            }
        }

        return returned;
    }

    private static HexCell FindReturnCell(
    PoliceRuntime police,
    HexGrid map)
    {
        HexCell station = FindNearestStation(police, map);

        if (station == null)
            return null;

        if (IsAvailable(station))
            return station;

        foreach (HexCoordinates coordinates
                 in station.Coordinates.GetNeighbors())
        {
            if (!map.TryGetCell(coordinates, out HexCell adjacent))
                continue;

            if (IsAvailable(adjacent))
                return adjacent;
        }

        return null;
    }

    private static HexCell FindNearestStation(
        PoliceRuntime police,
        HexGrid map)
    {
        if (police == null
            || map == null
            || map.PoliceStations.Count == 0)
        {
            return null;
        }

        HexCell nearest = null;
        int bestDistance = int.MaxValue;

        foreach (HexCell station in map.PoliceStations)
        {
            if (station == null)
                continue;

            int distance = DistanceToAssignment(police, station);

            if (nearest != null && distance >= bestDistance)
                continue;

            nearest = station;
            bestDistance = distance;
        }

        return nearest;
    }

    private static int DistanceToAssignment(
        PoliceRuntime police,
        HexCell station)
    {
        ObjectiveRuntime objective = police.GuardedObjective;

        if (objective != null && objective.Cells != null)
        {
            int bestDistance = int.MaxValue;

            foreach (HexCell objectiveCell in objective.Cells)
            {
                if (objectiveCell == null)
                    continue;

                int distance = station.Coordinates.Distance(
                    objectiveCell.Coordinates
                );

                if (distance < bestDistance)
                    bestDistance = distance;
            }

            if (bestDistance != int.MaxValue)
                return bestDistance;
        }

        return police.PositionCell != null
            ? station.Coordinates.Distance(
                police.PositionCell.Coordinates
            )
            : int.MaxValue;
    }

    private static bool IsAvailable(HexCell cell)
    {
        return cell?.Type != null
            && TacticalQuery.IsCellAvailable(cell);
    }
}

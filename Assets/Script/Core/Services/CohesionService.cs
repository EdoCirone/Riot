using System.Collections.Generic;

public static class CohesionService
{
    private const int CohesionPerDirectedAdjacency = 10;

    public static int Calculate(
        IReadOnlyList<SpezzoneRuntime> units,
        HexGrid map)
    {
        if (units == null || map == null)
            return 0;

        int total = 0;

        foreach (SpezzoneRuntime unit in units)
        {
            if (unit == null
                || !unit.IsAlive
                || unit.PositionCell == null)
            {
                continue;
            }

            foreach (HexCoordinates neighbor
                     in unit.PositionCell.Coordinates.GetNeighbors())
            {
                if (!map.TryGetCell(neighbor, out HexCell cell))
                    continue;

                if (cell.OccupiedBy is SpezzoneRuntime other
                    && other != unit
                    && other.IsAlive)
                {
                    total += CohesionPerDirectedAdjacency;
                }
            }
        }

        return total;
    }
}

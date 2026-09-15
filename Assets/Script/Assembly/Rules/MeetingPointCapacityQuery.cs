using System.Collections.Generic;

public static class MeetingPointCapacityQuery
{
    public static bool TryCalculate(
        HexMapSO map,
        MeetingPointSO meetingPoint,
        out int capacity)
    {
        capacity = 0;

        if (map == null || meetingPoint == null)
            return false;

        if (!TryGetCellType(
                map,
                meetingPoint.Anchor,
                out HexTypeSO anchorType)
            || anchorType == null
            || !anchorType.IsMeetingGround)
        {
            return false;
        }

        var visited = new HashSet<HexCoordinates>();
        var pending = new Queue<HexCoordinates>();

        visited.Add(meetingPoint.Anchor);
        pending.Enqueue(meetingPoint.Anchor);

        while (pending.Count > 0)
        {
            HexCoordinates current = pending.Dequeue();
            capacity++;

            foreach (HexCoordinates neighbor in current.GetNeighbors())
            {
                if (!visited.Add(neighbor))
                    continue;

                if (!TryGetCellType(
                        map,
                        neighbor,
                        out HexTypeSO neighborType))
                {
                    continue;
                }

                if (neighborType == null
                    || !neighborType.IsMeetingGround)
                {
                    continue;
                }

                pending.Enqueue(neighbor);
            }
        }

        return true;
    }

    private static bool TryGetCellType(
        HexMapSO map,
        HexCoordinates coordinates,
        out HexTypeSO type)
    {
        type = null;

        int column = coordinates.Q;
        int row =
            coordinates.R
            + (column - (column & 1)) / 2;

        if (column < 0
            || column >= map.Width
            || row < 0
            || row >= map.Height)
        {
            return false;
        }

        type = map.GetCellType(column, row);
        return true;
    }
}

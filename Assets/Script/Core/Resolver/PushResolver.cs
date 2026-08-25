using System.Collections.Generic;

public static class PushResolver
{
    public readonly struct PushMove
    {
        public AbstractUnitsRunTime Unit { get; }
        public HexCell Destination { get; }

        public PushMove(AbstractUnitsRunTime unit, HexCell destination)
        {
            Unit = unit;
            Destination = destination;
        }
    }

    public sealed class PushResult
    {
        public bool IsResolved { get; }
        public bool WasRemoved { get; }
        public IReadOnlyList<PushMove> Moves { get; }

        public PushResult(bool isResolved, bool wasRemoved, IReadOnlyList<PushMove> moves)
        {
            IsResolved = isResolved;
            WasRemoved = wasRemoved;
            Moves = moves;
        }
    }

    public static PushResult Resolve(AbstractUnitsRunTime pusher, AbstractUnitsRunTime pushed, HexGrid map)
    {
        List<PushMove> moves = new();

        if (pusher == null || pushed == null || map == null)
            return new PushResult(false, false, moves);

        if (pusher.PositionCell == null || pushed.PositionCell == null)
            return new PushResult(false, false, moves);

        if (!pusher.IsAlive || !pushed.IsAlive)
            return new PushResult(false, false, moves);

        if (pusher.PositionCell.Coordinates.Distance(pushed.PositionCell.Coordinates) != 1)
        {
            return new PushResult(false, false, moves);
        }

        if (TryBuildPushChain(pusher, pushed, map, moves))
        {
            bool applied = ApplyPushChain(moves);

            return new PushResult(isResolved: applied, wasRemoved: false, moves);
        }

        pushed.RemoveFromBoard(CauseFrom(pusher));

        return new PushResult(isResolved: true, wasRemoved: true, moves);
    }

    private static bool TryBuildPushChain(
        AbstractUnitsRunTime pusher,
        AbstractUnitsRunTime pushed,
        HexGrid map,
        List<PushMove> moves)
    {
        HexCoordinates pusherCoord = pusher.PositionCell.Coordinates;

        HexCoordinates current = pushed.PositionCell.Coordinates;

        int dirQ = current.Q - pusherCoord.Q;
        int dirR = current.R - pusherCoord.R;

        List<AbstractUnitsRunTime> column = new() { pushed };
        AbstractUnitsRunTime unitToMove = pushed;

        while (true)
        {
            HexCoordinates behind = new(current.Q + dirQ, current.R + dirR);

            if (map.TryGetCell(behind, out HexCell behindCell)
                && behindCell.Type != null
                && behindCell.Type.IsWalkable
                && !behindCell.IsObjective
                && behindCell.Barricade == null)
            {
                AbstractUnitsRunTime blocker = behindCell.OccupiedBy;

                if (blocker == null)
                {
                    BuildMovesFromColumn(column, behindCell, moves);

                    return true;
                }

                if (!blocker.IsSeated
                    && IsSameSide(blocker, unitToMove))
                {
                    column.Add(blocker);
                    unitToMove = blocker;
                    current = behind;
                    continue;
                }
            }

            return TryReleaseSideways(column, dirQ, dirR, map, moves);
        }
    }

    private static void BuildMovesFromColumn(List<AbstractUnitsRunTime> column, HexCell tail, List<PushMove> moves)
    {
        for (int i = 0; i < column.Count; i++)
        {
            HexCell destination = i + 1 < column.Count
                ? column[i + 1].PositionCell
                : tail;

            moves.Add(new PushMove(column[i], destination));
        }
    }

    private static bool TryReleaseSideways(
        List<AbstractUnitsRunTime> column,
        int dirQ,
        int dirR,
        HexGrid map,
        List<PushMove> moves)
    {
        for (int i = column.Count - 1; i >= 0; i--)
        {
            HexCell side = FindSideCell(column[i], dirQ, dirR, map);

            if (side == null)
                continue;

            for (int j = 0; j < i; j++)
            {
                moves.Add(new PushMove(column[j], column[j + 1].PositionCell));
            }

            moves.Add(new PushMove(column[i], side));

            return true;
        }

        return false;
    }

    private static HexCell FindSideCell(AbstractUnitsRunTime unit, int dirQ, int dirR, HexGrid map)
    {
        int directionIndex = -1;

        for (int i = 0;
             i < HexCoordinates.Directions.Length;
             i++)
        {
            HexCoordinates direction = HexCoordinates.Directions[i];

            if (direction.Q == dirQ
                && direction.R == dirR)
            {
                directionIndex = i;
                break;
            }
        }

        if (directionIndex < 0)
            return null;

        HexCoordinates from = unit.PositionCell.Coordinates;

        HexCell best = null;
        int bestAllies = int.MaxValue;

        for (int offset = -1; offset <= 1; offset += 2)
        {
            int index = (directionIndex + offset + 6) % 6;

            HexCoordinates sideDirection = HexCoordinates.Directions[index];

            HexCoordinates candidateCoordinates = from + sideDirection;

            if (!map.TryGetCell(candidateCoordinates, out HexCell candidate))
            {
                continue;
            }

            if (!TacticalQuery.IsCellAvailable(candidate))
                continue;

            if (candidate.IsObjective)
                continue;

            int allies = CountAdjacentAllies(unit, candidateCoordinates, map);

            if (allies < bestAllies)
            {
                bestAllies = allies;
                best = candidate;
            }
        }

        return best;
    }

    private static int CountAdjacentAllies(AbstractUnitsRunTime unit, HexCoordinates from, HexGrid map)
    {
        int count = 0;

        foreach (HexCoordinates direction
                 in HexCoordinates.Directions)
        {
            if (!map.TryGetCell(from + direction, out HexCell cell))
            {
                continue;
            }

            AbstractUnitsRunTime other = cell.OccupiedBy;

            if (other == null || !other.IsAlive)
                continue;

            if (other == unit)
                continue;

            if (IsSameSide(other, unit))
                count++;
        }

        return count;
    }

    private static bool ApplyPushChain(IReadOnlyList<PushMove> moves)
    {
        for (int i = moves.Count - 1; i >= 0; i--)
        {
            PushMove move = moves[i];

            if (!move.Unit.SetPosition(move.Destination))
                return false;
        }

        return true;
    }

    private static MoraleLossCause CauseFrom(AbstractUnitsRunTime source)
    {
        return source is PoliceRuntime
            ? MoraleLossCause.PoliceContact
            : MoraleLossCause.Other;
    }

    private static bool IsSameSide(AbstractUnitsRunTime first, AbstractUnitsRunTime second)
    {
        return (first is PoliceRuntime)
            == (second is PoliceRuntime);
    }
}

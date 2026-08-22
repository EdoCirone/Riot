using System.Collections.Generic;

public static class UnitActionResolver
{
    public sealed class UnitActionResult
    {
        public bool Succeeded { get; }
        public UnitActionFailure Failure { get; }
        public IReadOnlyList<AbstractUnitsRunTime> AffectedUnits { get; }
        public int ActionPointCost { get; }
        public bool WasSeated { get; }

        private UnitActionResult(
            bool succeeded,
            UnitActionFailure failure,
            IReadOnlyList<AbstractUnitsRunTime> affectedUnits,
            int actionPointCost,
            bool wasSeated)
        {
            Succeeded = succeeded;
            Failure = failure;
            AffectedUnits = affectedUnits;
            ActionPointCost = actionPointCost;
            WasSeated = wasSeated;
        }

        public static UnitActionResult Success(
            IReadOnlyList<AbstractUnitsRunTime> affectedUnits,
            int actionPointCost,
            bool wasSeated = false)
        {
            return new UnitActionResult(
                true,
                UnitActionFailure.None,
                affectedUnits,
                actionPointCost,
                wasSeated
            );
        }

        public static UnitActionResult Fail(
            UnitActionFailure failure,
            int actionPointCost = 0,
            bool wasSeated = false)
        {
            return new UnitActionResult(
                false,
                failure,
                new List<AbstractUnitsRunTime>(),
                actionPointCost,
                wasSeated
            );
        }
    }

    public static UnitActionResult ResolveChant(
        AbstractUnitsRunTime caster,
        HexGrid map)
    {
        if (caster == null || !caster.IsAlive)
        {
            return UnitActionResult.Fail(
                UnitActionFailure.InvalidUnit,
                TacticalQuery.ChantCost
            );
        }

        if (caster.PositionCell == null)
        {
            return UnitActionResult.Fail(
                UnitActionFailure.InvalidPosition,
                TacticalQuery.ChantCost
            );
        }

        if (map == null)
        {
            return UnitActionResult.Fail(
                UnitActionFailure.InvalidMap,
                TacticalQuery.ChantCost
            );
        }

        if (!caster.CanPerformAction(ActionType.Chant))
        {
            return UnitActionResult.Fail(
                UnitActionFailure.ActionNotAllowed,
                TacticalQuery.ChantCost
            );
        }

        if (!caster.TrySpendActionPoint(
                TacticalQuery.ChantCost))
        {
            return UnitActionResult.Fail(
                UnitActionFailure.InsufficientActionPoints,
                TacticalQuery.ChantCost
            );
        }

        List<AbstractUnitsRunTime> affectedUnits =
            new();

        caster.GainMorale(1);
        caster.ClearPanic();
        affectedUnits.Add(caster);

        foreach (HexCoordinates neighbor
                 in caster.PositionCell.Coordinates.GetNeighbors())
        {
            if (!map.TryGetCell(neighbor, out HexCell cell))
                continue;

            if (cell.OccupiedBy is not SpezzoneRuntime spezzone)
                continue;

            if (!spezzone.IsAlive)
                continue;

            spezzone.GainMorale(1);
            spezzone.ClearPanic();
            affectedUnits.Add(spezzone);
        }

        return UnitActionResult.Success(
            affectedUnits,
            TacticalQuery.ChantCost
        );
    }

    public static UnitActionResult ResolveSitStand(
        AbstractUnitsRunTime unit)
    {
        if (unit == null || !unit.IsAlive)
        {
            return UnitActionResult.Fail(
                UnitActionFailure.InvalidUnit
            );
        }

        bool wasSeated = unit.IsSeated;

        int cost =
            TacticalQuery.GetSitStandCost(unit);

        if (!unit.CanPerformAction(ActionType.SitStand))
        {
            return UnitActionResult.Fail(
                UnitActionFailure.ActionNotAllowed,
                cost,
                wasSeated
            );
        }

        if (!unit.TrySpendActionPoint(cost))
        {
            return UnitActionResult.Fail(
                UnitActionFailure.InsufficientActionPoints,
                cost,
                wasSeated
            );
        }

        if (wasSeated)
            unit.StandUp();
        else
            unit.SitDown();

        return UnitActionResult.Success(
            new List<AbstractUnitsRunTime> { unit },
            cost,
            wasSeated
        );
    }
}

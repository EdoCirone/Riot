using System.Collections.Generic;
using UnityEngine;

public static class PanicResolver
{
    public readonly struct PanicEffect
    {
        public AbstractUnitsRunTime Unit { get; }
        public int Steps { get; }
        public int PanicTurns { get; }

        public PanicEffect(
            AbstractUnitsRunTime unit,
            int steps,
            int panicTurns)
        {
            Unit = unit;
            Steps = steps;
            PanicTurns = panicTurns;
        }
    }

    public static IReadOnlyList<PanicEffect> Resolve(
        HexCell origin,
        AbstractUnitsRunTime epicentre,
        HexGrid map)
    {
        List<PanicEffect> effects = new();

        if (origin == null || epicentre == null || map == null)
            return effects;

        List<(AbstractUnitsRunTime unit, int steps)> wave =
            TacticalQuery.GetPanicWave(origin, epicentre, map);

        int baseTurns = epicentre is PoliceRuntime
            ? TacticalQuery.PanicTurnsPolice
            : TacticalQuery.PanicTurnsCorteo;

        foreach ((AbstractUnitsRunTime unit, int steps) entry in wave)
        {
            int turns = Mathf.Max(1, baseTurns - entry.steps);

            entry.unit.ApplyPanic(turns);

            effects.Add(
                new PanicEffect(
                    entry.unit,
                    entry.steps,
                    entry.unit.PanicTurnsLeft
                )
            );
        }

        return effects;
    }
}

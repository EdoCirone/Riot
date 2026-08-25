using System.Collections.Generic;

public static class AuraService
{
    public sealed class AuraResult
    {
        public IReadOnlyList<AbstractUnitsRunTime> RemovedUnits { get; }

        public AuraResult(IReadOnlyList<AbstractUnitsRunTime> removedUnits)
        {
            RemovedUnits = removedUnits;
        }
    }

    public static AuraResult Resolve(
        IReadOnlyList<SpezzoneRuntime> spezzoni,
        IReadOnlyList<PoliceRuntime> police,
        HexGrid map)
    {
        List<AbstractUnitsRunTime> removedUnits = new();
        HashSet<AbstractUnitsRunTime> removedSet = new();

        if (map == null)
            return new AuraResult(removedUnits);

        bool someoneFell;

        do
        {
            someoneFell = false;

            List<(AbstractUnitsRunTime unit, int bonus)> pending = new();

            if (spezzoni != null)
            {
                foreach (SpezzoneRuntime unit in spezzoni)
                {
                    if (unit == null || !unit.IsAlive)
                        continue;

                    int bonus = TacticalQuery.GetAuraBonus(unit, map).Mor;

                    pending.Add((unit, bonus));
                }
            }

            if (police != null)
            {
                foreach (PoliceRuntime unit in police)
                {
                    if (unit == null || !unit.IsAlive)
                        continue;

                    int bonus = TacticalQuery.GetAuraBonus(unit, map).Mor;

                    pending.Add((unit, bonus));
                }
            }

            foreach ((AbstractUnitsRunTime unit, int bonus) entry
                     in pending)
            {
                bool wasAlive = entry.unit.IsAlive;

                entry.unit.ApplyAuraMorale(entry.bonus);

                if (wasAlive && !entry.unit.IsAlive)
                {
                    someoneFell = true;

                    if (removedSet.Add(entry.unit))
                        removedUnits.Add(entry.unit);
                }
            }
        }
        while (someoneFell);

        return new AuraResult(removedUnits);
    }
}

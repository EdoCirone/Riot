using System.Collections.Generic;

public static class CombatResolver
{
    public const int SkirmishCost = 1;

    public enum SkirmishFailure
    {
        None,
        InvalidUnit,
        InvalidPosition,
        NotAdjacent,
        InsufficientActionPoints
    }

    public sealed class SkirmishResolution
    {
        public bool Succeeded { get; }
        public SkirmishFailure Failure { get; }
        public CombatResult? Result { get; }
        public IReadOnlyList<AbstractUnitsRunTime> HitUnits { get; }

        private SkirmishResolution(
            bool succeeded,
            SkirmishFailure failure,
            CombatResult? result,
            IReadOnlyList<AbstractUnitsRunTime> hitUnits)
        {
            Succeeded = succeeded;
            Failure = failure;
            Result = result;
            HitUnits = hitUnits;
        }

        public static SkirmishResolution Success(CombatResult result, IReadOnlyList<AbstractUnitsRunTime> hitUnits)
        {
            return new SkirmishResolution(true, SkirmishFailure.None, result, hitUnits);
        }

        public static SkirmishResolution Fail(SkirmishFailure failure)
        {
            return new SkirmishResolution(false, failure, result: null, new List<AbstractUnitsRunTime>());
        }
    }

    public static SkirmishResolution ResolveSkirmish(
        AbstractUnitsRunTime attacker,
        AbstractUnitsRunTime defender,
        HexGrid map)
    {
        if (attacker == null
            || defender == null
            || !attacker.IsAlive
            || !defender.IsAlive)
        {
            return SkirmishResolution.Fail(SkirmishFailure.InvalidUnit);
        }

        if (attacker.PositionCell == null
            || defender.PositionCell == null)
        {
            return SkirmishResolution.Fail(SkirmishFailure.InvalidPosition);
        }

        if (attacker.PositionCell.Coordinates.Distance(defender.PositionCell.Coordinates) != 1)
        {
            return SkirmishResolution.Fail(SkirmishFailure.NotAdjacent);
        }

        if (!attacker.TrySpendActionPoint(SkirmishCost))
        {
            return SkirmishResolution.Fail(SkirmishFailure.InsufficientActionPoints);
        }

        CombatResult result = Resolve(attacker, defender, map);

        List<AbstractUnitsRunTime> hitUnits = new();

        switch (result)
        {
            case CombatResult.Win:
                defender.LoseMorale(1, CauseFrom(attacker));

                hitUnits.Add(defender);
                break;

            case CombatResult.Lose:
                attacker.LoseMorale(1, CauseFrom(defender));

                hitUnits.Add(attacker);
                break;

            case CombatResult.Par:
                attacker.LoseMorale(1, CauseFrom(defender));

                defender.LoseMorale(1, CauseFrom(attacker));

                hitUnits.Add(attacker);
                hitUnits.Add(defender);
                break;
        }

        return SkirmishResolution.Success(result, hitUnits);
    }

    public static CombatResult Resolve(AbstractUnitsRunTime attacker, AbstractUnitsRunTime defender, HexGrid map)
    {
        int atk = GetEffectiveAtk(attacker, map);
        int def = GetEffectiveDef(defender, map);

        if (atk > def)
            return CombatResult.Win;

        if (atk < def)
            return CombatResult.Lose;

        return CombatResult.Par;
    }

    public static int GetEffectiveAtk(AbstractUnitsRunTime unit, HexGrid map)
    {
        return unit.Atk
            + TacticalQuery.GetAuraBonus(unit, map).Atk;
    }

    public static int GetEffectiveDef(AbstractUnitsRunTime unit, HexGrid map)
    {
        return unit.Def
            + TacticalQuery.GetAuraBonus(unit, map).Def;
    }

    private static MoraleLossCause CauseFrom(AbstractUnitsRunTime source)
    {
        return source is PoliceRuntime
            ? MoraleLossCause.PoliceContact
            : MoraleLossCause.Other;
    }
}

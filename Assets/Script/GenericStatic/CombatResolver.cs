
public static class CombatResolver
{
    public static CombatResult Resolve(AbstractUnitsRunTime attacker, AbstractUnitsRunTime defender, HexGrid map)
    {
        int atk = GetEffectiveAtk(attacker, map);
        int def = GetEffectiveDef(defender, map);

        if (atk > def) return CombatResult.Win;
        if (atk < def) return CombatResult.Lose;
        return CombatResult.Par;
    }

    public static int GetEffectiveAtk(AbstractUnitsRunTime unit, HexGrid map)
        => unit.Atk + TacticalQuery.GetAuraBonus(unit, map).Atk;

    public static int GetEffectiveDef(AbstractUnitsRunTime unit, HexGrid map)
        => unit.Def + TacticalQuery.GetAuraBonus(unit, map).Def;
}
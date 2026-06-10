using UnityEngine;

public static class CombatResolver
{
    public static CombatResult Resolve(SpezzoneRuntime attaker, SpezzoneRuntime defender)
    {

        if (attaker.Atk > defender.Def)
        {
            return CombatResult.Win;
        }

        else if (attaker.Atk < defender.Def)
        {
            return CombatResult.Lose;
        }

        else
        {
            return CombatResult.Par;
        }
    }
}

using NUnit.Framework;
using UnityEngine;

public class CombatResolverTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        private readonly int _atk;
        private readonly int _def;

        public TestUnit(int atk, int def)
            : base(
                positionCell: null,
                status: UnitsStatus.Alive,
                morale: 10,
                actionPoints: 4)
        {
            _atk = atk;
            _def = def;
        }

        public override string DisplayName => "Test Unit";
        public override Sprite Avatar => null;
        public override int Atk => _atk;
        public override int Def => _def;
        public override int AuraAtk => 0;
        public override int AuraDef => 0;
        public override int AuraMor => 0;
        public override GameObject GraphicsPrefab => null;

        public override bool CanPerformAction(ActionType action)
        {
            return true;
        }
    }

    [TestCase(5, 4, CombatResult.Win)]
    [TestCase(3, 4, CombatResult.Lose)]
    [TestCase(4, 4, CombatResult.Par)]
    public void Resolve_ComparesAttackerAtkWithDefenderDef(
        int attackerAtk,
        int defenderDef,
        CombatResult expectedResult)
    {
        TestUnit attacker = new TestUnit(attackerAtk, def: 0);
        TestUnit defender = new TestUnit(atk: 0, defenderDef);

        CombatResult result =
            CombatResolver.Resolve(attacker, defender, map: null);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public void EffectiveStats_WithoutMapReturnBaseStats()
    {
        TestUnit unit = new TestUnit(atk: 7, def: 5);

        int effectiveAtk = CombatResolver.GetEffectiveAtk(unit, map: null);
        int effectiveDef = CombatResolver.GetEffectiveDef(unit, map: null);

        Assert.That(effectiveAtk, Is.EqualTo(7));
        Assert.That(effectiveDef, Is.EqualTo(5));
    }
}

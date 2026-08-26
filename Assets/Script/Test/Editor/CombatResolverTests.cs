using NUnit.Framework;
using UnityEngine;

public class CombatResolverTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        private readonly int _atk;
        private readonly int _def;

        public TestUnit(int atk, int def, HexCell positionCell = null, int morale = 10, int actionPoints = 4)
            : base(positionCell, UnitsStatus.Alive, morale, actionPoints)
        {
            _atk = atk;
            _def = def;

            positionCell?.TryOccupy(this);
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
        TestUnit attacker = new(atk: attackerAtk, def: 0);

        TestUnit defender = new(atk: 0, def: defenderDef);

        CombatResult result = CombatResolver.Resolve(attacker, defender, map: null);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public void EffectiveStats_WithoutMapReturnBaseStats()
    {
        TestUnit unit = new(atk: 7, def: 5);

        int effectiveAtk = CombatResolver.GetEffectiveAtk(unit, map: null);

        int effectiveDef = CombatResolver.GetEffectiveDef(unit, map: null);

        Assert.That(effectiveAtk, Is.EqualTo(7));
        Assert.That(effectiveDef, Is.EqualTo(5));
    }

    [TestCase(5, 4, CombatResult.Win, 10, 9, 1)]
    [TestCase(3, 4, CombatResult.Lose, 9, 10, 1)]
    [TestCase(4, 4, CombatResult.Par, 9, 9, 2)]
    public void ResolveSkirmish_AppliesExpectedOutcome(
        int attackerAtk,
        int defenderDef,
        CombatResult expectedResult,
        int expectedAttackerMorale,
        int expectedDefenderMorale,
        int expectedHitCount)
    {
        HexCell attackerCell = new(new HexCoordinates(0, 0), type: null);

        HexCell defenderCell = new(new HexCoordinates(1, 0), type: null);

        TestUnit attacker = new(atk: attackerAtk, def: 0, positionCell: attackerCell);

        TestUnit defender = new(atk: 0, def: defenderDef, positionCell: defenderCell);

        CombatResolver.SkirmishResolution resolution = CombatResolver.ResolveSkirmish(attacker, defender, map: null);

        Assert.That(resolution.Succeeded, Is.True);

        Assert.That(resolution.Failure, Is.EqualTo(CombatResolver.SkirmishFailure.None));

        Assert.That(resolution.Result, Is.EqualTo(expectedResult));

        Assert.That(resolution.HitUnits.Count, Is.EqualTo(expectedHitCount));

        Assert.That(attacker.ActionPoints, Is.EqualTo(3));

        Assert.That(attacker.Morale, Is.EqualTo(expectedAttackerMorale));

        Assert.That(defender.Morale, Is.EqualTo(expectedDefenderMorale));
    }

    [Test]
    public void ResolveSkirmish_RejectsNonAdjacentUnitsWithoutMutation()
    {
        HexCell attackerCell = new(new HexCoordinates(0, 0), type: null);

        HexCell defenderCell = new(new HexCoordinates(2, 0), type: null);

        TestUnit attacker = new(atk: 5, def: 0, positionCell: attackerCell);

        TestUnit defender = new(atk: 0, def: 4, positionCell: defenderCell);

        CombatResolver.SkirmishResolution resolution = CombatResolver.ResolveSkirmish(attacker, defender, map: null);

        Assert.That(resolution.Succeeded, Is.False);

        Assert.That(resolution.Failure, Is.EqualTo(CombatResolver.SkirmishFailure.NotAdjacent));

        Assert.That(resolution.Result, Is.Null);
        Assert.That(attacker.ActionPoints, Is.EqualTo(4));
        Assert.That(attacker.Morale, Is.EqualTo(10));
        Assert.That(defender.Morale, Is.EqualTo(10));
    }

    [Test]
    public void ResolveSkirmish_RejectsInsufficientActionPoints()
    {
        HexCell attackerCell = new(new HexCoordinates(0, 0), type: null);

        HexCell defenderCell = new(new HexCoordinates(1, 0), type: null);

        TestUnit attacker = new(atk: 5, def: 0, positionCell: attackerCell, actionPoints: 0);

        TestUnit defender = new(atk: 0, def: 4, positionCell: defenderCell);

        CombatResolver.SkirmishResolution resolution = CombatResolver.ResolveSkirmish(attacker, defender, map: null);

        Assert.That(resolution.Succeeded, Is.False);

        Assert.That(resolution.Failure, Is.EqualTo(CombatResolver.SkirmishFailure.InsufficientActionPoints));

        Assert.That(attacker.ActionPoints, Is.Zero);
        Assert.That(attacker.Morale, Is.EqualTo(10));
        Assert.That(defender.Morale, Is.EqualTo(10));
    }
}

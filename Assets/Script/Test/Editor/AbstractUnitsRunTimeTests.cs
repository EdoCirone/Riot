using NUnit.Framework;
using UnityEngine;

public class AbstractUnitsRunTimeTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        private readonly bool _canBeArrested;

        protected override bool CanBeArrested => _canBeArrested;

        public TestUnit(
            int actionPoints,
            int morale = 10,
            bool canBeArrested = false)
            : base(
                new HexCell(new HexCoordinates(0, 0), null),
                UnitsStatus.Alive,
                morale,
                actionPoints)
        {
            _canBeArrested = canBeArrested;
            _positionCell.TryOccupy(this);
        }

        public override string DisplayName => "Test Unit";
        public override Sprite Avatar => null;
        public override int Atk => 0;
        public override int Def => 0;
        public override int AuraAtk => 0;
        public override int AuraDef => 0;
        public override int AuraMor => 0;
        public override GameObject GraphicsPrefab => null;

        public override bool CanPerformAction(ActionType action)
        {
            return true;
        }
    }

    [Test]
    public void TrySpendActionPoint_DecreasesAvailablePoints()
    {
        TestUnit unit = new TestUnit(actionPoints: 4);

        bool result = unit.TrySpendActionPoint(2);

        Assert.That(result, Is.True);
        Assert.That(unit.ActionPoints, Is.EqualTo(2));
    }

    [Test]
    public void TrySpendActionPoint_DoesNotSpendWhenPointsAreInsufficient()
    {
        TestUnit unit = new TestUnit(actionPoints: 2);

        bool result = unit.TrySpendActionPoint(3);

        Assert.That(result, Is.False);
        Assert.That(unit.ActionPoints, Is.EqualTo(2));
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void TrySpendActionPoint_RejectsNonPositiveAmounts(int amount)
    {
        TestUnit unit = new TestUnit(actionPoints: 4);

        bool result = unit.TrySpendActionPoint(amount);

        Assert.That(result, Is.False);
        Assert.That(unit.ActionPoints, Is.EqualTo(4));
    }

    [Test]
    public void RefillActionPoints_RestoresMaximumPoints()
    {
        TestUnit unit = new TestUnit(actionPoints: 4);
        unit.TrySpendActionPoint(3);

        unit.RefillActionPoints();

        Assert.That(unit.ActionPoints, Is.EqualTo(4));
        Assert.That(unit.MaxActionPoints, Is.EqualTo(4));
    }

    [Test]
    public void LoseMorale_DecreasesMoraleWithoutRemovingUnit()
    {
        TestUnit unit = new TestUnit(actionPoints: 4, morale: 10);
        HexCell cell = unit.PositionCell;

        unit.LoseMorale(3);

        Assert.That(unit.Morale, Is.EqualTo(7));
        Assert.That(unit.Status, Is.EqualTo(UnitsStatus.Alive));
        Assert.That(cell.OccupiedBy, Is.SameAs(unit));
    }

    [Test]
    public void GainMorale_DoesNotExceedMaximum()
    {
        TestUnit unit = new TestUnit(actionPoints: 4, morale: 10);
        unit.LoseMorale(4);

        unit.GainMorale(20);

        Assert.That(unit.Morale, Is.EqualTo(10));
        Assert.That(unit.MaxMorale, Is.EqualTo(10));
    }

    [Test]
    public void LoseMorale_WhenMoraleReachesZero_DispersesAndVacatesCell()
    {
        TestUnit unit = new TestUnit(actionPoints: 4, morale: 10);
        HexCell cell = unit.PositionCell;

        unit.LoseMorale(10, MoraleLossCause.Other);

        Assert.That(unit.Morale, Is.Zero);
        Assert.That(unit.Status, Is.EqualTo(UnitsStatus.Disperse));
        Assert.That(unit.IsAlive, Is.False);
        Assert.That(cell.OccupiedBy, Is.Null);
    }

    [Test]
    public void LoseMorale_FromPoliceContact_ArrestsArrestableUnit()
    {
        TestUnit unit = new TestUnit(
            actionPoints: 4,
            morale: 10,
            canBeArrested: true);

        HexCell cell = unit.PositionCell;

        unit.LoseMorale(10, MoraleLossCause.PoliceContact);

        Assert.That(unit.Morale, Is.Zero);
        Assert.That(unit.Status, Is.EqualTo(UnitsStatus.Arrested));
        Assert.That(cell.OccupiedBy, Is.Null);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void GainMorale_IgnoresNonPositiveAmounts(int amount)
    {
        TestUnit unit = new TestUnit(actionPoints: 4, morale: 10);
        unit.LoseMorale(4);

        unit.GainMorale(amount);

        Assert.That(unit.Morale, Is.EqualTo(6));
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void LoseMorale_IgnoresNonPositiveAmounts(int amount)
    {
        TestUnit unit = new TestUnit(actionPoints: 4, morale: 10);

        unit.LoseMorale(amount);

        Assert.That(unit.Morale, Is.EqualTo(10));
        Assert.That(unit.Status, Is.EqualTo(UnitsStatus.Alive));
    }
}

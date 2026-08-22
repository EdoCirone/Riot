using NUnit.Framework;
using UnityEngine;

public class HexCellTests
{
    private sealed class TestUnit : AbstractUnitsRunTime
    {
        public TestUnit(HexCell position)
            : base(position, UnitsStatus.Alive, 10, 4)
        {
            position.TryOccupy(this);
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

    private static HexCell CreateCell(int q, int r)
    {
        return new HexCell(new HexCoordinates(q, r), null);
    }

    [Test]
    public void SetPosition_MovesUnitAndUpdatesBothCells()
    {
        HexCell origin = CreateCell(0, 0);
        HexCell destination = CreateCell(1, 0);
        TestUnit unit = new TestUnit(origin);

        bool moved = unit.SetPosition(destination);

        Assert.That(moved, Is.True);
        Assert.That(unit.PositionCell, Is.SameAs(destination));
        Assert.That(origin.OccupiedBy, Is.Null);
        Assert.That(destination.OccupiedBy, Is.SameAs(unit));
    }

    [Test]
    public void SetPosition_DoesNotMoveIntoOccupiedCell()
    {
        HexCell origin = CreateCell(0, 0);
        HexCell destination = CreateCell(1, 0);

        TestUnit movingUnit = new TestUnit(origin);
        TestUnit occupyingUnit = new TestUnit(destination);

        bool moved = movingUnit.SetPosition(destination);

        Assert.That(moved, Is.False);
        Assert.That(movingUnit.PositionCell, Is.SameAs(origin));
        Assert.That(origin.OccupiedBy, Is.SameAs(movingUnit));
        Assert.That(destination.OccupiedBy, Is.SameAs(occupyingUnit));
    }

    [Test]
    public void SetPosition_ReturnsFalseWhenDestinationIsNull()
    {
        HexCell origin = CreateCell(0, 0);
        TestUnit unit = new TestUnit(origin);

        bool moved = unit.SetPosition(null);

        Assert.That(moved, Is.False);
        Assert.That(unit.PositionCell, Is.SameAs(origin));
        Assert.That(origin.OccupiedBy, Is.SameAs(unit));
    }

    [Test]
    public void Vacate_DoesNotRemoveDifferentUnit()
    {
        HexCell ownerCell = CreateCell(0, 0);
        HexCell otherCell = CreateCell(1, 0);

        TestUnit owner = new TestUnit(ownerCell);
        TestUnit other = new TestUnit(otherCell);

        ownerCell.Vacate(other);

        Assert.That(ownerCell.OccupiedBy, Is.SameAs(owner));
    }

    [Test]
    public void TryOccupy_ReturnsFalseForNullUnit()
    {
        HexCell cell = CreateCell(0, 0);

        bool occupied = cell.TryOccupy(null);

        Assert.That(occupied, Is.False);
        Assert.That(cell.OccupiedBy, Is.Null);
    }

    [Test]
    public void TryPlaceBarricade_ReturnsFalseForNullBarricade()
    {
        HexCell cell = CreateCell(0, 0);

        bool placed = cell.TryPlaceBarricade(null);

        Assert.That(placed, Is.False);
        Assert.That(cell.Barricade, Is.Null);
    }

    [Test]
    public void Barricade_BlocksMovementUntilRemoved()
    {
        HexCell origin = CreateCell(0, 0);
        HexCell destination = CreateCell(1, 0);
        TestUnit unit = new TestUnit(origin);
        BarricadeRuntime barricade = new BarricadeRuntime(null);

        bool placed = destination.TryPlaceBarricade(barricade);
        bool movedWhileBlocked = unit.SetPosition(destination);

        destination.RemoveBarricade();
        bool movedAfterRemoval = unit.SetPosition(destination);

        Assert.That(placed, Is.True);
        Assert.That(movedWhileBlocked, Is.False);
        Assert.That(movedAfterRemoval, Is.True);
        Assert.That(origin.OccupiedBy, Is.Null);
        Assert.That(destination.OccupiedBy, Is.SameAs(unit));
    }
}

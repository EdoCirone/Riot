using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PoliceReturnTests
{
    private PoliceSO _policeData;
    private HexTypeSO _walkableType;

    [SetUp]
    public void SetUp()
    {
        _policeData = ScriptableObject.CreateInstance<PoliceSO>();
        _walkableType = ScriptableObject.CreateInstance<HexTypeSO>();

        SerializedObject serializedType = new(_walkableType);
        serializedType.FindProperty("_isWalkable").boolValue = true;
        serializedType.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_policeData);
        Object.DestroyImmediate(_walkableType);
    }

    private HexCell Cell(int q, int r)
    {
        return new HexCell(
            new HexCoordinates(q, r),
            _walkableType
        );
    }

    private PoliceRuntime CreatePolice(HexCell position)
    {
        return new PoliceRuntime(
            position,
            UnitsStatus.Alive,
            _policeData,
            morale: 10,
            actionPoint: 4
        );
    }

    [Test]
    public void TryReturnToBoard_WhenDispersed_RestoresUnit()
    {
        HexCell startingCell = Cell(0, 0);
        HexCell returnCell = Cell(1, 0);
        PoliceRuntime police = CreatePolice(startingCell);

        police.TrySpendActionPoint(3);
        police.ApplyPanic(2);
        police.RaiseAlarm(3);
        police.LoseMorale(10, MoraleLossCause.Other);

        Assert.That(police.Status, Is.EqualTo(UnitsStatus.Disperse));
        Assert.That(startingCell.OccupiedBy, Is.Null);

        bool returned = police.TryReturnToBoard(returnCell);

        Assert.That(returned, Is.True);
        Assert.That(police.Status, Is.EqualTo(UnitsStatus.Alive));
        Assert.That(police.PositionCell, Is.SameAs(returnCell));
        Assert.That(returnCell.OccupiedBy, Is.SameAs(police));

        Assert.That(police.Morale, Is.EqualTo(police.MaxMorale));
        Assert.That(police.ActionPoints, Is.EqualTo(police.MaxActionPoints));
        Assert.That(police.IsPanicked, Is.False);
        Assert.That(police.IsAlarmed, Is.False);
    }
    [Test]
    public void TryReturnToBoard_WhenCellIsOccupied_LeavesUnitDispersed()
    {
        HexCell startingCell = Cell(0, 0);
        HexCell occupiedCell = Cell(1, 0);

        PoliceRuntime returningPolice = CreatePolice(startingCell);
        PoliceRuntime blockingPolice = CreatePolice(occupiedCell);

        returningPolice.LoseMorale(10, MoraleLossCause.Other);

        bool returned =
            returningPolice.TryReturnToBoard(occupiedCell);

        Assert.That(returned, Is.False);
        Assert.That(
            returningPolice.Status,
            Is.EqualTo(UnitsStatus.Disperse)
        );

        Assert.That(startingCell.OccupiedBy, Is.Null);
        Assert.That(
            occupiedCell.OccupiedBy,
            Is.SameAs(blockingPolice)
        );
    }
}

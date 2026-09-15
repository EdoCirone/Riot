using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MeetingPointCapacityQueryTests
{
    private HexMapSO _map;
    private HexTypeSO _regularType;
    private HexTypeSO _meetingGroundType;
    private MeetingPointSO _meetingPoint;

    [SetUp]
    public void SetUp()
    {
        _regularType =
            ScriptableObject.CreateInstance<HexTypeSO>();

        _meetingGroundType =
            ScriptableObject.CreateInstance<HexTypeSO>();

        SerializedObject serializedType =
            new SerializedObject(_meetingGroundType);

        serializedType
            .FindProperty("_isMeetingGround")
            .boolValue = true;

        serializedType.ApplyModifiedPropertiesWithoutUndo();

        _map =
            ScriptableObject.CreateInstance<HexMapSO>();

        _map.Initialize(
            width: 5,
            height: 5,
            defaultType: _regularType);

        _meetingPoint =
            ScriptableObject.CreateInstance<MeetingPointSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_meetingPoint);
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_meetingGroundType);
        Object.DestroyImmediate(_regularType);
    }

    [Test]
    public void TryCalculate_WithMissingInput_ReturnsFalse()
    {
        bool missingMap =
            MeetingPointCapacityQuery.TryCalculate(
                null,
                _meetingPoint,
                out int missingMapCapacity);

        bool missingMeetingPoint =
            MeetingPointCapacityQuery.TryCalculate(
                _map,
                null,
                out int missingMeetingPointCapacity);

        Assert.That(missingMap, Is.False);
        Assert.That(missingMapCapacity, Is.Zero);

        Assert.That(missingMeetingPoint, Is.False);
        Assert.That(missingMeetingPointCapacity, Is.Zero);
    }

    [Test]
    public void TryCalculate_WhenAnchorIsNotMeetingGround_ReturnsFalse()
    {
        SetAnchor(new HexCoordinates(2, 1));

        bool result =
            MeetingPointCapacityQuery.TryCalculate(
                _map,
                _meetingPoint,
                out int capacity);

        Assert.That(result, Is.False);
        Assert.That(capacity, Is.Zero);
    }

    [Test]
    public void TryCalculate_WithSingleBorderCell_ReturnsOne()
    {
        SetAnchor(new HexCoordinates(0, 0));

        _map.SetCellType(
            col: 0,
            row: 0,
            type: _meetingGroundType);

        bool result =
            MeetingPointCapacityQuery.TryCalculate(
                _map,
                _meetingPoint,
                out int capacity);

        Assert.That(result, Is.True);
        Assert.That(capacity, Is.EqualTo(1));
    }

    [Test]
    public void TryCalculate_CountsOnlyConnectedMeetingGround()
    {
        SetAnchor(new HexCoordinates(2, 1));

        _map.SetCellType(
            col: 2,
            row: 2,
            type: _meetingGroundType);

        _map.SetCellType(
            col: 3,
            row: 2,
            type: _meetingGroundType);

        _map.SetCellType(
            col: 2,
            row: 3,
            type: _meetingGroundType);

        _map.SetCellType(
            col: 0,
            row: 0,
            type: _meetingGroundType);

        bool result =
            MeetingPointCapacityQuery.TryCalculate(
                _map,
                _meetingPoint,
                out int capacity);

        Assert.That(result, Is.True);
        Assert.That(capacity, Is.EqualTo(3));
    }

    private void SetAnchor(HexCoordinates coordinates)
    {
        SerializedObject serializedMeetingPoint =
            new SerializedObject(_meetingPoint);

        SerializedProperty anchor =
            serializedMeetingPoint.FindProperty("_anchor");

        anchor.FindPropertyRelative("Q").intValue =
            coordinates.Q;

        anchor.FindPropertyRelative("R").intValue =
            coordinates.R;

        serializedMeetingPoint.ApplyModifiedPropertiesWithoutUndo();
    }
}

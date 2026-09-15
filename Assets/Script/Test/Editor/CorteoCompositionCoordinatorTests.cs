using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CorteoCompositionCoordinatorTests
{
    private GameObject _budgetObject;
    private AssemblyBudgetState _budget;
    private CorteoSelectionSO _selection;
    private SpezzoneSO _unit;
    private CorteoCompositionCoordinator _coordinator;
    private HexMapSO _map;
    private HexTypeSO _meetingGroundType;
    private MeetingPointSO _meetingPoint;

    [SetUp]
    public void SetUp()
    {
        _budgetObject =
            new GameObject("CorteoCompositionCoordinatorTests");

        _budget =
            _budgetObject.AddComponent<AssemblyBudgetState>();

        _selection =
            ScriptableObject.CreateInstance<CorteoSelectionSO>();

        _unit =
            ScriptableObject.CreateInstance<SpezzoneSO>();

        _coordinator =
            new CorteoCompositionCoordinator(
                _selection,
                _budget);
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
            width: 1,
            height: 1,
            defaultType: null);

        _map.SetCellType(
            col: 0,
            row: 0,
            type: _meetingGroundType);

        _meetingPoint =
            ScriptableObject.CreateInstance<MeetingPointSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_unit);
        Object.DestroyImmediate(_selection);
        Object.DestroyImmediate(_budgetObject);
        Object.DestroyImmediate(_meetingPoint);
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_meetingGroundType);
    }

    [Test]
    public void TryAdd_WhenAllowed_AddsUnit()
    {
        bool result = _coordinator.TryAdd(
            _unit,
            1);

        Assert.That(result, Is.True);
        Assert.That(_selection.Count, Is.EqualTo(1));
        Assert.That(_selection.CountOf(_unit), Is.EqualTo(1));
    }

    [Test]
    public void TryAdd_WhenBudgetIsInsufficient_DoesNotAddUnit()
    {
        Assert.That(_budget.TrySpend(13), Is.True);
        Assert.That(_budget.AvailablePoints, Is.EqualTo(2));

        bool result = _coordinator.TryAdd(
            _unit,
            1);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.Zero);
    }

    [Test]
    public void TryAdd_WhenCapacityIsReached_DoesNotAddUnit()
    {
        Assert.That(
            _coordinator.TryAdd(_unit, 1),
            Is.True);

        bool result = _coordinator.TryAdd(
            _unit,
            1);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.EqualTo(1));
    }

    [Test]
    public void TryAdd_DoesNotSpendBudget()
    {
        int initialBasePoints = _budget.BasePoints;
        int initialTemporaryPoints = _budget.TemporaryPoints;

        bool result = _coordinator.TryAdd(
            _unit,
            1);

        Assert.That(result, Is.True);
        Assert.That(
            _budget.BasePoints,
            Is.EqualTo(initialBasePoints));
        Assert.That(
            _budget.TemporaryPoints,
            Is.EqualTo(initialTemporaryPoints));
    }

    [Test]
    public void TryRemoveOne_RemovesSelectedUnit()
    {
        Assert.That(
            _coordinator.TryAdd(_unit, 1),
            Is.True);

        bool result =
            _coordinator.TryRemoveOne(_unit);

        Assert.That(result, Is.True);
        Assert.That(_selection.Count, Is.Zero);
    }

    [Test]
    public void TryAdd_WithMissingDependencies_ReturnsFalse()
    {
        var missingSelection =
            new CorteoCompositionCoordinator(
                null,
                _budget);

        var missingBudget =
            new CorteoCompositionCoordinator(
                _selection,
                null);

        Assert.That(
            missingSelection.TryAdd(_unit, 1),
            Is.False);

        Assert.That(
            missingBudget.TryAdd(_unit, 1),
            Is.False);
    }
    [Test]
    public void TryAdd_WithMapAndMeetingPoint_UsesCalculatedCapacity()
    {
        bool firstResult = _coordinator.TryAdd(
            _unit,
            _map,
            _meetingPoint);

        bool secondResult = _coordinator.TryAdd(
            _unit,
            _map,
            _meetingPoint);

        Assert.That(firstResult, Is.True);
        Assert.That(secondResult, Is.False);
        Assert.That(_selection.Count, Is.EqualTo(1));
    }

    [Test]
    public void TryAdd_WhenMeetingPointIsInvalid_DoesNotAddUnit()
    {
        _map.SetCellType(
            col: 0,
            row: 0,
            type: null);

        bool result = _coordinator.TryAdd(
            _unit,
            _map,
            _meetingPoint);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.Zero);
    }
}

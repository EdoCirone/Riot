using NUnit.Framework;
using UnityEngine;

public sealed class CorteoSelectionSOTests
{
    private CorteoSelectionSO _selection;
    private SpezzoneSO _unit;

    [SetUp]
    public void SetUp()
    {
        _selection =
            ScriptableObject.CreateInstance<CorteoSelectionSO>();

        _unit =
            ScriptableObject.CreateInstance<SpezzoneSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_selection);
        Object.DestroyImmediate(_unit);
    }

    [Test]
    public void NewSelection_IsEmpty()
    {
        Assert.That(_selection.HasSelection, Is.False);
        Assert.That(_selection.Count, Is.Zero);
        Assert.That(_selection.SelectedUnits, Is.Empty);
    }

    [Test]
    public void TryAdd_WithAvailableCapacity_AddsUnit()
    {
        bool result = _selection.TryAdd(_unit, 3);

        Assert.That(result, Is.True);
        Assert.That(_selection.HasSelection, Is.True);
        Assert.That(_selection.Count, Is.EqualTo(1));
        Assert.That(_selection.CountOf(_unit), Is.EqualTo(1));
    }

    [Test]
    public void TryAdd_AllowsNinthUnit_WhenCapacityIsNine()
    {
        for (int i = 0; i < 9; i++)
        {
            Assert.That(
                _selection.TryAdd(_unit, 9),
                Is.True);
        }

        Assert.That(_selection.Count, Is.EqualTo(9));
        Assert.That(_selection.CountOf(_unit), Is.EqualTo(9));
    }

    [Test]
    public void TryAdd_WhenCapacityIsFull_DoesNotAddUnit()
    {
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);

        bool result = _selection.TryAdd(_unit, 2);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.EqualTo(2));
    }

    [Test]
    public void TryAdd_WithNullUnit_DoesNotChangeSelection()
    {
        bool result = _selection.TryAdd(null, 3);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.Zero);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void TryAdd_WithNonPositiveCapacity_DoesNotChangeSelection(
        int capacity)
    {
        bool result = _selection.TryAdd(_unit, capacity);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.Zero);
    }

    [Test]
    public void TryRemoveOne_RemovesOnlyOneOccurrence()
    {
        Assert.That(_selection.TryAdd(_unit, 3), Is.True);
        Assert.That(_selection.TryAdd(_unit, 3), Is.True);

        bool result = _selection.TryRemoveOne(_unit);

        Assert.That(result, Is.True);
        Assert.That(_selection.Count, Is.EqualTo(1));
        Assert.That(_selection.CountOf(_unit), Is.EqualTo(1));
    }

    [Test]
    public void TryRemoveOne_WhenUnitIsAbsent_DoesNotChangeSelection()
    {
        bool result = _selection.TryRemoveOne(_unit);

        Assert.That(result, Is.False);
        Assert.That(_selection.Count, Is.Zero);
    }

    [Test]
    public void ClearSelection_RemovesAllUnits()
    {
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);

        _selection.ClearSelection();

        Assert.That(_selection.HasSelection, Is.False);
        Assert.That(_selection.Count, Is.Zero);
        Assert.That(_selection.SelectedUnits, Is.Empty);
    }

    [Test]
    public void TotalActivationCost_SumsEverySelectedUnit()
    {
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);

        Assert.That(
            _selection.TotalActivationCost,
            Is.EqualTo(6));
    }
}

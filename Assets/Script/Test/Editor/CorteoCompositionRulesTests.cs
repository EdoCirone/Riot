using NUnit.Framework;
using UnityEngine;

public sealed class CorteoCompositionRulesTests
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
    public void CanAdd_WhenCapacityAndBudgetAllow_ReturnsTrue()
    {
        bool result = CorteoCompositionRules.CanAdd(
            _selection,
            _unit,
            1,
            3);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAdd_WhenBudgetIsInsufficient_ReturnsFalse()
    {
        bool result = CorteoCompositionRules.CanAdd(
            _selection,
            _unit,
            1,
            2);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanAdd_WhenSelectionReachedCapacity_ReturnsFalse()
    {
        Assert.That(_selection.TryAdd(_unit, 1), Is.True);

        bool result = CorteoCompositionRules.CanAdd(
            _selection,
            _unit,
            1,
            100);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanAdd_IncludesExistingSelectionCost()
    {
        Assert.That(_selection.TryAdd(_unit, 2), Is.True);

        bool result = CorteoCompositionRules.CanAdd(
            _selection,
            _unit,
            2,
            5);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanAdd_DoesNotModifySelection()
    {
        CorteoCompositionRules.CanAdd(
            _selection,
            _unit,
            1,
            3);

        Assert.That(_selection.Count, Is.Zero);
        Assert.That(_selection.TotalActivationCost, Is.Zero);
    }
}

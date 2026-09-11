using NUnit.Framework;
using UnityEngine;

public sealed class AssemblyBudgetStateTests
{
    private GameObject _gameObject;
    private AssemblyBudgetState _budget;

    [SetUp]
    public void SetUp()
    {
        _gameObject =
            new GameObject("AssemblyBudgetStateTests");

        _budget =
            _gameObject.AddComponent<AssemblyBudgetState>();

        Assert.That(
            _budget.TrySetFlyerBonus(
                15 * 60,
                17 * 60),
            Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void TrySpend_ConsumesBonusBeforeBasePoints()
    {
        bool result = _budget.TrySpend(4);

        Assert.That(result, Is.True);
        Assert.That(_budget.BasePoints, Is.EqualTo(15));
        Assert.That(_budget.TemporaryPoints, Is.EqualTo(8));
        Assert.That(_budget.AvailablePoints, Is.EqualTo(23));
    }

    [Test]
    public void TrySpend_WhenCostExceedsBonus_UsesBaseForRemainder()
    {
        bool result = _budget.TrySpend(14);

        Assert.That(result, Is.True);
        Assert.That(_budget.TemporaryPoints, Is.Zero);
        Assert.That(_budget.BasePoints, Is.EqualTo(13));
        Assert.That(_budget.AvailablePoints, Is.EqualTo(13));
    }

    [Test]
    public void TrySpend_WhenCostEqualsAvailablePoints_LeavesZero()
    {
        bool result = _budget.TrySpend(27);

        Assert.That(result, Is.True);
        Assert.That(_budget.TemporaryPoints, Is.Zero);
        Assert.That(_budget.BasePoints, Is.Zero);
        Assert.That(_budget.AvailablePoints, Is.Zero);
    }

    [Test]
    public void TrySpend_WhenCostExceedsAvailablePoints_DoesNotChangeBudget()
    {
        bool result = _budget.TrySpend(28);

        Assert.That(result, Is.False);
        Assert.That(_budget.BasePoints, Is.EqualTo(15));
        Assert.That(_budget.TemporaryPoints, Is.EqualTo(12));
        Assert.That(_budget.AvailablePoints, Is.EqualTo(27));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void TrySpend_WithNonPositiveCost_DoesNotChangeBudget(
        int cost)
    {
        bool result = _budget.TrySpend(cost);

        Assert.That(result, Is.False);
        Assert.That(_budget.BasePoints, Is.EqualTo(15));
        Assert.That(_budget.TemporaryPoints, Is.EqualTo(12));
    }
}

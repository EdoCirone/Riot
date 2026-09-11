using NUnit.Framework;

public sealed class AssemblyBudgetRulesTests
{
    [TestCase(12 * 60, 17 * 60, true, 0)]
    [TestCase(12 * 60 + 15, 17 * 60, true, 1)]
    [TestCase(14 * 60, 17 * 60, true, 8)]
    [TestCase(16 * 60, 17 * 60, true, 16)]
    [TestCase(16 * 60 + 15, 17 * 60, false, 0)]
    [TestCase(12 * 60 + 7, 17 * 60, false, 0)]
    [TestCase(12 * 60, 12 * 60 + 59, false, 0)]
    public void TryCalculateTemporaryBonus_ReturnsExpectedResult(
        int startMinutes,
        int deadlineMinutes,
        bool expectedSuccess,
        int expectedBonus)
    {
        bool success =
            AssemblyBudgetRules.TryCalculateTemporaryBonus(
                startMinutes,
                deadlineMinutes,
                out int bonus);

        Assert.That(success, Is.EqualTo(expectedSuccess));
        Assert.That(bonus, Is.EqualTo(expectedBonus));
    }
}

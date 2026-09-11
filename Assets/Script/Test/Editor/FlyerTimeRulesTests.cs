using NUnit.Framework;

public sealed class FlyerTimeRulesTests
{
    [Test]
    public void CalculateLatestStartMinutes_WithFivePmDeadline_ReturnsFourPm()
    {
        int result =
            FlyerTimeRules.CalculateLatestStartMinutes(17 * 60);

        Assert.That(result, Is.EqualTo(16 * 60));
    }

    [TestCase(12 * 60, 17 * 60, true)]
    [TestCase(16 * 60, 17 * 60, true)]
    [TestCase(16 * 60 + 15, 17 * 60, false)]
    [TestCase(11 * 60 + 45, 17 * 60, false)]
    [TestCase(12 * 60 + 7, 17 * 60, false)]
    [TestCase(12 * 60, 13 * 60, true)]
    [TestCase(12 * 60, 12 * 60 + 59, false)]
    [TestCase(-1, 17 * 60, false)]
    [TestCase(24 * 60, 17 * 60, false)]
    [TestCase(12 * 60, -1, false)]
    [TestCase(12 * 60, 24 * 60, false)]
    public void IsValidStartTime_ReturnsExpectedResult(
        int startMinutes,
        int deadlineMinutes,
        bool expected)
    {
        bool result = FlyerTimeRules.IsValidStartTime(
            startMinutes,
            deadlineMinutes);

        Assert.That(result, Is.EqualTo(expected));
    }
}

using NUnit.Framework;

public class LevelTimeRulesTests
{
    [TestCase(720, 0, 15, 720)]
    [TestCase(720, 1, 15, 735)]
    [TestCase(720, 20, 15, 1020)]
    public void CalculateCurrentMinutes_ReturnsExpectedTime(
        int startMinutes,
        int turnIndex,
        int minutesPerTurn,
        int expected)
    {
        int result = LevelTimeRules.CalculateCurrentMinutes(
            startMinutes,
            turnIndex,
            minutesPerTurn);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(1005, 1020, false)]
    [TestCase(1020, 1020, true)]
    [TestCase(1035, 1020, true)]
    public void HasReachedDeadline_ReturnsExpectedResult(
        int currentMinutes,
        int deadlineMinutes,
        bool expected)
    {
        bool result = LevelTimeRules.HasReachedDeadline(
            currentMinutes,
            deadlineMinutes);

        Assert.That(result, Is.EqualTo(expected));
    }
}

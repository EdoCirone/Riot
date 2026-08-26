using NUnit.Framework;

public class TensionRulesTests
{
    [TestCase(-10, 0)]
    [TestCase(0, 0)]
    [TestCase(30, 0)]
    [TestCase(31, 10)]
    [TestCase(60, 10)]
    [TestCase(61, 30)]
    [TestCase(90, 30)]
    [TestCase(91, 40)]
    [TestCase(100, 40)]
    [TestCase(110, 40)]
    public void GetInitialTension_ReturnsCompressedValue(int repression, int expected)
    {
        int result = TensionRules.GetInitialTension(repression);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(-10, EngagementRules.Containment)]
    [TestCase(0, EngagementRules.Containment)]
    [TestCase(29, EngagementRules.Containment)]
    [TestCase(30, EngagementRules.Engage)]
    [TestCase(59, EngagementRules.Engage)]
    [TestCase(60, EngagementRules.Sweep)]
    [TestCase(100, EngagementRules.Sweep)]
    [TestCase(110, EngagementRules.Sweep)]
    public void GetEngagementRules_ReturnsExpectedBand(int tension, EngagementRules expected)
    {
        EngagementRules result = TensionRules.GetEngagementRules(tension);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(20, 10, 30)]
    [TestCase(40, -20, 20)]
    [TestCase(95, 10, 100)]
    [TestCase(5, -10, 0)]
    [TestCase(100, int.MaxValue, 100)]
    [TestCase(0, int.MinValue, 0)]
    public void ApplyDelta_ClampsResultToScale(int current, int delta, int expected)
    {
        int result = TensionRules.ApplyDelta(current, delta);

        Assert.That(result, Is.EqualTo(expected));
    }
}

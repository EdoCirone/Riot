using NUnit.Framework;

public class LevelTensionTests
{
    [TestCase(
        -10,
        0,
        EngagementRules.Containment)]
    [TestCase(
        20,
        20,
        EngagementRules.Containment)]
    [TestCase(
        30,
        30,
        EngagementRules.Engage)]
    [TestCase(
        60,
        60,
        EngagementRules.Sweep)]
    [TestCase(
        110,
        100,
        EngagementRules.Sweep)]
    public void Constructor_ClampsValueAndAppliesInitialRules(
        int initial,
        int expectedValue,
        EngagementRules expectedRules)
    {
        LevelTension tension =
            new LevelTension(initial);

        Assert.That(
            tension.Current,
            Is.EqualTo(expectedValue)
        );

        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(expectedRules)
        );

        Assert.That(
            tension.HasPendingRulesChange,
            Is.False
        );
    }

    [TestCase(20, 10, 30)]
    [TestCase(95, 10, 100)]
    [TestCase(5, -10, 0)]
    public void Change_UpdatesAndClampsCurrentValue(
        int initial,
        int delta,
        int expected)
    {
        LevelTension tension =
            new LevelTension(initial);

        bool changed = tension.Change(delta);

        Assert.That(changed, Is.True);
        Assert.That(
            tension.Current,
            Is.EqualTo(expected)
        );
    }

    [Test]
    public void Change_ReturnsFalseWhenValueDoesNotChange()
    {
        LevelTension tension =
            new LevelTension(100);

        bool changed = tension.Change(10);

        Assert.That(changed, Is.False);
        Assert.That(tension.Current, Is.EqualTo(100));
    }

    [Test]
    public void Escalation_IsAppliedAtNextPoliceTurn()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.Change(10);

        Assert.That(
            tension.TargetRules,
            Is.EqualTo(EngagementRules.Engage)
        );

        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Containment)
        );

        Assert.That(
            tension.HasPendingRulesChange,
            Is.True
        );

        bool applied = tension.PreparePoliceTurn();

        Assert.That(applied, Is.True);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Engage)
        );

        Assert.That(
            tension.HasPendingRulesChange,
            Is.False
        );
    }

    [Test]
    public void ChangeWithinSameBand_DoesNotApplyRules()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.Change(5);

        bool applied = tension.PreparePoliceTurn();

        Assert.That(applied, Is.False);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Containment)
        );
    }

    [Test]
    public void ChangeCancelledBeforePoliceTurn_IsNotApplied()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.Change(10);
        tension.Change(-10);

        bool applied = tension.PreparePoliceTurn();

        Assert.That(applied, Is.False);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Containment)
        );

        Assert.That(
            tension.HasPendingRulesChange,
            Is.False
        );
    }

    [Test]
    public void CrossingSeveralBands_AppliesCurrentTarget()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.Change(40);

        bool applied = tension.PreparePoliceTurn();

        Assert.That(applied, Is.True);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Sweep)
        );
    }

    [Test]
    public void Deescalation_IsAppliedAtNextPoliceTurn()
    {
        LevelTension tension =
            new LevelTension(60);

        tension.Change(-1);

        bool applied = tension.PreparePoliceTurn();

        Assert.That(applied, Is.True);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Engage)
        );
    }
}

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
    public void Escalation_IsAppliedAfterAFullFollowingPlayerTurn()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.BeginPlayerTurn();
        tension.Change(10);

        bool appliedImmediately =
            tension.PreparePoliceTurn();

        Assert.That(appliedImmediately, Is.False);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Containment)
        );

        tension.BeginPlayerTurn();

        bool appliedAfterGraceTurn =
            tension.PreparePoliceTurn();

        Assert.That(appliedAfterGraceTurn, Is.True);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Engage)
        );
    }

    [Test]
    public void PendingEscalation_IsCancelledByDeescalation()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.BeginPlayerTurn();
        tension.Change(10);
        tension.PreparePoliceTurn();

        tension.BeginPlayerTurn();
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
    public void PendingChange_AppliesTheCurrentTargetBand()
    {
        LevelTension tension =
            new LevelTension(20);

        tension.BeginPlayerTurn();
        tension.Change(10);
        tension.PreparePoliceTurn();

        tension.BeginPlayerTurn();
        tension.Change(30);

        bool applied = tension.PreparePoliceTurn();

        Assert.That(applied, Is.True);
        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Sweep)
        );
    }

    [Test]
    public void Deescalation_UsesTheSameGracePeriod()
    {
        LevelTension tension =
            new LevelTension(60);

        tension.BeginPlayerTurn();
        tension.Change(-1);

        Assert.That(
            tension.PreparePoliceTurn(),
            Is.False
        );

        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Sweep)
        );

        tension.BeginPlayerTurn();

        Assert.That(
            tension.PreparePoliceTurn(),
            Is.True
        );

        Assert.That(
            tension.AppliedRules,
            Is.EqualTo(EngagementRules.Engage)
        );
    }
}

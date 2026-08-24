public sealed class LevelTension
{
    private bool _changeWasPendingAtPlayerTurnStart;

    public int Current { get; private set; }

    public EngagementRules AppliedRules
    {
        get;
        private set;
    }

    public EngagementRules TargetRules =>
        TensionRules.GetEngagementRules(Current);

    public bool HasPendingRulesChange =>
        TargetRules != AppliedRules;

    public LevelTension(int initialTension)
    {
        Current = TensionRules.ApplyDelta(
            initialTension,
            0
        );

        AppliedRules = TargetRules;
    }

    public bool Change(int delta)
    {
        int previous = Current;

        Current = TensionRules.ApplyDelta(
            Current,
            delta
        );

        return Current != previous;
    }

    public void BeginPlayerTurn()
    {
        _changeWasPendingAtPlayerTurnStart =
            HasPendingRulesChange;
    }

    public bool PreparePoliceTurn()
    {
        EngagementRules target = TargetRules;

        bool shouldApply =
            _changeWasPendingAtPlayerTurnStart
            && target != AppliedRules;

        _changeWasPendingAtPlayerTurnStart = false;

        if (!shouldApply)
            return false;

        AppliedRules = target;
        return true;
    }
}

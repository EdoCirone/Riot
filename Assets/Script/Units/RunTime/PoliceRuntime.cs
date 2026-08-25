using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _police;
    private ObjectiveRuntime _guardedObjective;
    private EngagementRules _engagementRules = EngagementRules.Containment;

    private int _leashRadius;
    private int _alarmTurnsLeft;
    private bool _overridesLeashRadius;

    /// <summary>Un'unità allarmata ignora il guinzaglio e ingaggia, a qualunque condotta.</summary>

    public EngagementRules EngagementRules => _engagementRules;
    public int LeashRadius => _leashRadius;
    public bool IsAlarmed => _alarmTurnsLeft > 0;
    private bool _overridesEngagementRules;

    public override string DisplayName => _police.DisplayName;

    public override Sprite Avatar => _police.Avatar;
    public override int Atk => _police.Atk;
    public override int Def => _police.Def;

    public override int AuraAtk => _police.AuraAtk;
    public override int AuraDef => _police.AuraDef;
    public override int AuraMor => _police.AuraMor;
    public bool OverridesEngagementRules => _overridesEngagementRules;
    public bool OverridesLeashRadius => _overridesLeashRadius;

    public override GameObject GraphicsPrefab => _police.GraphicsPrefab;
    public ObjectiveRuntime GuardedObjective => _guardedObjective;

    public PoliceRuntime(HexCell pos, UnitsStatus stato, PoliceSO police, int morale, int actionPoint)
        : base(pos, stato, morale, actionPoint)
    {
        _police = police;
        pos.TryOccupy(this);
    }

    public override bool CanPerformAction(ActionType action)
    {
        return _police.CanPerformAction(action);
    }
    public void AssignGuard(
     ObjectiveRuntime objective,
     EngagementRules rules,
     int leashRadius,
     bool overridesEngagementRules,
     bool overridesLeashRadius)
    {
        _guardedObjective = objective;
        _engagementRules = rules;
        _leashRadius = leashRadius;
        _overridesEngagementRules =
            overridesEngagementRules;
        _overridesLeashRadius =
            overridesLeashRadius;
    }

    public void RaiseAlarm(int turns) => _alarmTurnsLeft = Mathf.Max(_alarmTurnsLeft, turns);
    public void TickAlarm() { if (_alarmTurnsLeft > 0) _alarmTurnsLeft--; }

    public void ReassignGuard(ObjectiveRuntime objective)
    {
        _guardedObjective = objective;
    }

    public bool ApplyLevelEngagementRules(EngagementRules rules)
    {
        if (_overridesEngagementRules
            || _engagementRules == rules)
        {
            return false;
        }

        _engagementRules = rules;
        return true;
    }

    public bool ApplyLevelLeashRadius(
    int leashRadius)
    {
        if (_overridesLeashRadius
            || _leashRadius == leashRadius)
        {
            return false;
        }

        _leashRadius = leashRadius;
        return true;
    }
}

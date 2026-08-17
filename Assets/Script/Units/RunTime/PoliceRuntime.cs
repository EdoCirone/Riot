using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _police;
    private ObjectiveRuntime _guardedObjective;
    private EngagementRules _engagementRules = EngagementRules.Containment;
    private int _leashRadius;

    public EngagementRules EngagementRules => _engagementRules;
    public int LeashRadius => _leashRadius;

    public override string DisplayName => _police.DisplayName;

    public override Sprite Avatar => _police.Avatar;
    public override int Atk => _police.Atk;
    public override int Def => _police.Def;

    public override int AuraAtk => _police.AuraAtk;
    public override int AuraDef => _police.AuraDef;
    public override int AuraMor => _police.AuraMor;

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
    public void AssignGuard(ObjectiveRuntime objective, EngagementRules rules, int leashRadius)
    {
        _guardedObjective = objective;
        _engagementRules = rules;
        _leashRadius = leashRadius;
    }
}

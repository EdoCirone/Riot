using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _police;

    public override string DisplayName => _police.DisplayName;

    public override Sprite Avatar => _police.Avatar;
    public override int Atk => _police.Atk;
    public override int Def => _police.Def;

    public override int AuraAtk => _police.AuraAtk;
    public override int AuraDef => _police.AuraDef;
    public override int AuraMor => _police.AuraMor;

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
    public override GameObject GraphicsPrefab => _police.GraphicsPrefab;

}

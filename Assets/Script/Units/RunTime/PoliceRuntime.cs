using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _police;

    public override Sprite Avatar => _police.Avatar;
    public override int Atk => _police.Atk;
    public override int Def => _police.Def;

    public PoliceRuntime(HexCell pos, UnitsStatus stato, PoliceSO police, int morale, int actionPoint)
        : base(pos, stato, morale, actionPoint)
    {
        _police = police;
        pos.TryOccupy(this);
    }
    public override GameObject GraphicsPrefab => _police.GraphicsPrefab;

}

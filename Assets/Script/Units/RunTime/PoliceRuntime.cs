using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _police;

    public override int Atk => _police.Atk;
    public override int Def => _police.Def;
    public int Mov => _police.Mov;


    public PoliceRuntime(HexCell pos, UnitsStatus stato, PoliceSO police)
        : base(pos, stato)
    {
        _police = police;
        pos.TryOccupy(this);
    }
    public override GameObject GraphicsPrefab => _police.GraphicsPrefab;

}

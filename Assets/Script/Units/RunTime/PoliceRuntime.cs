using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _police;

    public int Atk => _police.Atk;
    public int Def => _police.Def;
    public int Mov => _police.Mov;


    public PoliceRuntime(HexCell pos, UnitsStatus stato, PoliceSO police)
        : base(pos, stato)
    {
        _police = police;
        pos.TryOccupy(this);
    }

}

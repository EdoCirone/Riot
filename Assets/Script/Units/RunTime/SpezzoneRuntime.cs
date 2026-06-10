using UnityEngine;

public class SpezzoneRuntime : AbstractUnitsRunTime
{
    private SpezzoneSO _spezzone;


    public int Atk => _spezzone.Atk;
    public int Def => _spezzone.Def;
    public int Mov => _spezzone.Mov;


    public SpezzoneRuntime(HexCell pos, UnitsStatus stato, SpezzoneSO spezzone)
        : base(pos, stato)
    {
        _spezzone = spezzone;
        pos.TryOccupy(this);
    }

}

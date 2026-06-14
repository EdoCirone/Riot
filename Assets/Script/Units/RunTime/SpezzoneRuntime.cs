using UnityEngine;

public class SpezzoneRuntime : AbstractUnitsRunTime
{
    private SpezzoneSO _spezzone;


    public override int Atk => _spezzone.Atk;
    public override int Def => _spezzone.Def;
    public int Mov => _spezzone.Mov;


    public SpezzoneRuntime(HexCell pos, UnitsStatus stato, SpezzoneSO spezzone)
        : base(pos, stato)
    {
        _spezzone = spezzone;
        pos.TryOccupy(this);
    }

    public override GameObject GraphicsPrefab => _spezzone.GraphicsPrefab;

}

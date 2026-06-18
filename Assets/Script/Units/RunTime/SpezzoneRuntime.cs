using UnityEngine;

public class SpezzoneRuntime : AbstractUnitsRunTime
{
    private SpezzoneSO _spezzone;


    public override int Atk => _spezzone.Atk;
    public override int Def => _spezzone.Def;

    public SpezzoneRuntime(HexCell pos, UnitsStatus stato, SpezzoneSO spezzone, int morale, int actionPoints)
     : base(pos, stato, morale, actionPoints)
    {
        _spezzone = spezzone;
        pos.TryOccupy(this);
    }

    public override GameObject GraphicsPrefab => _spezzone.GraphicsPrefab;

}

using UnityEngine;

public class SpezzoneRuntime : AbstractUnitsRunTime
{
    private SpezzoneSO _spezzone;
    private Inventory _inventory = new();

    public override int Atk => _spezzone.Atk;
    public override int Def => _spezzone.Def;
    public Inventory Inventory => _inventory;

    public SpezzoneRuntime(HexCell pos, UnitsStatus stato, SpezzoneSO spezzone, int morale, int actionPoints)
     : base(pos, stato, morale, actionPoints)
    {
        _spezzone = spezzone;
        pos.TryOccupy(this);
    }

    public override GameObject GraphicsPrefab => _spezzone.GraphicsPrefab;

}

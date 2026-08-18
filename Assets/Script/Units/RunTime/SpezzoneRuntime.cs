using UnityEngine;

public class SpezzoneRuntime : AbstractUnitsRunTime
{
    private SpezzoneSO _spezzone;
    private Inventory _inventory = new();

    public override string DisplayName => _spezzone.DisplayName;

    public override Sprite Avatar => _spezzone.Avatar;
    public override int Atk => _spezzone.Atk;
    public override int Def => _spezzone.Def + (_isSeated ? 5 : 0);

    public override int AuraAtk => _spezzone.AuraAtk;
    public override int AuraDef => _spezzone.AuraDef;
    public override int AuraMor => _spezzone.AuraMor;

    protected override bool CanBeArrested => true;

    public Inventory Inventory => _inventory;

    public SpezzoneRuntime(HexCell pos, UnitsStatus stato, SpezzoneSO spezzone, int morale, int actionPoints)
     : base(pos, stato, morale, actionPoints)
    {
        _spezzone = spezzone;
        pos.TryOccupy(this);
    }

    public override bool CanPerformAction(ActionType action)
    {
        return _spezzone.CanPerformAction(action);
    }

    public override GameObject GraphicsPrefab => _spezzone.GraphicsPrefab;

}

using UnityEngine;

public class SpezzoneRuntime : AbstractUnitsRunTime
{
    private SpezzoneSO _data;
    public Inventory Inventory { get; private set; }

    public override int Atk => _data.Atk;
    public override int Def => _data.Def;
    public override GameObject GraphicsPrefab => _data.GraphicsPrefab;

    public SpezzoneRuntime(
        HexCell positionCell,
        UnitsStatus status,
        SpezzoneSO data,
        int morale,
        int actionPoints,
        MoraleEventSO moraleEvent) 
        : base(positionCell, status, morale, actionPoints, moraleEvent)
    {
        _data = data;
        Inventory = new Inventory();
    }
}
using UnityEngine;

public class PoliceRuntime : AbstractUnitsRunTime
{
    private PoliceSO _data;

    public override int Atk => _data.Atk;
    public override int Def => _data.Def;
    public override GameObject GraphicsPrefab => _data.GraphicsPrefab;

    public PoliceRuntime(
        HexCell positionCell,
        UnitsStatus status,
        PoliceSO data,
        int morale,
        int actionPoints,
        MoraleEventSO moraleEvent) 
        : base(positionCell, status, morale, actionPoints, moraleEvent)
    {
        _data = data;
    }
}
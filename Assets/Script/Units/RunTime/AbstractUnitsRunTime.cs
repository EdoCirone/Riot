using UnityEngine;

public abstract class AbstractUnitsRunTime 
{
    protected HexCell _positionCell;
    protected UnitsStatus _status;

    public abstract int Atk { get; }
    public abstract int Def { get; }

    public HexCell PositionCell => _positionCell;
    public UnitsStatus Status => _status;

    public abstract GameObject GraphicsPrefab { get; }

    protected AbstractUnitsRunTime (HexCell positionCell, UnitsStatus status)
    {
        _positionCell = positionCell;
        _status = status;
    }

    public bool SetPosition(HexCell arriveCell)
    {
        bool isSucces = arriveCell.TryOccupy(this);
        if (isSucces)
        {
            _positionCell.Vacate();
            _positionCell = arriveCell;
        }
        return isSucces;
    }

    public void Disperse()
    {
        _status = UnitsStatus.Disperse;
        _positionCell.Vacate();
    }
}

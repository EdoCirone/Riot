using UnityEngine;

public abstract class AbstractUnitsRunTime 
{
    protected HexCell _positionCell;
    protected UnitsStatus _status;

    public HexCell PositionCell => _positionCell;
    public UnitsStatus Status => _status;

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
}

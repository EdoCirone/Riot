using UnityEngine;

public class SpezzoneRuntime
{
    private HexCell _positionCell;
    private UnitsStatus _status;

    public HexCell PositionCell => _positionCell;
    public UnitsStatus Status => _status;

    public SpezzoneRuntime(HexCell pos, UnitsStatus stato)
    {
        _positionCell = pos;
        _status = stato;
        pos.TryOccupy(this);
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

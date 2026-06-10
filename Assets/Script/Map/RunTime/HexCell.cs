using UnityEngine;

public class HexCell
{

    private HexCoordinates _coordinates;
    private HexTypeSO _type;
    private AbstractUnitsRunTime _occupiedBy;
    public AbstractUnitsRunTime OccupiedBy => _occupiedBy;


    public HexCoordinates Coordinates => _coordinates;
    public HexTypeSO Type => _type;

    public HexCell(HexCoordinates coordinates, HexTypeSO type)
    {

        _coordinates = coordinates;
        _type = type;
    }

    public bool TryOccupy(AbstractUnitsRunTime unit)
    {
        if (_occupiedBy == null)
        {
            _occupiedBy = unit;
            return true;
        }
        else
        {
            Debug.Log("try to occupy a not empty cell");
            return false;
        }
    }

    public void Vacate()
    {
        _occupiedBy = null;
    }
}

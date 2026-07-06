using UnityEngine;

public class HexCell
{

    private HexCoordinates _coordinates;
    private HexTypeSO _type;
    private AbstractUnitsRunTime _occupiedBy;
    private BarricadeRuntime _barricade;


    public AbstractUnitsRunTime OccupiedBy => _occupiedBy;
    public BarricadeRuntime Barricade => _barricade;

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

    public bool TryPlaceBarricade(BarricadeRuntime barricade)
    {
        if (_barricade == null && _occupiedBy == null)
        {
            _barricade = barricade;
            return true;
        }
        else
        {
            Debug.Log("try to place a barricade on a cell that already has one");
            return false;
        }
    }

    public void RemoveBarricade()
    {
        _barricade = null;
    }
}

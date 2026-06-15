using UnityEngine;

public class MovementOrder
{
    private AbstractUnitsRunTime _selectedUnit;
    private HexCell _directionCell;

    public AbstractUnitsRunTime SelectedUnit => _selectedUnit;
    public HexCell DirectionCell => _directionCell;

    public MovementOrder(AbstractUnitsRunTime selectedUnit, HexCell directionCell)
    {
        _selectedUnit = selectedUnit;
        _directionCell = directionCell;

    }
}

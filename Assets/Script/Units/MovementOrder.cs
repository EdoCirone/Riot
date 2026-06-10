using UnityEngine;

public class MovementOrder
{
    private SpezzoneRuntime _selectedSpezzone;
    private HexCell _directionCell;

    public SpezzoneRuntime SelectedSpezzone => _selectedSpezzone;
    public HexCell DirectionCell => _directionCell;

    public MovementOrder(SpezzoneRuntime selectedSpezzone, HexCell directionCell)
    {
        _selectedSpezzone = selectedSpezzone;
        _directionCell = directionCell;

    }
}

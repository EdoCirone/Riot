using UnityEngine;

public class UnitsSetup : MonoBehaviour
{

    [SerializeField] private UnitsSO _unit;
    [SerializeField] private HexGrid _grid;

    public AbstractUnitsRunTime Initialize()
    {
        if (_grid == null) return null;
        if (_unit == null) return null;

        HexCoordinates coord = HexCoordinates.FromWorldPosition(transform.position, _grid.CellSize);
        Debug.Log($"Setup {gameObject.name}: worldPos={transform.position}, coord={coord}");
        HexCell cell;
        bool found = _grid.TryGetCell(coord, out cell);
        Debug.Log($"TryGetCell result: {found}");

        if (_unit is PoliceSO police)
        {
            PoliceRuntime policeRuntime = new PoliceRuntime(cell, UnitsStatus.Alive, (PoliceSO) _unit);
            return policeRuntime;
        }
        else if(_unit is SpezzoneSO spezzone)
        {
            SpezzoneRuntime spezzoneRuntime = new SpezzoneRuntime(cell,UnitsStatus.Alive,(SpezzoneSO) _unit);
            return spezzoneRuntime;
        }

        return null;
    }

}

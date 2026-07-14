using UnityEngine;

public class UnitsSetup : MonoBehaviour
{
    [SerializeField] private UnitsSO _unit;
    [SerializeField] private HexGrid _grid;
    [SerializeField] private StartingItem[] _startingInventory;
    [SerializeField] private MoraleEventSO _moraleEvent;

    [System.Serializable]
    public struct StartingItem
    {
        public ItemSO item;
        public int quantity;
    }

    public AbstractUnitsRunTime Initialize()
    {
        if (_grid == null) return null;
        if (_unit == null) return null;
        if (_moraleEvent == null)
        {
            Debug.LogError($"[UnitsSetup] MoraleEvent is null on {gameObject.name}!");
            return null;
        }

        HexCoordinates coord = HexCoordinates.FromWorldPosition(transform.position, _grid.CellSize);
        HexCell cell;
        bool found = _grid.TryGetCell(coord, out cell);

        if (cell == null)
        {
            Debug.LogWarning($"No cell found at {coord} for {gameObject.name}, cannot initialize {_unit}");
            return null;
        }

        if (_unit is PoliceSO police)
        {
            PoliceRuntime policeRuntime = new PoliceRuntime(
                cell,
                UnitsStatus.Alive,
                police,
                police.Mor,
                police.ActionPoints,
                _moraleEvent 
            );
            return policeRuntime;
        }
        else if (_unit is SpezzoneSO spezzone)
        {
            SpezzoneRuntime spezzoneRuntime = new SpezzoneRuntime(
                cell,
                UnitsStatus.Alive,
                spezzone,
                spezzone.Mor,
                spezzone.ActionPoints,
                _moraleEvent 
            );

            foreach (var s in _startingInventory)
            {
                if (s.item == null || s.quantity <= 0) continue;
                spezzoneRuntime.Inventory.AddItem(s.item, s.quantity);
            }

            return spezzoneRuntime;
        }

        return null;
    }
}
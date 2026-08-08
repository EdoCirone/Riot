using UnityEngine;

public class UnitsSetup : MonoBehaviour
{

    [SerializeField] private UnitsSO _unit;
    [SerializeField] private HexGrid _grid;


    // in UnitsSetup — seeding provvisorio, sostituito dalla composizione corteo
    [SerializeField] private StartingItem[] _startingInventory;   // provvisorio

    [System.Serializable]
    public struct StartingItem { public ItemSO item; public int quantity; }


    public AbstractUnitsRunTime Initialize()
    {
        if (_grid == null) { Debug.LogWarning($"{name}: Grid not assigned"); return null; }
        if (_unit == null) { Debug.LogWarning($"{name}: Unit (SO) not assigned"); return null; }

        HexCoordinates coord = _grid.WorldToGrid(transform.position);
        //Debug.Log($"Setup {gameObject.name}: worldPos={transform.position}, coord={coord}");

        HexCell cell;
        bool found = _grid.TryGetCell(coord, out cell);
        //Debug.Log($"TryGetCell result: {found}");

        if (cell == null)
        {
            Debug.LogWarning($"No cell found at {coord} for {gameObject.name}, cannot initialize {_unit}");
            return null;
        }

        if (_unit is PoliceSO police)
        {
            PoliceRuntime policeRuntime = new PoliceRuntime(cell, UnitsStatus.Alive, police, police.Mor, police.ActionPoints);
            return policeRuntime;
        }
        else if (_unit is SpezzoneSO spezzone)
        {
            SpezzoneRuntime spezzoneRuntime = new SpezzoneRuntime(cell, UnitsStatus.Alive, spezzone, spezzone.Mor, spezzone.ActionPoints);

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

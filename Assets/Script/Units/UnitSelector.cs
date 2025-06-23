using System.Collections.Generic;
using UnityEngine;

public class UnitSelector : MonoBehaviour
{
    public static UnitSelector Instance;

    private CorteoUnit[] allUnits;
    private int lastSelectedSector = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        allUnits = FindObjectsByType<CorteoUnit>(FindObjectsSortMode.None);
    }

    public void SelectSector(int sector)
    {
        lastSelectedSector = sector;

        foreach (var unit in allUnits)
        {
            if (unit.sectorID == sector)
                unit.Select();
            else
                unit.Deselect();
        }

        Debug.Log($"Settore {sector} selezionato.");
    }
    public List<CorteoUnit> GetSelectedUnits()
    {
        List<CorteoUnit> selected = new List<CorteoUnit>();
        foreach (var unit in allUnits)
        {
            if (unit.sectorID == lastSelectedSector)
                selected.Add(unit);
        }
        return selected;
    }
}
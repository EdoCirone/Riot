using System.Collections.Generic;
using UnityEngine;

public class UnitsRenderer : MonoBehaviour
{

    [Header("Reference")]
    [SerializeField] private HexGrid _grid;

    private Dictionary<AbstractUnitsRunTime, GameObject> _unitsDict;

    private void Awake()
    {
        _unitsDict = new Dictionary<AbstractUnitsRunTime, GameObject>();
    }

    public void SpawnUnits(AbstractUnitsRunTime unit, GameObject existingGO)
    {
        _unitsDict.Add(unit, existingGO);
    }

    public void UpdateView(AbstractUnitsRunTime unit)
    {
        if (unit.Status == UnitsStatus.Disperse)
        {
            if (_unitsDict.TryGetValue(unit, out GameObject go))
            {
                go.SetActive(false);
            }
        }
        else if (_unitsDict.TryGetValue(unit, out GameObject go))
        {
            Vector3 newPos = unit.PositionCell.Coordinates.ToWorldPosition(_grid.CellSize);
            Debug.Log($"UpdateView: sposto {unit} a {newPos}");
            go.transform.position = newPos;
        }
        else
        {
            Debug.Log("try to update a null unit");
        }
    }
}
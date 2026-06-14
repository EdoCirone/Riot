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

    public void SpawnUnits(AbstractUnitsRunTime unit)
    {
        GameObject instance = Instantiate(unit.GraphicsPrefab, unit.PositionCell.Coordinates.ToWorldPosition(_grid.CellSize), Quaternion.identity);
        _unitsDict.Add(unit, instance);
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
            go.transform.position = unit.PositionCell.Coordinates.ToWorldPosition(_grid.CellSize);
        }
        else
        {
            Debug.Log("try to update a null unit");
        }

    }
}

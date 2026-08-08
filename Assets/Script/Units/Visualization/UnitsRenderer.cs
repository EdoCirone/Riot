using System.Collections.Generic;
using UnityEngine;

public class UnitsRenderer : MonoBehaviour
{

    [Header("Reference")]
    [SerializeField] private HexGrid _grid;

    private Dictionary<AbstractUnitsRunTime, GameObject> _unitsDict;

    public GameObject GetGameObject(AbstractUnitsRunTime unit)
    {
        if (_unitsDict.TryGetValue(unit, out GameObject go))
            return go;
        return null;
    }

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
        if (!_unitsDict.TryGetValue(unit, out GameObject go))
        {
            Debug.Log("UpdateView: unit not registered in the renderer");
            return;
        }

        UnitMovement movement = go.GetComponent<UnitMovement>();

        if (!unit.IsAlive)
        {
            movement?.SetPanicVisual(false);      
            go.transform.root.gameObject.SetActive(false);
            return;
        }

        go.transform.root.position = _grid.GridToWorld(unit.PositionCell.Coordinates);
        movement?.SetPanicVisual(unit.IsPanicked);
    }
}
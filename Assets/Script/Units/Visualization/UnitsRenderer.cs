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

    /// <summary>
    /// Lampo di danno. Va chiamato al momento dell'IMPATTO dell'animazione, non quando
    /// la logica applica il danno: le due cose sono separate da tutta la durata
    /// dell'animazione, ed è il senso di "logica prima, animazione dopo".
    /// </summary>
    public void FlashDamage(AbstractUnitsRunTime unit)
    {
        if (unit == null) return;
        if (!_unitsDict.TryGetValue(unit, out GameObject go)) return;
        go.GetComponent<UnitStatusView>()?.Flash();
    }

    public void UpdateView(AbstractUnitsRunTime unit)
    {
        if (!_unitsDict.TryGetValue(unit, out GameObject go))
        {
            Debug.Log("UpdateView: unit not registered in the renderer");
            return;
        }

        UnitStatusView statusView = go.GetComponent<UnitStatusView>();

        if (!unit.IsAlive)
        {
            statusView?.Clear();
            go.transform.root.gameObject.SetActive(false);
            return;
        }

        go.transform.root.position = _grid.GridToWorld(unit.PositionCell.Coordinates);
        statusView?.Refresh(unit.IsPanicked, unit.IsSeated);
    }
}

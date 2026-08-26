using UnityEngine;

public class UnitsSetup : MonoBehaviour
{
    [SerializeField] private UnitsSO _unit;

    // in UnitsSetup — seeding provvisorio, sostituito dalla composizione corteo
    [Header("Inventory iniziale (solo spezzoni)")]
    [Tooltip("seeding provvisorio, sostituito dalla composizione corteo")]
    [SerializeField] private StartingItem[] _startingInventory;   // provvisorio

    [Header("Polizia")]
    [Tooltip("Obiettivo presidiato. Se vuoto, viene assegnato quello più vicino allo spawn.")]
    [SerializeField] private ObjectiveSO _guardedObjective;
    [Tooltip("If off, this unit follows the level's engagement rules.")]
    [SerializeField] private bool _overrideEngagement;

    [Tooltip("Engagement rules for this unit. Only used when the override is on.")]
    [SerializeField] private EngagementRules _engagementRules = EngagementRules.Containment;

    [Tooltip("Leash radius for this unit. -1 uses the level's value.")]
    [SerializeField] private int _leashRadiusOverride = -1;

    public bool OverrideEngagement => _overrideEngagement;
    public EngagementRules EngagementRules => _engagementRules;
    public int LeashRadiusOverride => _leashRadiusOverride;

    public ObjectiveSO GuardedObjective => _guardedObjective;

    [System.Serializable]
    public struct StartingItem { public ItemSO item; public int quantity; }

    /// <summary>
    /// Crea il Runtime dell'unità. Se <paramref name="startCell"/> è null la cella viene
    /// dedotta dalla posizione nel mondo — è il caso delle unità piazzate a mano in scena,
    /// oggi la polizia. Se è valorizzata, l'unità nasce lì: è il caso dello spawn a runtime
    /// dal punto di ritrovo.
    /// ⚠ La griglia arriva da fuori e non è più un campo serializzato: un prefab non può
    /// tenere un riferimento a un oggetto di scena.
    /// </summary>
    public AbstractUnitsRunTime Initialize(HexGrid grid, HexCell startCell = null)
    {
        if (grid == null) { Debug.LogWarning($"{name}: grid not provided"); return null; }
        if (_unit == null) { Debug.LogWarning($"{name}: Unit (SO) not assigned"); return null; }

        HexCell cell = startCell;

        if (cell == null)
        {
            HexCoordinates coord = grid.WorldToGrid(transform.position);
            if (!grid.TryGetCell(coord, out cell))
            {
                Debug.LogWarning($"No cell found at {coord} for {gameObject.name}, cannot initialize {_unit}");
                return null;
            }
        }

        // ⚠ Verifica PRIMA di costruire: i costruttori Runtime chiamano TryOccupy ma ne
        // buttano il risultato, quindi un'unità nata su una cella occupata esisterebbe
        // con la griglia che punta a un'altra — e il suo Vacate cancellerebbe quella.
        if (!TacticalQuery.IsCellAvailable(cell))
        {
            Debug.LogError($"{gameObject.name}: cell {cell.Coordinates} is not free, {_unit} not spawned");
            return null;
        }

        if (_unit is PoliceSO police)
        {
            return new PoliceRuntime(cell, UnitsStatus.Alive, police, police.Mor, police.ActionPoints);
        }

        if (_unit is SpezzoneSO spezzone)
        {
            SpezzoneRuntime spezzoneRuntime =
                new(cell, UnitsStatus.Alive, spezzone, spezzone.Mor, spezzone.ActionPoints);

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

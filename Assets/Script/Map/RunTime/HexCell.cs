using UnityEngine;

public class HexCell
{

    private HexCoordinates _coordinates;
    private HexTypeSO _type;
    private AbstractUnitsRunTime _occupiedBy;
    private BarricadeRuntime _barricade;
    private ObjectiveRuntime _objective;

    /// <summary>L'obiettivo di cui questa cella fa parte, se ce n'è uno.</summary>
    public ObjectiveRuntime Objective => _objective;
    public AbstractUnitsRunTime OccupiedBy => _occupiedBy;
    public BarricadeRuntime Barricade => _barricade;
    public HexCoordinates Coordinates => _coordinates;
    public HexTypeSO Type => _type;

    /// <summary>
    /// ⚠ Questa è la verità su "questa cella è un obiettivo", non `Type.IsObjective`.
    /// Il tipo serve a dipingere; l'appartenenza a un obiettivo la decide l'ObjectiveSO.
    /// </summary>
    public bool IsObjective => _objective != null;

    public HexCell(HexCoordinates coordinates, HexTypeSO type)
    {

        _coordinates = coordinates;
        _type = type;
    }

    public bool TryOccupy(AbstractUnitsRunTime unit)
    {
        if(_barricade != null)
        {
            Debug.Log($"try to occupy a cell with a barricade {_coordinates}");
            return false;
        }

        if (_occupiedBy == null)
        {
            _occupiedBy = unit;
            return true;
        }
        else
        {
            Debug.Log($"try to occupy a not empty cell {_coordinates}");
            return false;
        }
    }

    public void Vacate()
    {
        _occupiedBy = null;
    }

    public bool TryPlaceBarricade(BarricadeRuntime barricade)
    {
        if (_barricade == null && _occupiedBy == null)
        {
            _barricade = barricade;
            return true;
        }
        else
        {
            Debug.Log($"try to place a barricade on a cell that already has one {_coordinates}");
            return false;
        }
    }

    public void BindObjective(ObjectiveRuntime objective) => _objective = objective;

    // ⚠ Riservato: nessun chiamante oggi. Servirà quando la polizia potrà spendere
    // punti azione per rimuovere una barricata (non ancora progettato come costo).
    // Non è codice morto: non cancellare.
    public void RemoveBarricade()
    {
        _barricade = null;
    }
}

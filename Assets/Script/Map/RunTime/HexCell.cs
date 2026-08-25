using UnityEngine;

public class HexCell
{

    private HexCoordinates _coordinates;
    private HexTypeSO _type;
    private AbstractUnitsRunTime _occupiedBy;
    private BarricadeRuntime _barricade;
    private ObjectiveRuntime _objective;
    private MeetingPointRuntime _meetingPoint;

    public ObjectiveRuntime Objective => _objective;
    public AbstractUnitsRunTime OccupiedBy => _occupiedBy;
    public BarricadeRuntime Barricade => _barricade;
    public HexCoordinates Coordinates => _coordinates;
    public HexTypeSO Type => _type;
    public MeetingPointRuntime MeetingPoint => _meetingPoint;

    public bool IsObjective => _objective != null;
    public bool IsMeetingPoint => _meetingPoint != null;

    public HexCell(HexCoordinates coordinates, HexTypeSO type)
    {

        _coordinates = coordinates;
        _type = type;
    }

    public void BindMeetingPoint(MeetingPointRuntime meetingPoint) => _meetingPoint = meetingPoint;
    public bool TryOccupy(AbstractUnitsRunTime unit)
    {
        if (unit == null)
            return false;

        if (_barricade != null)
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

    /// <summary>
    /// ⚠ Libera solo se a chiamare è chi la occupa davvero. Senza questo controllo,
    /// un'unità nata su una cella già presa cancella dalla griglia quella che c'era prima
    /// nel momento in cui si sposta.
    /// </summary>
    public void Vacate(AbstractUnitsRunTime unit)
    {
        if (_occupiedBy != unit) return;
        _occupiedBy = null;
    }

    public bool TryPlaceBarricade(BarricadeRuntime barricade)
    {
        if (barricade == null)
            return false;

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

using System.Collections.Generic;

/// <summary>
/// Il ritrovo vivo sulla griglia. ⚠ La sua CAPIENZA non è un parametro da tarare: è
/// quante celle hai dipinto. Il GDD 20.3 chiedeva che il limite del corteo fosse visibile
/// in fase di composizione invece di essere un errore al caricamento — qui il limite è la
/// piazza stessa.
/// </summary>
public class MeetingPointRuntime
{
    private readonly MeetingPointSO _data;
    private readonly List<HexCell> _cells;

    public MeetingPointSO Data => _data;
    public IReadOnlyList<HexCell> Cells => _cells;

    /// <summary>Quante unità ci stanno: una per cella.</summary>
    public int Capacity => _cells.Count;

    public MeetingPointRuntime(MeetingPointSO data, List<HexCell> cells)
    {
        _data = data;
        _cells = cells;
    }

    public override string ToString() => _data != null ? _data.ToString() : "meeting point";
}
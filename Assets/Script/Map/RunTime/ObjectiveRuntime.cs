using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stato vivo di un obiettivo: quante celle-turno sono state accumulate e se è già
/// rivendicato.
/// ⚠ NON mettere questo stato sull'ObjectiveSO: gli SO sono asset condivisi e in Editor
/// il valore sopravvive fra una sessione di Play e l'altra. È il difetto già registrato
/// per SFXSO._lastIndex.
/// </summary>
public class ObjectiveRuntime
{
    private readonly ObjectiveSO _data;
    private readonly List<HexCell> _cells;

    private int _progress;
    private bool _claimed;

    public ObjectiveSO Data => _data;
    public IReadOnlyList<HexCell> Cells => _cells;

    /// <summary>Celle-turno necessarie: una per ogni cella che compone l'obiettivo.</summary>
    public int Required => _cells.Count;
    public int Progress => _progress;
    public bool IsClaimed => _claimed;

    public ObjectiveRuntime(ObjectiveSO data, List<HexCell> cells)
    {
        _data = data;
        _cells = cells;
    }

    /// <summary>Celle dell'obiettivo occupate da uno spezzone vivo in questo momento.</summary>
    public int OccupiedCount()
    {
        int n = 0;
        foreach (HexCell cell in _cells)
            if (cell.OccupiedBy is SpezzoneRuntime spezzone && spezzone.IsAlive) n++;
        return n;
    }

    /// <summary>
    /// Un turno di occupazione. Restituisce true se l'obiettivo è stato rivendicato ADESSO.
    /// ⚠ Se in un turno non c'è nessuno sopra, l'accumulo si AZZERA: l'obiettivo è una
    /// finestra da difendere, non un lavoro da rosicchiare a pezzi.
    /// </summary>
    public bool Tick()
    {
        if (_claimed) return false;

        int occupied = OccupiedCount();

        if (occupied == 0)
        {
            if (_progress > 0)
                Debug.Log($"[OBJ] {this}: occupation interrupted, {_progress}/{Required} lost");
            _progress = 0;
            return false;
        }

        if (_data.RequiresSimultaneous)
        {
            if (occupied < _cells.Count)
            {
                _progress = 0;
                Debug.Log($"[OBJ] {this}: needs all {_cells.Count} cells at once, only {occupied} held");
                return false;
            }
            _progress = Required;
        }
        else
        {
            _progress += occupied;
        }

        if (_progress >= Required)
        {
            _claimed = true;
            Debug.Log($"[OBJ] {this} CLAIMED");
            return true;
        }

        Debug.Log($"[OBJ] {this}: {_progress}/{Required}");
        return false;
    }

    public override string ToString() => _data != null ? _data.ToString() : "objective";
}
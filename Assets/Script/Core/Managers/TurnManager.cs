using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private PathFinder _pathFinder;
    [SerializeField] private PoliceAI _policeAI;

    [Header("Events")]
    [SerializeField] GameEventSO _endTurnEvent;
    // Placeholder per future applicazioni (sfx, vfx, HUD)
    //[SerializeField] GameEventSO _winCombatEvent;
    //[SerializeField] GameEventSO _loseCombatEvent;
    //[SerializeField] GameEventSO _parCombatEvent;

    private HexGrid _map;
    private UnitsRenderer _unitsRenderer;

    // true = turno della polizia, false = turno del corteo (giocatore)
    private bool _waitingForPolice = false;

    public PathFinder PathFinder => _pathFinder;
    public GameEventSO EndTurnEvent => _endTurnEvent;
    public bool IsPoliceTurn => _waitingForPolice;

    private void Start()
    {
        if (_lvlManager == null)
        {
            Debug.LogWarning("Need LVL Manager in TurnManager");
            return;
        }

        _unitsRenderer = _lvlManager.Renderer;
        _map = _lvlManager.Map;

        if (_pathFinder == null)
        {
            Debug.LogWarning("PathFinder non assegnato in TurnManager");
            return;
        }
    }

    #region Direction
    // Trova quale delle 6 direzioni esagonali, applicata "distance" volte, porta da "from" a "to".
    // Restituisce null se le due celle non sono allineate su una direzione pura.
    private HexCoordinates? FindDirection(HexCoordinates from, HexCoordinates to)
    {
        int distance = from.Distance(to);

        foreach (var dir in HexCoordinates.Directions)
        {
            HexCoordinates candidate = new HexCoordinates(
                from.Q + dir.Q * distance,
                from.R + dir.R * distance
            );

            if (candidate.Equals(to))
                return dir;
        }

        return null;
    }
    #endregion

    #region Push
    // Verifica se esistono esattamente 2 celle libere consecutive tra attaccante e difensore
    // (distanza 3, in linea retta). Se sì, restituisce la cella dove l'attaccante deve fermarsi
    // (adiacente al difensore) tramite chargeDestination.
    private bool HasChargeRoom(HexCoordinates atkCoord, HexCoordinates defCoord, out HexCoordinates chargeDestination)
    {
        chargeDestination = default;

        int distance = atkCoord.Distance(defCoord);
        if (distance != 3) return false;

        HexCoordinates? dir = FindDirection(atkCoord, defCoord);
        if (dir == null) return false;

        HexCoordinates dirValue = dir.Value;

        HexCoordinates firstStep = new HexCoordinates(atkCoord.Q + dirValue.Q, atkCoord.R + dirValue.R);
        HexCoordinates secondStep = new HexCoordinates(atkCoord.Q + dirValue.Q * 2, atkCoord.R + dirValue.R * 2);

        if (!_map.TryGetCell(firstStep, out HexCell firstCell) || !IsCellAvailable(firstCell)) return false;
        if (!_map.TryGetCell(secondStep, out HexCell secondCell) || !IsCellAvailable(secondCell)) return false;

        chargeDestination = secondStep;
        return true;
    }

    // Carica: richiede esattamente 2 celle libere di rincorsa, costa 4 PA (2 fissi + 2 per le celle percorse).
    // L'attaccante si sposta adiacente al difensore, poi si risolve la spinta come al solito.
    public bool ExecuteCharge(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        HexCoordinates atkCoord = atk.PositionCell.Coordinates;
        HexCoordinates defCoord = def.PositionCell.Coordinates;

        if (!HasChargeRoom(atkCoord, defCoord, out HexCoordinates chargeDestination))
        {
            Debug.Log("Carica non valida: distanza o spazio di rincorsa insufficienti");
            return false;
        }

        const int chargeCost = 4; // 2 fissi + 2 celle percorse in rincorsa
        if (!atk.TrySpendActionPoint(chargeCost))
        {
            Debug.Log($"Carica non eseguita: punti azione insufficienti (servono {chargeCost})");
            return false;
        }

        _map.TryGetCell(chargeDestination, out HexCell destinationCell);
        atk.SetPosition(destinationCell);
        _unitsRenderer.UpdateView(atk);

        PushResolution(atk, def);

        return true;
    }

    // Calcola dove finisce un'unità spinta: stessa direzione attaccante->difensore, applicata oltre il difensore.
    private HexCell PushHandle(HexCoordinates atkCoord, HexCoordinates defCoord, CombatResult result)
    {
        int resultQ = (defCoord.Q - atkCoord.Q);
        int resultR = (defCoord.R - atkCoord.R);
        HexCoordinates pushCoord = new HexCoordinates(defCoord.Q + resultQ, defCoord.R + resultR);

        HexCell returnCell;

        if (_map.TryGetCell(pushCoord, out returnCell))
        {
            if (IsCellAvailable(returnCell))
            {
                return returnCell;
            }
            else
            {
                return FoundNearCellAvailable(defCoord, pushCoord);
            }
        }
        return null;
    }

    // Risolve un impatto diretto (Carica): calcola Win/Lose/Par e applica spinta o dispersione.
    private void PushResolution(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        CombatResult result = CombatResolver.Resolve(atk, def);
        switch (result)
        {
            case CombatResult.Win:
                {
                    HexCell target = PushHandle(atk.PositionCell.Coordinates, def.PositionCell.Coordinates, result);
                    if (target != null)
                    {
                        def.SetPosition(target);
                    }
                    else
                    {
                        Debug.Log($"Spezzone arrestato su {def.PositionCell.Coordinates}");
                        def.Disperse();
                    }
                    break;
                }

            case CombatResult.Lose:
                {
                    HexCell target = PushHandle(def.PositionCell.Coordinates, atk.PositionCell.Coordinates, result);
                    if (target != null)
                    {
                        atk.SetPosition(target);
                    }
                    else
                    {
                        Debug.Log("Police Disperse");
                        atk.Disperse();
                    }
                    break;
                }

            case CombatResult.Par:
                break;
        }
        _unitsRenderer.UpdateView(atk);
        _unitsRenderer.UpdateView(def);
    }
    #endregion

    #region Moviment
    // Se la cella diretta di spinta non è libera, cerca una cella laterale comune
    // (intersezione tra i vicini di partenza e arrivo), in ordine casuale.
    private HexCell FoundNearCellAvailable(HexCoordinates startPushCell, HexCoordinates endPushCell)
    {
        HexCoordinates[] startCellNeighbors = startPushCell.GetNeighbors();
        HexCoordinates[] endCellNeighbors = endPushCell.GetNeighbors();

        List<HexCoordinates> common = new List<HexCoordinates>();
        foreach (var n in startCellNeighbors)
            foreach (var m in endCellNeighbors)
                if (n == m) common.Add(n);

        if (common.Count > 1 && Random.value > 0.5f)
        {
            var tmp = common[0];
            common[0] = common[1];
            common[1] = tmp;
        }

        foreach (var coord in common)
        {
            if (_map.TryGetCell(coord, out HexCell cell) && IsCellAvailable(cell))
                return cell;
        }
        return null;
    }

    // Una cella è disponibile se esiste, non è occupata da nessuno, ed è percorribile.
    private bool IsCellAvailable(HexCell cell)
    {
        if (cell == null) return false;
        if (cell.OccupiedBy != null) return false;

        return cell.Type.IsWalkable;
    }

    // Movimento puro: sposta l'unità di una cella, costa 1 PA per cella percorsa.
    // "path" è la sequenza di celle intermedie + destinazione (calcolata altrove, es. da pathfinding/preview).
    public bool ExecuteMovement(AbstractUnitsRunTime unit, List<HexCell> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.Log("Movimento non valido: percorso vuoto");
            return false;
        }

        int cost = path.Count;
        if (!unit.TrySpendActionPoint(cost))
        {
            Debug.Log($"Movimento non eseguito: punti azione insufficienti (servono {cost})");
            return false;
        }

        HexCell destination = path[path.Count - 1];
        unit.SetPosition(destination);
        _unitsRenderer.UpdateView(unit);

        return true;
    }
    #endregion

    #region Scontri
    // Scontro: richiede adiacenza, costa 1 PA fisso. Non sposta nessuno, intacca solo il Morale.
    // Parità: entrambi perdono 1 Morale (diverso dalla Carica, dove la parità è uno stallo).
    public bool ExecuteSkirmish(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        HexCoordinates atkCoord = atk.PositionCell.Coordinates;
        HexCoordinates defCoord = def.PositionCell.Coordinates;

        if (atkCoord.Distance(defCoord) != 1)
        {
            Debug.Log("Scontro non valido: le unità non sono adiacenti");
            return false;
        }

        const int skirmishCost = 1;
        if (!atk.TrySpendActionPoint(skirmishCost))
        {
            Debug.Log($"Scontro non eseguito: punti azione insufficienti (servono {skirmishCost})");
            return false;
        }

        CombatResult result = CombatResolver.Resolve(atk, def);
        switch (result)
        {
            case CombatResult.Win:
                def.LoseMorale(1);
                break;
            case CombatResult.Lose:
                atk.LoseMorale(1);
                break;
            case CombatResult.Par:
                atk.LoseMorale(1);
                def.LoseMorale(1);
                break;
        }

        return true;
    }
    #endregion

    // Passa la mano: dal turno corteo a quello polizia, o viceversa con fine turno completo.
    public void EndTurn()
    {
        if (!_waitingForPolice)
        {
            _waitingForPolice = true;
            Debug.Log("--- TURNO POLIZIA ---");

            foreach (var police in _lvlManager.Police)
            {
                if (police.Status == UnitsStatus.Disperse) continue;
                police.RefillActionPoints();
            }

            _policeAI.ExecutePoliceActions();
        }
        else
        {
            _waitingForPolice = false;

            foreach (var spezzone in _lvlManager.Spezzoni)
            {
                if (spezzone.Status == UnitsStatus.Disperse) continue;

                spezzone.RefillActionPoints();
            }
        }

        _endTurnEvent.Raise();
    }
}
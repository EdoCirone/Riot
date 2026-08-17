using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private PathFinder _pathFinder;
    [SerializeField] private PoliceAI _policeAI;

    [Space]
    [Header("TurnEvents")]
    [SerializeField] private GameEventSO _startPlayerTurnEvent;
    [SerializeField] private GameEventSO _endPlayerTurnEvent;

    [Space]
    [Header("FightEvents")]
    [SerializeField] private UnitEventSO _throwEvent;
    [SerializeField] private GameEventSO _skirmishWinEvent;
    [SerializeField] private GameEventSO _skirmishLoseEvent;
    [SerializeField] private GameEventSO _skirmishParEvent;
    [Header("ChargeEvents")]
    [SerializeField] private GameEventSO _chargeEvent;

    [Space]
    [Header("CameraEvents")]
    [SerializeField] private GameObjectEventSO _startFollowEvent;
    [SerializeField] private GameEventSO _stopFollowEvent;

    [Space]
    [Header("AlertEvents")]
    [SerializeField] private StringEventSO _alertEvent;


    private HexGrid _map;
    private UnitsRenderer _unitsRenderer;

    private bool _waitingForPolice = false;

    public PathFinder PathFinder => _pathFinder;
    public GameEventSO EndPlayerTurnEvent => _endPlayerTurnEvent;
    public GameEventSO StartPlayerTurnEvent => _startPlayerTurnEvent;

    public UnitEventSO ThrowEvent => _throwEvent;
    private bool IsCellAvailable(HexCell cell) => TacticalQuery.IsCellAvailable(cell);
    public bool IsPoliceTurn => _waitingForPolice;

    //Helper method to determine the cause of morale loss based on the source unit type
    private static MoraleLossCause CauseFrom(AbstractUnitsRunTime source)
    => source is PoliceRuntime ? MoraleLossCause.PoliceContact : MoraleLossCause.Other;

    //Due unità sono della stessa parte se sono entrambe polizia o entrambe corteo
    private static bool IsSameSide(AbstractUnitsRunTime a, AbstractUnitsRunTime b)
    => (a is PoliceRuntime) == (b is PoliceRuntime);

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
            Debug.LogWarning("PathFinder not assigned in TurnManager");
            return;
        }

        _startPlayerTurnEvent.Raise();
    }

    #region Charge

    private bool HasChargeRoom(HexCoordinates atkCoord, HexCoordinates defCoord, out HexCoordinates chargeDestination)
     => TacticalQuery.HasChargeRoom(atkCoord, defCoord, _map, out chargeDestination);

    public bool CanCharge(AbstractUnitsRunTime atk, AbstractUnitsRunTime def, out HexCell destinationCell)
    {
        destinationCell = null;

        if (atk == null || def == null) return false;
        if (!atk.IsAlive || !def.IsAlive) return false;
        if (def.IsSeated) return false;                       // barricata umana
        if (atk.ActionPoints < TacticalQuery.ChargeCost) return false;

        if (!HasChargeRoom(atk.PositionCell.Coordinates, def.PositionCell.Coordinates,
                           out HexCoordinates chargeDestination)) return false;

        return _map.TryGetCell(chargeDestination, out destinationCell);
    }

    public void StartCharge(AbstractUnitsRunTime atk, AbstractUnitsRunTime def, Action onComplete)
    {
        StartCoroutine(ChargeWithCallback(atk, def, onComplete));
    }

    private IEnumerator ChargeWithCallback(AbstractUnitsRunTime atk, AbstractUnitsRunTime def, Action onComplete)
    {
        yield return StartCoroutine(ExecuteCharge(atk, def));
        onComplete?.Invoke();
    }

    public IEnumerator ExecuteCharge(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        if (!CanCharge(atk, def, out HexCell destinationCell))
        {
            Debug.Log("Invalid charge: target, alignment, run-up space or AP");
            yield break;
        }

        GameObject atkGO = _unitsRenderer.GetGameObject(atk);
        if (atkGO == null)
        {
            Debug.LogError($"GameObject not found for {atk}");
            yield break;
        }

        UnitMovement movement = atkGO.GetComponent<UnitMovement>();
        if (movement == null)
        {
            Debug.LogError($"UnitMovement not found on {atkGO.name}");
            yield break;
        }

        Vector3 defenderWorldPos = _map.GridToWorld(def.PositionCell.Coordinates);

        atk.TrySpendActionPoint(TacticalQuery.ChargeCost);

        bool done = false;
        bool resolved = false;

        // Funzione locale: legale dentro una coroutine perché non contiene yield.
        // Serve a garantire che PushResolution giri UNA volta sola, da qualunque
        // strada ci si arrivi (callback dell'animazione o timeout).

        void ResolveOnce()
        {
            if (resolved) return;
            resolved = true;
            PushResolution(atk, def);
        }

        atk.SetPosition(destinationCell);

        movement.PlayCharge(destinationCell, defenderWorldPos, _map, () =>
        {
            ResolveOnce();
            done = true;
        });

        float elapsed = 0f;
        yield return new WaitUntil(() => done || (elapsed += Time.deltaTime) > 5f);
        if (!done)
        {
            Debug.LogWarning($"[CHARGE] animation not completed by {atk}: continuing anyway");
            ResolveOnce();
        }
    }

    private bool TryBuildPushChain(
     AbstractUnitsRunTime pusher,
     AbstractUnitsRunTime pushed,
     out List<(AbstractUnitsRunTime unit, HexCell destination)> moves)
    {
        moves = new List<(AbstractUnitsRunTime, HexCell)>();

        HexCoordinates pusherCoord = pusher.PositionCell.Coordinates;
        HexCoordinates current = pushed.PositionCell.Coordinates;

        int dirQ = current.Q - pusherCoord.Q;
        int dirR = current.R - pusherCoord.R;

        // La colonna compressa, dal difensore all'ultimo della fila.
        List<AbstractUnitsRunTime> column = new() { pushed };
        AbstractUnitsRunTime unitToMove = pushed;

        while (true)
        {
            HexCoordinates behind = new HexCoordinates(current.Q + dirQ, current.R + dirR);

            if (_map.TryGetCell(behind, out HexCell behindCell)
                && behindCell.Type.IsWalkable
                && !behindCell.IsObjective
                && behindCell.Barricade == null)
            {
                AbstractUnitsRunTime blocker = behindCell.OccupiedBy;

                if (blocker == null)
                {
                    BuildMovesFromColumn(column, behindCell, moves);   // catena chiusa
                    return true;
                }

                if (!blocker.IsSeated && IsSameSide(blocker, unitToMove))
                {
                    column.Add(blocker);
                    unitToMove = blocker;
                    current = behind;
                    continue;                                          // il domino prosegue
                }
            }

            // Tappo: si cerca uno sfogo laterale partendo da chi è più indietro.
            return TryReleaseSideways(column, dirQ, dirR, moves);
        }
    }

    /// <summary>
    /// Catena chiusa: ognuno entra nella cella di chi lo segue, l'ultimo nella cella libera.
    /// </summary>
    private void BuildMovesFromColumn(
        List<AbstractUnitsRunTime> column, HexCell tail,
        List<(AbstractUnitsRunTime unit, HexCell destination)> moves)
    {
        for (int i = 0; i < column.Count; i++)
            moves.Add((column[i], i + 1 < column.Count ? column[i + 1].PositionCell : tail));
    }

    /// <summary>
    /// Cerca la prima unità — partendo dal FONDO della colonna — che possa scartare di lato.
    /// Chi scarta libera la propria cella e tutta la fila davanti a lui arretra di uno;
    /// chi sta dietro di lui resta fermo.
    /// ⚠ L'ordine di `moves` conta: ApplyPushChain applica dall'ultimo al primo, quindi
    /// chi scarta deve essere l'ULTIMO elemento della lista.
    /// </summary>
    private bool TryReleaseSideways(
        List<AbstractUnitsRunTime> column, int dirQ, int dirR,
        List<(AbstractUnitsRunTime unit, HexCell destination)> moves)
    {
        for (int i = column.Count - 1; i >= 0; i--)
        {
            HexCell side = FindSideCell(column[i], dirQ, dirR);
            if (side == null) continue;

            for (int j = 0; j < i; j++)
                moves.Add((column[j], column[j + 1].PositionCell));

            moves.Add((column[i], side));

            Debug.Log($"[PUSH] {column[i]} steps aside to {side.Coordinates}, {i} unit(s) shift back");
            return true;
        }

        return false;
    }



    /// <summary>
    /// Le due celle che confinano sia con quella dell'unità sia con quella dove sarebbe
    /// stata spinta: su esagoni sono sempre e solo due, e sono le direzioni che affiancano
    /// quella della spinta. Fra le due, quella con meno alleati adiacenti: la spinta disgrega.
    /// </summary>
    private HexCell FindSideCell(AbstractUnitsRunTime unit, int dirQ, int dirR)
    {
        int dirIndex = -1;
        for (int i = 0; i < HexCoordinates.Directions.Length; i++)
        {
            if (HexCoordinates.Directions[i].Q == dirQ && HexCoordinates.Directions[i].R == dirR)
            {
                dirIndex = i;
                break;
            }
        }

        if (dirIndex < 0)
        {
            Debug.LogError($"[PUSH] ({dirQ},{dirR}) is not a hex direction: cannot find side cells");
            return null;
        }

        HexCoordinates from = unit.PositionCell.Coordinates;
        HexCell best = null;
        int bestAllies = int.MaxValue;

        for (int offset = -1; offset <= 1; offset += 2)
        {
            HexCoordinates side = HexCoordinates.Directions[(dirIndex + offset + 6) % 6];
            HexCoordinates candidateCoord = new HexCoordinates(from.Q + side.Q, from.R + side.R);

            if (!_map.TryGetCell(candidateCoord, out HexCell candidate)) continue;
            if (!IsCellAvailable(candidate)) continue;
            if (candidate.IsObjective) continue;
            int allies = CountAdjacentAllies(unit, candidateCoord);
            if (allies < bestAllies)
            {
                bestAllies = allies;
                best = candidate;
            }
        }

        return best;
    }

    private int CountAdjacentAllies(AbstractUnitsRunTime unit, HexCoordinates from)
    {
        int count = 0;
        foreach (HexCoordinates dir in HexCoordinates.Directions)
        {
            if (!_map.TryGetCell(from + dir, out HexCell cell)) continue;

            AbstractUnitsRunTime other = cell.OccupiedBy;
            if (other == null || !other.IsAlive) continue;
            if (other == unit) continue;              // sé stesso non conta: vedi sotto
            if (IsSameSide(other, unit)) count++;
        }
        return count;
    }

    private void ApplyPushChain(List<(AbstractUnitsRunTime unit, HexCell destination)> moves)
    {
        for (int i = moves.Count - 1; i >= 0; i--)
        {
            (AbstractUnitsRunTime unit, HexCell destination) = moves[i];

            if (!unit.SetPosition(destination))
            {
                Debug.LogError($"[PUSH] {unit} could not occupy {destination.Coordinates}: inconsistent chain");
                return;
            }
            _unitsRenderer.UpdateView(unit);
        }
        Debug.Log($"[PUSH] applied: {moves.Count} unit(s) moved");
    }

    private void ResolvePushOrRemove(AbstractUnitsRunTime pusher, AbstractUnitsRunTime pushed)
    {
        if (pusher.PositionCell.Coordinates.Distance(pushed.PositionCell.Coordinates) != 1)
        {
            Debug.LogError($"[PUSH] {pusher} and {pushed} are not adjacent: push not resolved");
            return;
        }

        if (TryBuildPushChain(pusher, pushed, out var moves))
        {
            ApplyPushChain(moves);
            return;
        }

        Debug.Log($"[PUSH] no way back and no way out: {pushed} removed at {pushed.PositionCell.Coordinates}");
        pushed.RemoveFromBoard(CauseFrom(pusher));
    }

    private void PushResolution(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        // La cella dell'urto, catturata PRIMA della spinta: è l'unica che esiste di sicuro,
        // e non dipende dal fatto che RemoveFromBoard lasci _positionCell popolato.
        HexCell impactCell = def.PositionCell;

        ResolvePushOrRemove(pusher: atk, pushed: def);

        // Se il difensore è sopravvissuto si è spostato: l'onda parte da dove è adesso.
        // Se la spinta l'ha rimosso, resta valida la cella dell'urto.
        if (def.IsAlive)
        {
            impactCell = def.PositionCell;
            def.LoseMorale(1, CauseFrom(atk));
            _unitsRenderer.FlashDamage(def);
        }
        ApplyPanicWave(impactCell, def);

        _chargeEvent?.Raise();

        _unitsRenderer.UpdateView(atk);
        _unitsRenderer.UpdateView(def);
        _lvlManager.RefreshBoardState();
    }

    /// <summary>
    /// Applica l'onda. NON chiama RefreshBoardState: lo fa il chiamante, che di solito
    /// sta risolvendo qualcosa di più grande (la carica) e deve ricalcolare una volta sola.
    /// </summary>
    private void ApplyPanicWave(HexCell origin, AbstractUnitsRunTime epicentre)
    {
        if (origin == null)
        {
            Debug.LogWarning("[PANIC] no origin cell: wave skipped");
            return;
        }

        var wave = TacticalQuery.GetPanicWave(origin, epicentre, _map);

        int baseTurns = epicentre is PoliceRuntime
            ? TacticalQuery.PanicTurnsPolice
            : TacticalQuery.PanicTurnsCorteo;

        foreach (var (unit, steps) in wave)
        {
            unit.ApplyPanic(Mathf.Max(1, baseTurns - steps));
            _unitsRenderer.UpdateView(unit);
        }

        Debug.Log($"[PANIC] wave from {origin.Coordinates}: {wave.Count} unit(s) affected");
    }
    #endregion

    #region Moviment

    public bool ExecuteMovement(AbstractUnitsRunTime unit, List<HexCell> path, System.Action onComplete = null)
    {
        if (path == null || path.Count == 0)
        {
            _alertEvent?.Raise("No Path Found");
            onComplete?.Invoke();
            return false;
        }

        // Check PA
        int cost = path.Count;
        if (unit.ActionPoints < cost)
        {
            Debug.Log($"Insufficent PA to move ({cost} PA needed)");
            onComplete?.Invoke();
            return false;
        }

        GameObject unitGO = _unitsRenderer.GetGameObject(unit);
        if (unitGO == null)
        {
            Debug.LogError($"GameObject don't found for {unit}");
            onComplete?.Invoke();
            return false;
        }

        UnitMovement movement = unitGO.GetComponent<UnitMovement>();
        if (movement == null)
        {
            Debug.LogError($"UnitMovement not found on {unitGO.name}");
            onComplete?.Invoke();
            return false;
        }

        if (movement.IsMoving)
        {
            onComplete?.Invoke();
            return false;
        }

        unit.TrySpendActionPoint(cost);

        if (path[0] == unit.PositionCell)
            path.RemoveAt(0);

        if (path.Count == 0)
        {
            onComplete?.Invoke();
            return true;
        }

        _startFollowEvent?.Raise(unitGO);
        movement.MoveAlongPath(path, _lvlManager.Map, () =>
        {
            _unitsRenderer.UpdateView(unit);
            _lvlManager.RefreshBoardState();
            _lvlManager.CheckObjectiveIntrusion(unit);
            _stopFollowEvent?.Raise();
            onComplete?.Invoke();
        });

        return true;
    }

    //Metodo che mi serve per evitare la sovraposizione 
    public HexCoordinates? FindBestAdjacentCell(HexCoordinates from, HexCoordinates targetCoord)
    {
        HexCoordinates[] neighbors = targetCoord.GetNeighbors();
        HexCoordinates? best = null;
        int minDistance = int.MaxValue;

        foreach (var neighbor in neighbors)
        {
            if (!_lvlManager.Map.TryGetCell(neighbor, out HexCell cell)) continue;
            if (!IsCellAvailable(cell)) continue;

            int distance = from.Distance(neighbor);
            if (distance < minDistance)
            {
                minDistance = distance;
                best = neighbor;
            }
        }

        return best;
    }
    #endregion

    #region Scontri

    public void StartSkirmish(AbstractUnitsRunTime atk, AbstractUnitsRunTime def, Action onComplete)
    {
        StartCoroutine(SkirmishWithCallback(atk, def, onComplete));
    }

    private IEnumerator SkirmishWithCallback(AbstractUnitsRunTime atk, AbstractUnitsRunTime def, Action onComplete)
    {
        yield return StartCoroutine(ExecuteSkirmish(atk, def));
        onComplete?.Invoke();
    }

    private void RaiseCombactResult(CombatResult result)
    {
        switch (result)
        {
            case CombatResult.Win: _skirmishWinEvent?.Raise(); break;
            case CombatResult.Lose: _skirmishLoseEvent?.Raise(); break;
            case CombatResult.Par: _skirmishParEvent?.Raise(); break;
        }
    }

    public IEnumerator ExecuteSkirmish(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        HexCoordinates atkCoord = atk.PositionCell.Coordinates;
        HexCoordinates defCoord = def.PositionCell.Coordinates;

        if (atkCoord.Distance(defCoord) != 1)
        {
            Debug.Log("Not Valid Skirmish units are not adicent");
            yield break;
        }

        const int skirmishCost = 1;
        if (!atk.TrySpendActionPoint(skirmishCost))
        {
            Debug.Log($"skirmish not able to be execute, {skirmishCost}AP needed)");
            yield break;
        }

        CombatResult result = CombatResolver.Resolve(atk, def, _map);
        List<AbstractUnitsRunTime> hit = new();

        switch (result)
        {
            case CombatResult.Win:
                def.LoseMorale(1, CauseFrom(atk)); hit.Add(def); break;
            case CombatResult.Lose:
                atk.LoseMorale(1, CauseFrom(def)); hit.Add(atk); break;
            case CombatResult.Par:
                atk.LoseMorale(1, CauseFrom(def)); hit.Add(atk);
                def.LoseMorale(1, CauseFrom(atk)); hit.Add(def); break;
        }

        bool done = false;
        bool finalized = false;

        void FinalizeOnce()
        {
            if (finalized) return;
            finalized = true;

            _unitsRenderer.UpdateView(atk);
            _unitsRenderer.UpdateView(def);
            _lvlManager.RefreshBoardState();

            // Chi viene attaccato chiama i colleghi: vale solo per la polizia.
            if (def is PoliceRuntime) _lvlManager.RaiseAlarmAround(def.PositionCell, $"{def} attacked");
        }

        GameObject atkGO = _unitsRenderer.GetGameObject(atk);
        UnitMovement movement = atkGO.GetComponent<UnitMovement>();
        Vector3 defWorldPos = _map.GridToWorld(def.PositionCell.Coordinates);

        GameObject defGO = _unitsRenderer.GetGameObject(def);
        UnitMovement defMovement = defGO != null ? defGO.GetComponent<UnitMovement>() : null;
        Vector3 atkWorldPos = _map.GridToWorld(atk.PositionCell.Coordinates);

        movement.PlaySkirmish(defWorldPos,
             onComplete: () =>
             {
                 FinalizeOnce();
                 done = true;
             },
             onImpact: () =>
             {
                 defMovement?.PlayHitReaction(atkWorldPos);
                 RaiseCombactResult(result);
             });

        // ⚠ Il fail-safe non deve solo sbloccare: deve lasciare la plancia in uno stato
        // valido. Il Morale è già stato applicato prima dell'animazione, quindi senza
        // FinalizeOnce resterebbero morti non nascosti e aure non ricalcolate.
        float elapsed = 0f;
        yield return new WaitUntil(() => done || (elapsed += Time.deltaTime) > 5f);

        if (!done)
        {
            Debug.LogWarning($"[Skirmish] animation not complete from {atk}: finalizing anyway");
            FinalizeOnce();
        }
    }
    #endregion

    #region Lancio
    public void ExecuteThrow(AbstractUnitsRunTime atk, PoliceRuntime target, ThrowItemSO item)
    {
        if (atk is not SpezzoneRuntime spezzone) return;
        if (target == null || item == null) return;

        // UNICA decisione. Le righe sotto spiegano soltanto, non decidono.
        if (!TacticalQuery.CanThrow(spezzone, target.PositionCell, item, _map))
        {
            if (!spezzone.Inventory.HasItem(item))
                _alertEvent?.Raise("No throw objects");
            else if (spezzone.ActionPoints < item.ActionPointCost)
                _alertEvent?.Raise($"Not enough AP, {item.ActionPointCost} needed");
            else
                _alertEvent?.Raise("Invalid throw target");
            return;
        }

        spezzone.TrySpendActionPoint(item.ActionPointCost);
        spezzone.Inventory.ConsumeItem(item);
        _throwEvent.Raise(target);
        target.LoseMorale(item.MoralLost);
        _unitsRenderer.UpdateView(target);
        _lvlManager.RefreshBoardState();
    }

    #endregion

    #region Barricade


    public bool ExecuteBarricade(AbstractUnitsRunTime atk, HexCell targetCell, BarricadeSO item)
    {
        if (atk is not SpezzoneRuntime spezzone) return false;
        if (targetCell == null || item == null) return false;

        if (!TacticalQuery.CanPlaceBarricade(spezzone, targetCell, item))
        {
            if (!spezzone.Inventory.HasItem(item))
                _alertEvent?.Raise("No barricade objects");
            else if (spezzone.ActionPoints < item.ActionPointCost)
                _alertEvent?.Raise($"Not enough AP, {item.ActionPointCost} needed");
            else if (targetCell.IsObjective)
                _alertEvent?.Raise("Cannot barricade an objective");
            else
                _alertEvent?.Raise("Not available cell for barricade");
            return false;
        }

        spezzone.TrySpendActionPoint(item.ActionPointCost);
        spezzone.Inventory.ConsumeItem(item);
        targetCell.TryPlaceBarricade(new BarricadeRuntime(item));

        Vector3 worldPos = _map.GridToWorld(targetCell.Coordinates);
        Instantiate(item.GraphicPrefab, worldPos, Quaternion.identity);

        _lvlManager.RefreshBoardState();
        return true;
    }

    #endregion

    #region Chant & SitDown

    public bool ExecuteChant(AbstractUnitsRunTime caster)
    {
        if (!caster.TrySpendActionPoint(TacticalQuery.ChantCost))
        {
            Debug.Log($"Chant not executed: {TacticalQuery.ChantCost} AP needed");
            _alertEvent?.Raise($"Not enough AP, {TacticalQuery.ChantCost} needed");
            return false;
        }

        caster.GainMorale(1);
        caster.ClearPanic();
        _unitsRenderer.UpdateView(caster);

        foreach (HexCoordinates n in caster.PositionCell.Coordinates.GetNeighbors())
        {
            if (!_map.TryGetCell(n, out HexCell cell)) continue;
            if (cell.OccupiedBy is SpezzoneRuntime spezzone && spezzone.IsAlive)
            {
                spezzone.GainMorale(1);
                spezzone.ClearPanic();
                _unitsRenderer.UpdateView(spezzone);
            }
        }

        _lvlManager.RefreshBoardState();
        return true;
    }

    public bool ExecuteSitStand(AbstractUnitsRunTime unit)
    {
        if (unit == null || !unit.IsAlive) return false;

        // Attenzione all'ordine: il costo e il verbo dipendono dallo stato PRIMA
        // del cambiamento. Leggerli dopo SitDown/StandUp darebbe il valore sbagliato.
        int cost = TacticalQuery.GetSitStandCost(unit);
        bool wasSeated = unit.IsSeated;

        if (!unit.TrySpendActionPoint(cost))
        {
            Debug.Log($"{(wasSeated ? "Stand up" : "Sit down")} not executed: {cost} AP needed");
            _alertEvent?.Raise($"Not enough AP, {cost} needed");
            return false;
        }

        if (wasSeated) unit.StandUp();
        else unit.SitDown();

        _unitsRenderer.UpdateView(unit);
        _lvlManager.RefreshBoardState();

        Debug.Log($"{unit} {(wasSeated ? "stands up" : "sits down")}. Def now {unit.Def}, AP left {unit.ActionPoints}");
        return true;
    }

    #endregion

    public void EndTurn()
    {
        if (_waitingForPolice) return;

        _lvlManager.RefreshBoardState();
        if (_lvlManager.CheckCohesionDefeat()) return;

        _waitingForPolice = true;
        _endPlayerTurnEvent.Raise();

        if (!_lvlManager.IsGameActive)
        {
            _waitingForPolice = false;
            return;
        }

        Debug.Log("--- TURNO POLIZIA ---");

        foreach (var police in _lvlManager.Police)
        {
            if (!police.IsAlive) continue;
            police.RefillActionPoints();
        }

        // Il panico degli SPEZZONI scala qui: il giocatore sta chiudendo il proprio turno.
        foreach (var spezzone in _lvlManager.Spezzoni)
        {
            if (!spezzone.IsAlive) continue;
            spezzone.TickPanic();
            _unitsRenderer.UpdateView(spezzone);
        }

        _lvlManager.RefreshBoardState();

        StartCoroutine(ExecutePoliceTurn());
    }

    private IEnumerator ExecutePoliceTurn()
    {
        yield return StartCoroutine(_policeAI.ExecutePoliceActions());

        Debug.Log("--- FINE TURNO POLIZIA ---");
        _waitingForPolice = false;

        if (!_lvlManager.IsGameActive)
        {
            yield break;
        }

        foreach (var spezzone in _lvlManager.Spezzoni)
        {
            if (!spezzone.IsAlive) continue;
            spezzone.RefillActionPoints();
        }

        // Il panico della POLIZIA scala qui: hanno appena finito il loro turno.
        foreach (var police in _lvlManager.Police)
        {
            if (!police.IsAlive) continue;
            police.TickPanic();
            _unitsRenderer.UpdateView(police);
        }

        foreach (var police in _lvlManager.Police)
            if (police.IsAlive) police.TickAlarm();

        _lvlManager.RefreshBoardState();
        _startPlayerTurnEvent.Raise();
    }
}
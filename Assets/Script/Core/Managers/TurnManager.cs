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
    [SerializeField] private GameEventSO _chargeWinEvent;
    [SerializeField] private GameEventSO _chargeLoseEvent;
    [SerializeField] private GameEventSO _chargeParEvent;

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
            Debug.LogWarning("PathFinder non assegnato in TurnManager");
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
            Debug.Log("Carica non valida: bersaglio, allineamento, spazio di rincorsa o PA insufficienti");
            yield break;
        }

        GameObject atkGO = _unitsRenderer.GetGameObject(atk);
        if (atkGO == null)
        {
            Debug.LogError($"GameObject non trovato per {atk}");
            yield break;
        }

        UnitMovement movement = atkGO.GetComponent<UnitMovement>();
        if (movement == null)
        {
            Debug.LogError($"UnitMovement non trovato su {atkGO.name}");
            yield break;
        }

        Vector3 defenderWorldPos = _map.transform.position + def.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);

        atk.TrySpendActionPoint(TacticalQuery.ChargeCost);

        bool done = false;
        atk.SetPosition(destinationCell);
        movement.PlayCharge(destinationCell, defenderWorldPos, _map, () =>
        {
            PushResolution(atk, def);
            done = true;
        });

        float elapsed = 0f;
        yield return new WaitUntil(() => done || (elapsed += Time.deltaTime) > 5f);
        if (!done) Debug.LogWarning($"[CARICA] animazione non completata da {atk}: proseguo comunque");
    }


    private bool TryBuildPushChain(
        AbstractUnitsRunTime pusher,
        AbstractUnitsRunTime pushed,
        out List<(AbstractUnitsRunTime unit, HexCell destination)> moves)
    {
        moves = new List<(AbstractUnitsRunTime, HexCell)>();

        HexCoordinates pusherCoord = pusher.PositionCell.Coordinates;
        HexCoordinates current = pushed.PositionCell.Coordinates;

        // pusher e pushed sono adiacenti: il delta è già la direzione unitaria
        int dirQ = current.Q - pusherCoord.Q;
        int dirR = current.R - pusherCoord.R;

        AbstractUnitsRunTime unitToMove = pushed;

        while (true)
        {
            HexCoordinates behind = new HexCoordinates(current.Q + dirQ, current.R + dirR);

            if (!_map.TryGetCell(behind, out HexCell behindCell)) return false;  // bordo mappa
            if (!behindCell.Type.IsWalkable) return false;                       // muro
            if (behindCell.Type.IsObjective) return false;                       // un obiettivo non si prende per spinta
            if (behindCell.Barricade != null) return false;                      // barricata

            moves.Add((unitToMove, behindCell));

            AbstractUnitsRunTime blocker = behindCell.OccupiedBy;
            if (blocker == null) return true;                                    // catena chiusa

            if (blocker.IsSeated) return false;                                  // il seduto non si sposta
            if (!IsSameSide(blocker, unitToMove)) return false;                  // il nemico fa muro

            unitToMove = blocker;                                                // il domino prosegue
            current = behind;
        }
    }


    private void ApplyPushChain(List<(AbstractUnitsRunTime unit, HexCell destination)> moves)
    {
        for (int i = moves.Count - 1; i >= 0; i--)
        {
            (AbstractUnitsRunTime unit, HexCell destination) = moves[i];

            if (!unit.SetPosition(destination))
            {
                Debug.LogError($"[SPINTA] {unit} non ha potuto occupare {destination.Coordinates}: catena incoerente");
                return;
            }

            _unitsRenderer.UpdateView(unit);
        }
    }

    private void ResolvePushOrRemove(AbstractUnitsRunTime pusher, AbstractUnitsRunTime pushed)
    {
        if (TryBuildPushChain(pusher, pushed, out var moves))
        {
            ApplyPushChain(moves);
        }
        else
        {
            Debug.Log($"[SPINTA] Catena bloccata: {pushed} fuori gioco su {pushed.PositionCell.Coordinates}");
            pushed.RemoveFromBoard(CauseFrom(pusher));
        }
    }

    private void RaiseChargeResult(CombatResult result)
    {
        switch (result)
        {
            case CombatResult.Win: _chargeWinEvent?.Raise(); break;
            case CombatResult.Lose: _chargeLoseEvent?.Raise(); break;
            case CombatResult.Par: _chargeParEvent?.Raise(); break;
        }
    }

    private void PushResolution(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        CombatResult result = CombatResolver.Resolve(atk, def, _map);

        switch (result)
        {
            case CombatResult.Win:
                ResolvePushOrRemove(pusher: atk, pushed: def);
                break;

            case CombatResult.Lose:
                ResolvePushOrRemove(pusher: def, pushed: atk);
                break;

            case CombatResult.Par:
                break;
        }

        RaiseChargeResult(result);

        _unitsRenderer.UpdateView(atk);
        _unitsRenderer.UpdateView(def);
        _lvlManager.RefreshBoardState();
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
        if(unitGO == null)
        {
            Debug.LogError($"GameObject don't found for {unit}");
            onComplete?.Invoke();
            return false;
        }

        UnitMovement movement = unitGO.GetComponent<UnitMovement>();
        if (movement == null)
        {
            Debug.LogError($"UnitMovement non trovato su {unitGO.name}");
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
        switch (result)
        {
            case CombatResult.Win: def.LoseMorale(1, CauseFrom(atk)); break;
            case CombatResult.Lose: atk.LoseMorale(1, CauseFrom(def)); break;
            case CombatResult.Par:
                atk.LoseMorale(1, CauseFrom(def));
                def.LoseMorale(1, CauseFrom(atk)); break;
        }

        bool done = false;
        GameObject atkGO = _unitsRenderer.GetGameObject(atk);
        UnitMovement movement = atkGO.GetComponent<UnitMovement>();
        Vector3 defWorldPos = _map.transform.position + def.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);

        // difensore reagisce
        GameObject defGO = _unitsRenderer.GetGameObject(def);
        UnitMovement defMovement = defGO != null ? defGO.GetComponent<UnitMovement>() : null;
        Vector3 atkWorldPos = _map.transform.position + atk.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);

        movement.PlaySkirmish(defWorldPos,
             onComplete: () =>
             {
                 _unitsRenderer.UpdateView(atk);
                 _unitsRenderer.UpdateView(def);
                 _lvlManager.RefreshBoardState();
                 done = true;
             },
             onImpact: () =>
             {
                 defMovement?.PlayHitReaction(atkWorldPos);
                 RaiseCombactResult(result);
             });

        float elapsed = 0f;
        yield return new WaitUntil(() => done || (elapsed += Time.deltaTime) > 5f);
        if (!done) Debug.LogWarning($"[SCONTRO] animation not complete from {atk}: i still continue");
    }
    #endregion

    #region Lancio
    public void ExecuteThrow(AbstractUnitsRunTime atk, PoliceRuntime target, ThrowItemSO item)
    {
        if (atk is not SpezzoneRuntime spezzone) return;
        if(!spezzone.Inventory.HasItem(item))
        {
            _alertEvent?.Raise("No throw objects");
            return;
        }

        if(spezzone.ActionPoints < item.ActionPointCost)
        {
            _alertEvent?.Raise($"Not enough PA, {item.ActionPointCost} needed");
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
        if (item == null) return false;
        if(atk is not SpezzoneRuntime spezzone) return false;

        if (!spezzone.Inventory.HasItem(item))
        {
            _alertEvent?.Raise("No barricade objects");
            return false;
        }

        if (!IsCellAvailable(targetCell))
        {
            _alertEvent?.Raise("Not available cell for barricade");
            return false;
        }

        if(spezzone.ActionPoints < item.ActionPointCost)
        {
            _alertEvent?.Raise($"Not enough PA, {item.ActionPointCost} needed");
            return false;
        }

        spezzone.TrySpendActionPoint(item.ActionPointCost);
        spezzone.Inventory.ConsumeItem(item);
        targetCell.TryPlaceBarricade(new BarricadeRuntime(item));

        Vector3 worldPos = _map.transform.position + targetCell.Coordinates.ToWorldPosition(_map.CellSize);
        Instantiate(item.GraphicPrefab, worldPos, Quaternion.identity);
        return true;
    }

    #endregion

    #region Chant & SitDown

    public bool ExecuteChant(AbstractUnitsRunTime caster)
    {
        const int chantCost = 3;
        if (!caster.TrySpendActionPoint(chantCost))
        {
            Debug.Log($"Coro non eseguito: PA insufficienti (servono {chantCost})");
            _alertEvent?.Raise($"Not enough PA, {chantCost} needed");
            return false;
        }

        caster.GainMorale(1);
        _unitsRenderer.UpdateView(caster);
        Debug.Log($"Coro: {caster} +1 morale (ora {caster.Morale}/{caster.MaxMorale})");

        foreach (HexCoordinates n in caster.PositionCell.Coordinates.GetNeighbors())
        {
            if (!_map.TryGetCell(n, out HexCell cell)) continue;
            if (cell.OccupiedBy is SpezzoneRuntime spezzone && spezzone.Status == UnitsStatus.Alive)
            {
                spezzone.GainMorale(1);
                _unitsRenderer.UpdateView(spezzone);
                Debug.Log($"Coro: {spezzone} +1 morale (ora {spezzone.Morale}/{spezzone.MaxMorale})");
            }
        }
        _lvlManager.RefreshBoardState();
        return true;
    }

    public bool ExecuteSitStand(AbstractUnitsRunTime unit)
    {
        if (!unit.IsSeated)
        {
            const int sitCost = 1;
            if (!unit.TrySpendActionPoint(sitCost))
            {
                Debug.Log($"Seduta non eseguita: PA insufficienti (servono {sitCost})");
                _alertEvent?.Raise($"Not enough PA, {sitCost} needed");
                return false;
            }
            unit.SitDown();
            Debug.Log($"{unit} si siede. Def ora {unit.Def}, PA rimasti {unit.ActionPoints}");
            return true;
        }

        const int standCost = 2;
        if (!unit.TrySpendActionPoint(standCost))
        {
            Debug.Log($"Rialzata non eseguita: PA insufficienti (servono {standCost})");
            _alertEvent?.Raise($"Not enough PA, {standCost} needed");
            return false;
        }
        unit.StandUp();
        Debug.Log($"{unit} si rialza. Def ora {unit.Def}, PA rimasti {unit.ActionPoints}");
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

        if(!_lvlManager.IsGameActive)
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

        StartCoroutine(ExecutePoliceTurn());
    }

    private IEnumerator ExecutePoliceTurn()
    {
        yield return StartCoroutine(_policeAI.ExecutePoliceActions());

        Debug.Log("--- FINE TURNO POLIZIA ---");
        _waitingForPolice = false;

        if(!_lvlManager.IsGameActive)
        {
            yield break;
        }

        foreach (var spezzone in _lvlManager.Spezzoni)
        {
            if (!spezzone.IsAlive) continue;
            spezzone.RefillActionPoints();
        }

        _startPlayerTurnEvent.Raise();
    }
}
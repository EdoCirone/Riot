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
    private bool _isConfigured;
    private bool _waitingForPolice = false;


    public PathFinder PathFinder => _pathFinder;
    public GameEventSO EndPlayerTurnEvent => _endPlayerTurnEvent;
    public GameEventSO StartPlayerTurnEvent => _startPlayerTurnEvent;

    public UnitEventSO ThrowEvent => _throwEvent;
    private bool IsCellAvailable(HexCell cell) => TacticalQuery.IsCellAvailable(cell);
    public bool IsConfigured => _isConfigured;
    public bool IsPoliceTurn => _waitingForPolice;

    //Helper method to determine the cause of morale loss based on the source unit type
    private static MoraleLossCause CauseFrom(AbstractUnitsRunTime source)
    => source is PoliceRuntime ? MoraleLossCause.PoliceContact : MoraleLossCause.Other;

    public void CollectConfigurationErrors(
    LVLManager expectedLevel,
    List<string> errors)
    {
        int initialErrorCount = errors.Count;

        if (_lvlManager == null)
        {
            errors.Add("TurnManager: LVLManager non assegnato");
        }
        else if (_lvlManager != expectedLevel)
        {
            errors.Add("TurnManager: riferimento a un LVLManager diverso");
        }

        if (_pathFinder == null)
            errors.Add("TurnManager: PathFinder non assegnato");

        if (_policeAI == null)
        {
            errors.Add("TurnManager: PoliceAI non assegnata");
        }
        else if (!_policeAI.isActiveAndEnabled)
        {
            errors.Add("TurnManager: PoliceAI disabilitata");
        }

        if (_startPlayerTurnEvent == null)
            errors.Add("TurnManager: StartPlayerTurnEvent non assegnato");

        if (_endPlayerTurnEvent == null)
            errors.Add("TurnManager: EndPlayerTurnEvent non assegnato");

        if (_throwEvent == null)
            errors.Add("TurnManager: ThrowEvent non assegnato");

        _isConfigured = errors.Count == initialErrorCount;
    }

    private void Start()
    {
        if (!_isConfigured
            || _lvlManager == null
            || !_lvlManager.IsConfigured)
        {
            enabled = false;
            return;
        }

        _unitsRenderer = _lvlManager.Renderer;
        _map = _lvlManager.Map;

        _startPlayerTurnEvent.Raise();
    }
    #region Charge

    public bool CanCharge(
        AbstractUnitsRunTime atk,
        AbstractUnitsRunTime def,
        out HexCell destinationCell)
    {
        return ChargeResolver.CanStart(
            atk,
            def,
            _map,
            out destinationCell
        );
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

    private void PushResolution(
     AbstractUnitsRunTime atk,
     AbstractUnitsRunTime def)
    {
        HexCell impactCell = def?.PositionCell;

        PushResolver.PushResult pushResult =
            PushResolver.Resolve(atk, def, _map);

        if (!pushResult.IsResolved)
        {
            Debug.LogError(
                "[PUSH] Resolution failed: invalid units, " +
                "positions or adjacency"
            );

            return;
        }

        foreach (PushResolver.PushMove move in pushResult.Moves)
        {
            _unitsRenderer.UpdateView(move.Unit);
        }

        if (pushResult.WasRemoved)
        {
            Debug.Log(
                $"[PUSH] no exit: {def} removed at " +
                $"{impactCell?.Coordinates}"
            );
        }
        else
        {
            Debug.Log(
                $"[PUSH] applied: " +
                $"{pushResult.Moves.Count} unit(s) moved"
            );
        }

        if (def.IsAlive)
        {
            impactCell = def.PositionCell;

            def.LoseMorale(1, CauseFrom(atk));
            _unitsRenderer.FlashDamage(def);
        }

        ApplyPanicWave(impactCell, def);

        ReportAggression(def, atk, impactCell);

        _lvlManager.CheckObjectiveIntrusion(atk);

        _chargeEvent?.Raise();

        _unitsRenderer.UpdateView(atk);
        _unitsRenderer.UpdateView(def);
        _lvlManager.RefreshBoardState();
    }

    /// <summary>
    /// Applica l'onda. NON chiama RefreshBoardState: lo fa il chiamante, che di solito
    /// sta risolvendo qualcosa di più grande (la carica) e deve ricalcolare una volta sola.
    /// </summary>
    private void ApplyPanicWave(
        HexCell origin,
        AbstractUnitsRunTime epicentre)
    {
        if (origin == null)
        {
            Debug.LogWarning("[PANIC] no origin cell: wave skipped");
            return;
        }

        IReadOnlyList<PanicResolver.PanicEffect> effects =
            PanicResolver.Resolve(origin, epicentre, _map);

        foreach (PanicResolver.PanicEffect effect in effects)
        {
            _unitsRenderer.UpdateView(effect.Unit);
        }

        Debug.Log(
            $"[PANIC] wave from {origin.Coordinates}: " +
            $"{effects.Count} unit(s) affected"
        );
    }
    #endregion

    #region Moviment

    /// <summary>
    /// Esegue il movimento lungo le celle indicate.
    ///
    /// Contratto:
    /// - path non contiene la posizione iniziale dell'unità;
    /// - ogni elemento rappresenta una cella da raggiungere;
    /// - ogni elemento costa 1 punto azione;
    /// - la collezione ricevuta non viene modificata.
    /// </summary>
    public bool ExecuteMovement(
        AbstractUnitsRunTime unit,
        IReadOnlyList<HexCell> path,
        Action onComplete = null)
    {
        if (unit == null || unit.PositionCell == null)
        {
            Debug.LogError("[MOVEMENT] Invalid unit or missing position");
            onComplete?.Invoke();
            return false;
        }

        if (path == null || path.Count == 0)
        {
            _alertEvent?.Raise("No Path Found");
            onComplete?.Invoke();
            return false;
        }

        if (path[0] == unit.PositionCell)
        {
            Debug.LogError(
                "[MOVEMENT] Invalid path: the starting cell must not be included");

            onComplete?.Invoke();
            return false;
        }

        // Copia difensiva: l'animazione è asincrona e non deve dipendere
        // da eventuali modifiche apportate dal chiamante alla lista originale.
        List<HexCell> movementPath = new List<HexCell>(path);

        int cost = movementPath.Count;

        if (unit.ActionPoints < cost)
        {
            Debug.Log(
                $"Insufficient AP to move: {cost} required, {unit.ActionPoints} available");

            onComplete?.Invoke();
            return false;
        }

        GameObject unitGO = _unitsRenderer.GetGameObject(unit);

        if (unitGO == null)
        {
            Debug.LogError($"[MOVEMENT] GameObject not found for {unit}");
            onComplete?.Invoke();
            return false;
        }

        UnitMovement movement = unitGO.GetComponent<UnitMovement>();

        if (movement == null)
        {
            Debug.LogError($"[MOVEMENT] UnitMovement not found on {unitGO.name}");
            onComplete?.Invoke();
            return false;
        }

        if (movement.IsMoving)
        {
            Debug.LogWarning($"[MOVEMENT] {unitGO.name} is already moving");
            onComplete?.Invoke();
            return false;
        }

        if (!unit.TrySpendActionPoint(cost))
        {
            Debug.LogError($"[MOVEMENT] Failed to spend {cost} AP");
            onComplete?.Invoke();
            return false;
        }

        _startFollowEvent?.Raise(unitGO);

        movement.MoveAlongPath(movementPath, _lvlManager.Map, () =>
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

    private void RaiseCombatResult(CombatResult result)
    {
        switch (result)
        {
            case CombatResult.Win: _skirmishWinEvent?.Raise(); break;
            case CombatResult.Lose: _skirmishLoseEvent?.Raise(); break;
            case CombatResult.Par: _skirmishParEvent?.Raise(); break;
        }
    }

    public IEnumerator ExecuteSkirmish(
      AbstractUnitsRunTime atk,
      AbstractUnitsRunTime def)
    {
        CombatResolver.SkirmishResolution resolution =
            CombatResolver.ResolveSkirmish(
                atk,
                def,
                _map
            );

        if (!resolution.Succeeded)
        {
            Debug.Log(
                $"[TURN] Skirmish not executed: " +
                $"{resolution.Failure}"
            );

            yield break;
        }

        CombatResult result = resolution.Result.Value;

        bool done = false;
        bool finalized = false;

        void FinalizeOnce()
        {
            if (finalized)
                return;

            finalized = true;

            _unitsRenderer.UpdateView(atk);
            _unitsRenderer.UpdateView(def);
            _lvlManager.RefreshBoardState();

            ReportAggression(def, atk);
        }

        GameObject atkGO =
            _unitsRenderer.GetGameObject(atk);

        UnitMovement movement =
            atkGO.GetComponent<UnitMovement>();

        Vector3 defWorldPos =
            _map.GridToWorld(
                def.PositionCell.Coordinates
            );

        GameObject defGO =
            _unitsRenderer.GetGameObject(def);

        UnitMovement defMovement =
            defGO != null
                ? defGO.GetComponent<UnitMovement>()
                : null;

        Vector3 atkWorldPos =
            _map.GridToWorld(
                atk.PositionCell.Coordinates
            );

        movement.PlaySkirmish(
            defWorldPos,
            onComplete: () =>
            {
                FinalizeOnce();
                done = true;
            },
            onImpact: () =>
            {
                defMovement?.PlayHitReaction(atkWorldPos);
                RaiseCombatResult(result);
            }
        );

        float elapsed = 0f;

        yield return new WaitUntil(
            () => done
                || (elapsed += Time.deltaTime) > 5f
        );

        if (!done)
        {
            Debug.LogWarning(
                $"[Skirmish] animation not complete from " +
                $"{atk}: finalizing anyway"
            );

            FinalizeOnce();
        }
    }
    #endregion

    #region Item Check

    private void ReportItemActionFailure(
    ItemActionFailure failure,
    ItemSO item,
    string missingItemMessage,
    string invalidTargetMessage)
    {
        switch (failure)
        {
            case ItemActionFailure.MissingItem:
                _alertEvent?.Raise(missingItemMessage);
                break;

            case ItemActionFailure.InsufficientActionPoints:
                _alertEvent?.Raise(
                    $"Not enough AP, {item?.ActionPointCost ?? 0} needed"
                );
                break;

            case ItemActionFailure.InvalidTarget:
                _alertEvent?.Raise(invalidTargetMessage);
                break;

            case ItemActionFailure.ActionNotAllowed:
                _alertEvent?.Raise("Action not allowed");
                break;

            case ItemActionFailure.InvalidActor:
            case ItemActionFailure.InvalidItem:
            case ItemActionFailure.ResolutionFailed:
                Debug.LogError(
                    $"[ITEM ACTION] Resolution failed: {failure}"
                );
                break;
        }
    }

    #endregion

    #region Lancio
    public void ExecuteThrow(
     AbstractUnitsRunTime atk,
     PoliceRuntime target,
     ThrowItemSO item)
    {
        if (atk is not SpezzoneRuntime spezzone)
            return;

        ItemActionResolver.ItemActionResult result =
            ItemActionResolver.ResolveThrow(
                spezzone,
                target,
                item,
                _map
            );

        if (!result.Succeeded)
        {
            ReportItemActionFailure(
                result.Failure,
                item,
                missingItemMessage: "No throw objects",
                invalidTargetMessage: "Invalid throw target"
            );

            return;
        }

        _throwEvent.Raise(target);

        ReportAggression(target, spezzone);

        _unitsRenderer.UpdateView(target);
        _lvlManager.RefreshBoardState();
    }

    #endregion

    #region Barricade


    public bool ExecuteBarricade(
     AbstractUnitsRunTime atk,
     HexCell targetCell,
     BarricadeSO item)
    {
        if (atk is not SpezzoneRuntime spezzone)
            return false;

        ItemActionResolver.ItemActionResult result =
            ItemActionResolver.ResolveBarricade(
                spezzone,
                targetCell,
                item
            );

        if (!result.Succeeded)
        {
            string invalidTargetMessage =
                targetCell != null && targetCell.IsObjective
                    ? "Cannot barricade an objective"
                    : "Not available cell for barricade";

            ReportItemActionFailure(
                result.Failure,
                item,
                missingItemMessage: "No barricade objects",
                invalidTargetMessage
            );

            return false;
        }

        if (item.GraphicPrefab != null)
        {
            Vector3 worldPosition =
                _map.GridToWorld(targetCell.Coordinates);

            Instantiate(
                item.GraphicPrefab,
                worldPosition,
                Quaternion.identity
            );
        }
        else
        {
            Debug.LogError(
                $"[BARRICADE] Graphic prefab missing on {item.name}"
            );
        }

        _lvlManager.RefreshBoardState();
        return true;
    }

    #endregion

    #region Chant & SitDown

    private void ReportUnitActionFailure(
        string actionName,
        UnitActionResolver.UnitActionResult result)
    {
        switch (result.Failure)
        {
            case UnitActionFailure.InsufficientActionPoints:
                _alertEvent?.Raise(
                    $"Not enough AP, {result.ActionPointCost} needed"
                );
                break;

            case UnitActionFailure.ActionNotAllowed:
                _alertEvent?.Raise("Action not allowed");
                break;

            case UnitActionFailure.InvalidUnit:
            case UnitActionFailure.InvalidPosition:
            case UnitActionFailure.InvalidMap:
                Debug.LogError(
                    $"[{actionName}] Resolution failed: {result.Failure}"
                );
                break;

            default:
                Debug.LogError(
                    $"[{actionName}] Unexpected failure: {result.Failure}"
                );
                break;
        }
    }

    public bool ExecuteChant(AbstractUnitsRunTime caster)
    {
        UnitActionResolver.UnitActionResult result =
            UnitActionResolver.ResolveChant(
                caster,
                _map
            );

        if (!result.Succeeded)
        {
            ReportUnitActionFailure("CHANT", result);
            return false;
        }

        foreach (AbstractUnitsRunTime affectedUnit
                 in result.AffectedUnits)
        {
            _unitsRenderer.UpdateView(affectedUnit);
        }

        _lvlManager.RefreshBoardState();
        return true;
    }

    public bool ExecuteSitStand(AbstractUnitsRunTime unit)
    {
        UnitActionResolver.UnitActionResult result =
            UnitActionResolver.ResolveSitStand(unit);

        if (!result.Succeeded)
        {
            ReportUnitActionFailure("SIT/STAND", result);
            return false;
        }

        foreach (AbstractUnitsRunTime affectedUnit
                 in result.AffectedUnits)
        {
            _unitsRenderer.UpdateView(affectedUnit);
        }

        _lvlManager.RefreshBoardState();

        Debug.Log(
            $"{unit} " +
            $"{(result.WasSeated ? "stands up" : "sits down")}. " +
            $"Def now {unit.Def}, AP left {unit.ActionPoints}"
        );

        return true;
    }

    #endregion

    #region Police

    private IEnumerator ExecutePoliceTurn()
    {
        // ⚠ Senza questa guardia un _policeAI non assegnato è un HARD LOCK, non un errore.
        // EndTurn ha già messo _waitingForPolice = true; la NullReferenceException ucciderebbe
        // la coroutine PRIMA della riga che lo rimette a false, e l'input resterebbe bloccato
        // per sempre con CanAcceptPlayerInput. Si salta il turno della polizia e si va avanti:
        // una partita senza avversario è diagnosticabile, una partita congelata no.
        if (_policeAI != null)
            yield return StartCoroutine(_policeAI.ExecutePoliceActions());
        else
            Debug.LogError("[TURN] PoliceAI not assigned in TurnManager: the police turn is skipped");

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

    /// <summary>
    /// Un poliziotto è stato aggredito da uno spezzone: il presidio attorno si sveglia.
    ///
    /// ⚠ Esiste come metodo unico apposta. Prima l'allarme era scritto a mano dentro il solo
    /// ExecuteSkirmish, quindi lanciare un sanpietrino o caricare un poliziotto non svegliava
    /// nessuno: due modi di aggredire su tre erano muti, e non se ne accorgeva nessuno perché
    /// non producevano nessun errore — semplicemente il presidio restava fermo.
    /// I chiamanti restano tre (scontro, lancio, spinta) ma la DECISIONE è qui: chi aggiunge
    /// un'azione ostile nuova chiama questo, non riscrive la regola.
    ///
    /// L'origine è un parametro opzionale perché la carica deve passare la cella dell'URTO,
    /// catturata prima della spinta: dopo, la vittima si è spostata o è uscita di scena.
    /// </summary>
    private void ReportAggression(AbstractUnitsRunTime victim, AbstractUnitsRunTime aggressor,
                                  HexCell origin = null)
    {
        if (victim is not PoliceRuntime) return;
        if (aggressor is not SpezzoneRuntime) return;

        origin ??= victim.PositionCell;
        if (origin == null) return;

        _lvlManager.RaiseAlarmAround(origin, $"{victim} attacked by {aggressor}");
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
}

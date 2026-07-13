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

    [Header("Events")]
    [SerializeField] private GameEventSO _startPlayerTurnEvent;
    [SerializeField] private GameEventSO _endPlayerTurnEvent;
    [SerializeField] private UnitEventSO _throwEvent;
    [SerializeField] private GameObjectEventSO _startFollowEvent;
    [SerializeField] private GameEventSO _stopFollowEvent;
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

    //Mi serve esposta publica per l'highlight di feedback
    public bool CanCharge(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        if (def.IsSeated) return false;
        return HasChargeRoom(atk.PositionCell.Coordinates, def.PositionCell.Coordinates, out _);
    }

    public bool ExecuteCharge(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        if (def.IsSeated)
        {
            Debug.Log("Carica non valida: bersaglio seduto (barricata umana)");
            return false;
        }

        HexCoordinates atkCoord = atk.PositionCell.Coordinates;
        HexCoordinates defCoord = def.PositionCell.Coordinates;
        Vector3 defenderWorldPos = _map.transform.position + def.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);
        

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

        GameObject atkGO = _unitsRenderer.GetGameObject(atk);
        UnitMovement movement = atkGO.GetComponent<UnitMovement>();

        _map.TryGetCell(chargeDestination, out HexCell destinationCell);

        Action onComplete = () =>
        {
            PushResolution(atk, def);

        };

        atk.SetPosition(destinationCell);
        movement.PlayCharge(destinationCell, defenderWorldPos, def.PositionCell, def, _map, onComplete);


        return true;
    }

    public HexCell CalculatePushDestination(HexCoordinates atkCoord, HexCoordinates defCoord)
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

    private void PushResolution(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        CombatResult result = CombatResolver.Resolve(atk, def);
        switch (result)
        {
            case CombatResult.Win:
                {
                    HexCell target = CalculatePushDestination(atk.PositionCell.Coordinates, def.PositionCell.Coordinates);
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
                    HexCell target = CalculatePushDestination(def.PositionCell.Coordinates, atk.PositionCell.Coordinates);
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

    private HexCell FoundNearCellAvailable(HexCoordinates startPushCell, HexCoordinates endPushCell)
    {
        HexCoordinates[] startCellNeighbors = startPushCell.GetNeighbors();
        HexCoordinates[] endCellNeighbors = endPushCell.GetNeighbors();

        List<HexCoordinates> common = new List<HexCoordinates>();
        foreach (var n in startCellNeighbors)
            foreach (var m in endCellNeighbors)
                if (n == m) common.Add(n);

        if (common.Count > 1 && UnityEngine.Random.value > 0.5f)
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

    public bool ExecuteMovement(AbstractUnitsRunTime unit, List<HexCell> path, System.Action onComplete = null)
    {
        if (path == null || path.Count == 0)
        {
            _alertEvent?.Raise("No Path Found");
            onComplete?.Invoke();
            return false;
        }

        // Consuma PA
        int cost = path.Count;
        if (!unit.TrySpendActionPoint(cost))
        {
            Debug.Log($"Movimento non eseguito: PA insufficienti (servono {cost})");
            onComplete?.Invoke();
            return false;
        }

        // Rimuovi la cella corrente se presente
        if (path.Count > 0 && path[0] == unit.PositionCell)
            path.RemoveAt(0);

        if (path.Count == 0)
        {
            onComplete?.Invoke();
            return true;
        }

        // Ottieni il GameObject
        GameObject unitGO = _unitsRenderer.GetGameObject(unit);
        if (unitGO == null)
        {
            Debug.LogError($"GameObject non trovato per {unit}");
            onComplete?.Invoke();
            return false;
        }



        // Ottieni UnitMovement
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

        // AVVIA MOVIMENTO CON CALLBACK
        
        _startFollowEvent?.Raise(unitGO);
        movement.MoveAlongPath(path, _lvlManager.Map, () =>
        {
            _unitsRenderer.UpdateView(unit);
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

    public IEnumerator ExecuteSkirmish(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        HexCoordinates atkCoord = atk.PositionCell.Coordinates;
        HexCoordinates defCoord = def.PositionCell.Coordinates;

        if (atkCoord.Distance(defCoord) != 1)
        {
            Debug.Log("Scontro non valido: le unità non sono adiacenti");
            yield break;
        }

        const int skirmishCost = 1;
        if (!atk.TrySpendActionPoint(skirmishCost))
        {
            Debug.Log($"Scontro non eseguito: punti azione insufficienti (servono {skirmishCost})");
            yield break;
        }

        CombatResult result = CombatResolver.Resolve(atk, def);
        switch (result)
        {
            case CombatResult.Win: def.LoseMorale(1); break;
            case CombatResult.Lose: atk.LoseMorale(1); break;
            case CombatResult.Par: atk.LoseMorale(1); def.LoseMorale(1); break;
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
                done = true;
            },
            onImpact: () => defMovement?.PlayHitReaction(atkWorldPos));

        yield return new WaitUntil(() => done);
    }
    #endregion

    #region Lancio
    public void ExecuteThrow(AbstractUnitsRunTime atk, PoliceRuntime target, ThrowItemSO item)
    {
        if (item == null)
        {
            _alertEvent?.Raise("not selected Item");
            return;
        }
        if (!atk.TrySpendActionPoint(item.ActionPointCost)) 
        {
            _alertEvent?.Raise($"Not enough PA, {item.ActionPointCost} needed");
            return;
        }
        _throwEvent.Raise(target);
        target.LoseMorale(item.MoralLost);
        if (atk is SpezzoneRuntime spezzone)
            spezzone.Inventory.ConsumeItem(item);
        _unitsRenderer.UpdateView(target);
    }

    #endregion

    #region Barricade


    public bool ExecuteBarricade(AbstractUnitsRunTime atk, HexCell targetCell, BarricadeSO item)
    {
        if (item == null)
        {
            return false;
        }


        BarricadeRuntime barricade = new BarricadeRuntime(item);

        if (!targetCell.TryPlaceBarricade(barricade))
        {
            _alertEvent?.Raise("Barricata non piazzata: cella non disponibile");
            return false;
        }

        if (!atk.TrySpendActionPoint(item.ActionPointCost))
        {
            targetCell.RemoveBarricade();
            _alertEvent?.Raise($"Not enough PA, {item.ActionPointCost} needed");
            return false;
        }
        if (atk is SpezzoneRuntime spezzone)
            spezzone.Inventory.ConsumeItem(item);

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

        _waitingForPolice = true;
        _endPlayerTurnEvent.Raise();

        Debug.Log("--- TURNO POLIZIA ---");

        foreach (var police in _lvlManager.Police)
        {
            if (police.Status == UnitsStatus.Disperse) continue;
            police.RefillActionPoints();
        }

        StartCoroutine(ExecutePoliceTurn());
    }

    private IEnumerator ExecutePoliceTurn()
    {
        yield return StartCoroutine(_policeAI.ExecutePoliceActions());

        Debug.Log("--- FINE TURNO POLIZIA ---");
        _waitingForPolice = false;

        foreach (var spezzone in _lvlManager.Spezzoni)
        {
            if (spezzone.Status == UnitsStatus.Disperse) continue;
            spezzone.RefillActionPoints();
        }

        _startPlayerTurnEvent.Raise();
    }
}
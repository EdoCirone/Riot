using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{

    [Header("Reference")]
    [SerializeField] LVLManager _lvlManager;
    [SerializeField] private PoliceAI _policeAI;

    [Header("Events")]
    [SerializeField] GameEventSO _endTurnEvent;
    //This events are placeholder for future application like sfx,vfx o HUD
    //[SerializeField] GameEventSO _winCombatEvent;
    //[SerializeField] GameEventSO _loseCombatEvent;
    //[SerializeField] GameEventSO _parCombatEvent;


    private TurnPhases _currentPhase;
    private List<MovementOrder> _movOrders = new List<MovementOrder>();
    private List<AttackOrder> _atkOrders = new List<AttackOrder>();

    private HexGrid _map;
    private UnitsRenderer _unitsRenderer;

    private bool _waitingForPolice = false;

    public GameEventSO EndTurnEvent => _endTurnEvent;



    private void Start()
    {
        if (_lvlManager == null)
        {
            Debug.LogWarning("Need LVL Manager in TurnManager");
            return;
        }

        _unitsRenderer = _lvlManager.Renderer;
        _map = _lvlManager.Map;
    }

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
                Debug.Log($"PushHandle: spingo verso {pushCoord}, cella trovata: {returnCell != null}, disponibile: {returnCell != null && IsCellAvailable(returnCell)}");
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
                    HexCell target = PushHandle(atk.PositionCell.Coordinates, def.PositionCell.Coordinates, result);
                    if (target != null)
                    {
                        Debug.Log($"Spingo def da {def.PositionCell.Coordinates} a {target.Coordinates}");
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

    private HexCell FoundNearCellAvailable(HexCoordinates startPushCell, HexCoordinates endPushCell)
    {
        HexCoordinates[] startCellNeighbors = startPushCell.GetNeighbors();
        HexCoordinates[] endCellNeighbors = endPushCell.GetNeighbors();

        List<HexCoordinates> common = new List<HexCoordinates>();
        foreach (var n in startCellNeighbors)
            foreach (var m in endCellNeighbors)
                if (n == m) common.Add(n);

        // mischia in ordine casuale
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

    private bool IsCellAvailable(HexCell cell)
    {
        if (cell == null) return false;
        if (cell.OccupiedBy is PoliceRuntime) return false;

        return cell.Type.IsWalkable;
    }

    public void AddMovementOrder(MovementOrder order) { _movOrders.Add(order); }
    public void AddAttackOrder(AttackOrder order) { _atkOrders.Add(order); }
    public void ExecuteResolution()
    {
        if (!_waitingForPolice)
        {
            // corteo Phase
            foreach (var order in _movOrders)
            {
                order.SelectedUnit.SetPosition(order.DirectionCell);
                _unitsRenderer.UpdateView(order.SelectedUnit);
            }
            _movOrders.Clear();
            foreach (var order in _atkOrders)
                PushResolution(order.Atk, order.Def);
            _atkOrders.Clear();
            _waitingForPolice = true;
            Debug.Log("--- TURNO POLIZIA ---");
        }
        else
        {
            // police Phase
            _policeAI.ExecutePoliceActions();
            foreach (var order in _movOrders)
            {
                order.SelectedUnit.SetPosition(order.DirectionCell);
                _unitsRenderer.UpdateView(order.SelectedUnit);
            }
            _movOrders.Clear();
            foreach (var order in _atkOrders)
            {
                PushResolution(order.Atk, order.Def);
            }
            
            _atkOrders.Clear();
            _waitingForPolice = false;
            EndTurn();
        }
    }

    private void EndTurn()
    {
        _endTurnEvent.Raise();
        _currentPhase = TurnPhases.Decision;

    }
}

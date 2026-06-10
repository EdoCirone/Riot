using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{

    [Header("Reference")]
    [SerializeField] LVLManager _lvlManager;


    [Header("Events")]
    [SerializeField] GameEventSO _endTurnEvent;
    [SerializeField] GameEventSO _winCombactEvent;
    [SerializeField] GameEventSO _loseCombactEvent;
    [SerializeField] GameEventSO _parCombactEvent;


    private TurnPhases _currentPhase;
    private List<MovementOrder> _orders = new List<MovementOrder>();

    private HexGrid _map;
    public GameEventSO EndTurnEvent => _endTurnEvent;

    private void Start()
    {
        if (_lvlManager == null)
        {
            Debug.LogWarning("Need LVL Manager in TurnManager");
            return;
        }
        _map = _lvlManager.Map;
    }

    private HexCell PushHandle(HexCoordinates atkCoord, HexCoordinates defCoord, CombatResult result)
    {

        int resultQ = (atkCoord.Q - defCoord.Q);
        int resultR = (atkCoord.R - defCoord.R);

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

    private void AddOrder(MovementOrder order)
    {
        _orders.Add(order);
    }

    private void ExecuteResolution()
    {
        foreach (var order in _orders)
        {
            order.SelectedSpezzone.SetPosition(order.DirectionCell);
        }
        _orders.Clear();
        _currentPhase = TurnPhases.EndTurn;
    }

    private void EndTurn()
    {
        _endTurnEvent.Raise();
        _currentPhase = TurnPhases.Decision;

    }
}

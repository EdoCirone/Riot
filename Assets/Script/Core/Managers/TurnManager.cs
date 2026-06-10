using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] GameEventSO _endTurnEvent;

    private TurnPhases _currentPhase;
    private List<MovementOrder> _orders = new List<MovementOrder>();

    public GameEventSO EndTurnEvent => _endTurnEvent;

    private void AddOrder( MovementOrder order)
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

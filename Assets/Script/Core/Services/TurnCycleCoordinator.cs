using System.Collections;
using UnityEngine;

public sealed class TurnCycleCoordinator
{
    private readonly LVLManager _level;
    private readonly PoliceAI _policeAI;
    private readonly UnitsRenderer _renderer;
    private readonly GameEventSO _startPlayerTurnEvent;
    private readonly GameEventSO _endPlayerTurnEvent;

    public bool IsPoliceTurn { get; private set; }

    public TurnCycleCoordinator(
        LVLManager level,
        PoliceAI policeAI,
        UnitsRenderer renderer,
        GameEventSO startPlayerTurnEvent,
        GameEventSO endPlayerTurnEvent)
    {
        _level = level;
        _policeAI = policeAI;
        _renderer = renderer;
        _startPlayerTurnEvent = startPlayerTurnEvent;
        _endPlayerTurnEvent = endPlayerTurnEvent;
    }

    public IEnumerator CompletePlayerTurn()
    {
        if (IsPoliceTurn)
            yield break;

        _level.RefreshBoardState();

        if (_level.CheckCohesionDefeat())
            yield break;

        IsPoliceTurn = true;
        _endPlayerTurnEvent.Raise();

        if (!_level.IsGameActive)
        {
            IsPoliceTurn = false;
            yield break;
        }

        Debug.Log("--- TURNO POLIZIA ---");

        foreach (PoliceRuntime police in _level.Police)
        {
            if (police.IsAlive)
                police.RefillActionPoints();
        }

        // Il panico degli spezzoni scala alla fine del turno giocatore.
        foreach (SpezzoneRuntime spezzone in _level.Spezzoni)
        {
            if (!spezzone.IsAlive)
                continue;

            spezzone.TickPanic();
            _renderer.UpdateView(spezzone);
        }

        _level.RefreshBoardState();

        if (_policeAI != null)
        {
            yield return _policeAI.ExecutePoliceActions();
        }
        else
        {
            Debug.LogError(
                "[TURN] PoliceAI not assigned: " +
                "the police turn is skipped"
            );
        }

        Debug.Log("--- FINE TURNO POLIZIA ---");
        IsPoliceTurn = false;

        if (!_level.IsGameActive)
            yield break;

        foreach (SpezzoneRuntime spezzone in _level.Spezzoni)
        {
            if (spezzone.IsAlive)
                spezzone.RefillActionPoints();
        }

        // Il panico della polizia scala alla fine del suo turno.
        foreach (PoliceRuntime police in _level.Police)
        {
            if (!police.IsAlive)
                continue;

            police.TickPanic();
            _renderer.UpdateView(police);
        }

        foreach (PoliceRuntime police in _level.Police)
        {
            if (police.IsAlive)
                police.TickAlarm();
        }

        _level.RefreshBoardState();
        _startPlayerTurnEvent.Raise();
    }
}

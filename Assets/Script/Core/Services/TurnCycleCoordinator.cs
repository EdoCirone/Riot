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

        // La sconfitta per Coesione ha priorità sull'occupazione
        // risolta dall'EndPlayerTurnEvent: un corteo già disperso
        // non può rivendicare l'obiettivo.
        if (_level.CheckCohesionDefeat())
            yield break;

        IsPoliceTurn = true;
        _endPlayerTurnEvent.Raise();

        if (!_level.IsGameActive)
        {
            IsPoliceTurn = false;
            yield break;
        }

        ApplyPendingEngagementRules();

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

    private void ApplyPendingEngagementRules()
    {
        LevelTension tension = _level.Tension;

        if (tension == null
            || !tension.PreparePoliceTurn())
        {
            return;
        }

        int updated = 0;
        int overridesPreserved = 0;

        foreach (PoliceRuntime police in _level.Police)
        {
            if (police == null || !police.IsAlive)
                continue;

            if (police.OverridesEngagementRules)
            {
                overridesPreserved++;
                continue;
            }

            if (police.ApplyLevelEngagementRules(
                tension.AppliedRules))
            {
                updated++;
            }
        }

        Debug.Log(
            $"[TENSION] Police rules changed to " +
            $"{tension.AppliedRules}: {updated} unit(s) " +
            $"updated, {overridesPreserved} override(s) preserved"
        );
    }
}

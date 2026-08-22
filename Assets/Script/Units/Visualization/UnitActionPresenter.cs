using System.Collections;
using UnityEngine;

public sealed class UnitActionPresenter
{
    private const float AnimationTimeout = 5f;

    private readonly HexGrid _map;
    private readonly UnitsRenderer _unitsRenderer;

    private readonly GameEventSO _skirmishWinEvent;
    private readonly GameEventSO _skirmishLoseEvent;
    private readonly GameEventSO _skirmishParEvent;

    public UnitActionPresenter(
        HexGrid map,
        UnitsRenderer unitsRenderer,
        GameEventSO skirmishWinEvent,
        GameEventSO skirmishLoseEvent,
        GameEventSO skirmishParEvent)
    {
        _map = map;
        _unitsRenderer = unitsRenderer;
        _skirmishWinEvent = skirmishWinEvent;
        _skirmishLoseEvent = skirmishLoseEvent;
        _skirmishParEvent = skirmishParEvent;
    }

    public IEnumerator PlaySkirmish(
        AbstractUnitsRunTime attacker,
        AbstractUnitsRunTime defender,
        CombatResult result)
    {
        if (attacker?.PositionCell == null ||
            defender?.PositionCell == null)
        {
            Debug.LogError(
                "[SKIRMISH] Cannot present units without positions"
            );

            yield break;
        }

        GameObject attackerObject =
            _unitsRenderer.GetGameObject(attacker);

        if (attackerObject == null)
        {
            Debug.LogError(
                $"[SKIRMISH] GameObject not found for {attacker}"
            );

            yield break;
        }

        UnitMovement attackerMovement =
            attackerObject.GetComponent<UnitMovement>();

        if (attackerMovement == null)
        {
            Debug.LogError(
                $"[SKIRMISH] UnitMovement not found on " +
                $"{attackerObject.name}"
            );

            yield break;
        }

        GameObject defenderObject =
            _unitsRenderer.GetGameObject(defender);

        UnitMovement defenderMovement =
            defenderObject != null
                ? defenderObject.GetComponent<UnitMovement>()
                : null;

        Vector3 defenderWorldPosition =
            _map.GridToWorld(
                defender.PositionCell.Coordinates
            );

        Vector3 attackerWorldPosition =
            _map.GridToWorld(
                attacker.PositionCell.Coordinates
            );

        bool completed = false;

        attackerMovement.PlaySkirmish(
            defenderWorldPosition,
            onComplete: () => completed = true,
            onImpact: () =>
            {
                defenderMovement?.PlayHitReaction(
                    attackerWorldPosition
                );

                RaiseCombatResult(result);
            }
        );

        float elapsed = 0f;

        while (!completed && elapsed < AnimationTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!completed)
        {
            Debug.LogWarning(
                $"[SKIRMISH] Animation not completed by " +
                $"{attacker}: continuing anyway"
            );
        }
    }

    private void RaiseCombatResult(CombatResult result)
    {
        switch (result)
        {
            case CombatResult.Win:
                _skirmishWinEvent?.Raise();
                break;

            case CombatResult.Lose:
                _skirmishLoseEvent?.Raise();
                break;

            case CombatResult.Par:
                _skirmishParEvent?.Raise();
                break;
        }
    }

    public IEnumerator PlayCharge(
    AbstractUnitsRunTime attacker,
    AbstractUnitsRunTime defender,
    HexCell destinationCell)
    {
        if (attacker == null ||
            defender?.PositionCell == null ||
            destinationCell == null)
        {
            Debug.LogError(
                "[CHARGE] Cannot present invalid units or destination"
            );

            yield break;
        }

        GameObject attackerObject =
            _unitsRenderer.GetGameObject(attacker);

        if (attackerObject == null)
        {
            Debug.LogError(
                $"[CHARGE] GameObject not found for {attacker}"
            );

            yield break;
        }

        UnitMovement movement =
            attackerObject.GetComponent<UnitMovement>();

        if (movement == null)
        {
            Debug.LogError(
                $"[CHARGE] UnitMovement not found on " +
                $"{attackerObject.name}"
            );

            yield break;
        }

        Vector3 defenderWorldPosition =
            _map.GridToWorld(
                defender.PositionCell.Coordinates
            );

        bool completed = false;

        movement.PlayCharge(
            destinationCell,
            defenderWorldPosition,
            _map,
            () => completed = true
        );

        float elapsed = 0f;

        while (!completed && elapsed < AnimationTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!completed)
        {
            Debug.LogWarning(
                $"[CHARGE] Animation not completed by " +
                $"{attacker}: continuing anyway"
            );
        }
    }
}

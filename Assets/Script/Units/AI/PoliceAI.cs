using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoliceAI : MonoBehaviour
{

    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private UnitEventSO _onSelectedEvent;

    private TurnManager _turnManager;

    private void Awake()
    {
        if (_lvlManager == null)
        {
            Debug.Log("LVL manager not found in PoliceAI");
            return;
        }

        _turnManager = _lvlManager.TurnManager;
    }

    public IEnumerator ExecutePoliceActions()
    {
        foreach (var police in _lvlManager.Police)
        {
            if (!police.IsAlive) continue;

            _onSelectedEvent?.Raise(police);

            bool actedThisTurn = true;

            while (actedThisTurn && police.ActionPoints > 0 && police.IsAlive)
            {
                actedThisTurn = false;

                List<SpezzoneRuntime> targets = GetTargetsByDistance(police);

                // PASSATA 1 — agire. Scontro se conviene, carica se è legale.
                // La carica NON guarda le statistiche: contro un muro è l'unico strumento
                // che funziona, quindi va cercata su TUTTI i bersagli prima di ripiegare.
                foreach (SpezzoneRuntime target in targets)
                {
                    int distance = police.PositionCell.Coordinates.Distance(target.PositionCell.Coordinates);

                    if (distance == 1)
                    {
                        int atk = CombatResolver.GetEffectiveAtk(police, _lvlManager.Map);
                        int def = CombatResolver.GetEffectiveDef(target, _lvlManager.Map);

                        if (atk <= def)
                        {
                            Debug.Log($"[AI] {police} cannot hurt {target} in melee (atk {atk} vs def {def}): looking elsewhere");
                            continue;
                        }

                        yield return StartCoroutine(_turnManager.ExecuteSkirmish(police, target));
                        actedThisTurn = true;
                        break;
                    }

                    if (distance == 3 && _turnManager.CanCharge(police, target, out _))
                    {
                        Debug.Log($"[AI] {police} charges {target}: pushing, stats do not matter");
                        yield return StartCoroutine(_turnManager.ExecuteCharge(police, target));
                        actedThisTurn = true;
                        break;
                    }
                }

                if (actedThisTurn) continue;

                // PASSATA 2 — nessuna azione disponibile: ci si avvicina al più raggiungibile.
                foreach (SpezzoneRuntime target in targets)
                {
                    HexCoordinates? targetCell = _turnManager.FindBestAdjacentCell(
                        police.PositionCell.Coordinates, target.PositionCell.Coordinates);

                    if (targetCell == null)
                    {
                        Debug.Log($"[AI] {police} has no free adjacent cell toward {target}");
                        continue;
                    }

                    List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(
                        police.PositionCell.Coordinates,
                        targetCell.Value,
                        _lvlManager.Map
                    );

                    if (pathCoords.Count <= 1)
                    {
                        Debug.Log($"[AI] {police} found no path toward {target}");
                        continue;
                    }

                    int maxSteps = Mathf.Min(police.ActionPoints, pathCoords.Count - 1);
                    List<HexCell> path = new List<HexCell>();
                    for (int i = 1; i <= maxSteps; i++)
                    {
                        if (_lvlManager.Map.TryGetCell(pathCoords[i], out HexCell cell))
                            path.Add(cell);
                    }

                    bool finishMovement = false;
                    bool success = _turnManager.ExecuteMovement(police, path, () =>
                    {
                        finishMovement = true;
                    });

                    float elapsed = 0f;
                    yield return new WaitUntil(() => finishMovement || (elapsed += Time.deltaTime) > 5f);
                    if (!finishMovement) Debug.LogWarning($"[AI] {police} movement not completed: continuing");

                    Debug.Log(success
                        ? $"[AI] {police} moved {path.Count} cell(s) toward {target}"
                        : $"[AI] {police} movement failed");

                    if (success)
                    {
                        actedThisTurn = true;
                        break;
                    }
                }

                if (!actedThisTurn)
                    Debug.Log($"[AI] {police} has nothing to do: turn ended with {police.ActionPoints} AP left");
            }
        }
    }

    /// <summary>
    /// Tutti gli spezzoni vivi, ordinati per distanza crescente. Il più vicino è il primo
    /// tentativo, non l'unico: se contro di lui non c'è niente da fare, si prova il dopo.
    /// </summary>
    private List<SpezzoneRuntime> GetTargetsByDistance(PoliceRuntime police)
    {
        List<SpezzoneRuntime> targets = new List<SpezzoneRuntime>();

        foreach (var spezzone in _lvlManager.Spezzoni)
        {
            if (!spezzone.IsAlive) continue;
            targets.Add(spezzone);
        }

        HexCoordinates from = police.PositionCell.Coordinates;
        targets.Sort((a, b) =>
            from.Distance(a.PositionCell.Coordinates)
                .CompareTo(from.Distance(b.PositionCell.Coordinates)));

        return targets;
    }

    private SpezzoneRuntime FoundNearestSpezzone(PoliceRuntime police)
    {
        SpezzoneRuntime nearest = null;
        int minDistance = int.MaxValue;
        foreach (var spezzone in _lvlManager.Spezzoni)
        {
            if (!spezzone.IsAlive) continue;
            int distance = police.PositionCell.Coordinates.Distance(spezzone.PositionCell.Coordinates);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = spezzone;
            }
        }
        return nearest;
    }


}

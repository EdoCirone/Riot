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

                // PASSATA 0 — fuori dal presidio: si torna al posto prima di ogni altra cosa.
                // È il "tenente" del GDD 8.4: non serve un richiamo, basta il guinzaglio.
                if (!IsWithinLeash(police, police.PositionCell.Coordinates))
                {
                    HexCoordinates post = NearestPostCell(police);
                    bool moved = false;
                    yield return StartCoroutine(MoveTowards(police, post, r => moved = r));

                    if (moved) { actedThisTurn = true; continue; }
                    break;
                }

                List<SpezzoneRuntime> targets = GetTargetsByDistance(police);

                // ⚠ Containment non inizia mai lo scontro: presidia e blocca, punto.
                // Finché non esiste l'Allarme (GDD 8.4) un'unità in Containment è di fatto
                // una statua: testare in Engage.
                if (police.EngagementRules != EngagementRules.Containment || police.IsAlarmed)
                {
                    // PASSATA 1 — agire. Scontro se conviene, carica se è legale.
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

                        // La carica sposta l'attaccante: la destinazione deve stare nel guinzaglio.
                        if (distance == 3 && _turnManager.CanCharge(police, target, out HexCell chargeCell)
                            && IsWithinLeash(police, chargeCell.Coordinates))
                        {
                            Debug.Log($"[AI] {police} charges {target}: pushing, stats do not matter");
                            yield return StartCoroutine(_turnManager.ExecuteCharge(police, target));
                            actedThisTurn = true;
                            break;
                        }
                    }

                    if (actedThisTurn) continue;

                    // PASSATA 2 — avvicinarsi, ma senza uscire dal presidio.
                    foreach (SpezzoneRuntime target in targets)
                    {
                        HexCoordinates? targetCell = _turnManager.FindBestAdjacentCell(
                            police.PositionCell.Coordinates, target.PositionCell.Coordinates);

                        if (targetCell == null)
                        {
                            Debug.Log($"[AI] {police} has no free adjacent cell toward {target}");
                            continue;
                        }

                        if (!IsWithinLeash(police, targetCell.Value))
                        {
                            Debug.Log($"[AI] {police} will not leave its post to reach {target}");
                            continue;
                        }

                        bool moved = false;
                        yield return StartCoroutine(MoveTowards(police, targetCell.Value, r => moved = r));

                        if (moved) { actedThisTurn = true; break; }
                    }
                }

                if (!actedThisTurn)
                    Debug.Log($"[AI] {police} holds position: turn ended with {police.ActionPoints} AP left");
            }
        }
    }

    /// <summary>
    /// Distanza dal presidio: la minima fra le celle dell'obiettivo difeso.
    /// Senza obiettivo assegnato l'unità è sempre "in posizione", cioè libera di girare.
    /// </summary>
    private int DistanceFromPost(PoliceRuntime police, HexCoordinates coord)
    {
        ObjectiveRuntime post = police.GuardedObjective;
        if (post == null) return 0;

        int best = int.MaxValue;
        foreach (HexCell cell in post.Cells)
            best = Mathf.Min(best, coord.Distance(cell.Coordinates));

        return best;
    }

    private bool IsWithinLeash(PoliceRuntime police, HexCoordinates coord)
    {
        if (police.IsAlarmed) return true;
        if (police.EngagementRules == EngagementRules.Sweep) return true;
        if (police.GuardedObjective == null) return true;

        return DistanceFromPost(police, coord) <= police.LeashRadius;
    }

    /// <summary>La cella del presidio più vicina all'unità: dove torna quando sfora.</summary>
    private HexCoordinates NearestPostCell(PoliceRuntime police)
    {
        HexCoordinates from = police.PositionCell.Coordinates;
        HexCoordinates best = from;
        int bestDistance = int.MaxValue;

        foreach (HexCell cell in police.GuardedObjective.Cells)
        {
            int d = from.Distance(cell.Coordinates);
            if (d < bestDistance) { bestDistance = d; best = cell.Coordinates; }
        }

        return best;
    }

    /// <summary>
    /// Movimento verso una coordinata. Estratto perché lo usano sia il rientro al presidio
    /// sia l'avvicinamento, e duplicarlo significherebbe due timeout da tenere allineati.
    /// </summary>
    private IEnumerator MoveTowards(PoliceRuntime police, HexCoordinates destination, System.Action<bool> onResult)
    {
        List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(
            police.PositionCell.Coordinates, destination, _lvlManager.Map);

        if (pathCoords.Count <= 1)
        {
            Debug.Log($"[AI] {police} found no path to {destination}");
            onResult(false);
            yield break;
        }

        int maxSteps = Mathf.Min(police.ActionPoints, pathCoords.Count - 1);
        List<HexCell> path = new List<HexCell>();

        for (int i = 1; i <= maxSteps; i++)
        {
            if (!_lvlManager.Map.TryGetCell(pathCoords[i], out HexCell cell)) continue;

            // ⚠ Il percorso non deve portare fuori dal presidio: si tronca al primo passo
            // che sforerebbe, invece di rifiutare tutto il movimento.
            if (!IsWithinLeash(police, cell.Coordinates)) break;

            path.Add(cell);
        }

        if (path.Count == 0)
        {
            onResult(false);
            yield break;
        }

        bool finishMovement = false;
        bool success = _turnManager.ExecuteMovement(police, path, () => finishMovement = true);

        float elapsed = 0f;
        yield return new WaitUntil(() => finishMovement || (elapsed += Time.deltaTime) > 5f);
        if (!finishMovement) Debug.LogWarning($"[AI] {police} movement not completed: continuing");

        Debug.Log(success
            ? $"[AI] {police} moved {path.Count} cell(s) toward {destination}"
            : $"[AI] {police} movement failed");

        onResult(success);
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

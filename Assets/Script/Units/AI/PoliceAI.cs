using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoliceAI : MonoBehaviour
{

    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private UnitEventSO _onSelectedEvent;

    private TurnManager _turnManager;
    private bool _isValid;

    private void Awake()
    {
        _isValid = _lvlManager != null && _lvlManager.TurnManager != null;

        if (!_isValid)
        {
            Debug.LogError("[AI] PoliceAI has no LVLManager (or the manager has no TurnManager): the police will not act");
            return;
        }

        _turnManager = _lvlManager.TurnManager;
    }
    public IEnumerator ExecutePoliceActions()
    {
        // Il coordinatore del ciclo attende questa coroutine.
        // Se la configurazione è invalida deve terminare subito,
        // così il controllo può tornare al giocatore.
        if (!_isValid)
            yield break;

        foreach (var police in _lvlManager.Police)
        {
            if (!police.IsAlive) continue;

            _onSelectedEvent?.Raise(police);

            bool actedThisTurn = true;

            // ⚠ Antioscillazione. Senza, un poliziotto che non può vincere nessuno scontro
            // fa avanti-indietro fra due celle finché non finisce i PA: la passata 2 sceglie
            // la cella adiacente migliore, e da quella la migliore torna a essere la
            // precedente. Non risolve il problema vero (contro un muro che non batte in
            // mischia dovrebbe arretrare e caricare, che è un piano su due turni e richiede
            // memoria fra i turni), ma smette di sprecare il turno andando avanti e indietro.
            HashSet<HexCoordinates> visitedThisTurn = new() { police.PositionCell.Coordinates };

            while (actedThisTurn && police.ActionPoints > 0 && police.IsAlive)
            {
                actedThisTurn = false;

                // PASSATA 0 — fuori dal presidio: si torna al posto prima di ogni altra cosa.
                // È il "tenente" del GDD 8.4: non serve un richiamo, basta il guinzaglio.
                if (!IsWithinLeash(police, police.PositionCell.Coordinates))
                {
                    HexCoordinates? post = FindReachablePostCell(police);

                    if (post == null)
                    {
                        // Nessuna cella del presidio è raggiungibile. Non è un caso da
                        // ignorare in silenzio: o il presidio è murato, o gli sono stati
                        // assegnati più poliziotti di quante celle abbia. In entrambi i casi
                        // è un errore di dato del livello, e va detto.
                        Debug.LogWarning($"[AI] {police} cannot reach any cell within leash of " +
                               $"{police.GuardedObjective}: it stays out of position");
                        break;
                    }

                    bool moved = false;
                    yield return StartCoroutine(MoveTowards(police, post.Value, r => moved = r));

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
                        if (distance == 3
                            && _turnManager.CanCharge(police, target, out HexCell chargeCell)
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

                        if (visitedThisTurn.Contains(targetCell.Value)) continue;

                        bool moved = false;
                        yield return StartCoroutine(MoveTowards(police, targetCell.Value, r => moved = r));

                        if (moved)
                        {
                            visitedThisTurn.Add(police.PositionCell.Coordinates);
                            actedThisTurn = true;
                            break;
                        }
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

    /// <summary>
    /// Dove rientrare: la cella raggiungibile più vicina che soddisfa il **guinzaglio**.
    ///
    /// ⚠ NON una cella dell'obiettivo. Le due cose sembrano la stessa e non lo sono:
    /// `IsWithinLeash` dice "entro LeashRadius dall'edificio", che è un'area di decine di
    /// celle; puntare alle sole celle dell'edificio è un requisito molto più stretto di
    /// quello che la regola chiede. Con le celle dell'obiettivo occupate dai colleghi —
    /// 3 celle e 4 poliziotti, la situazione che il volantino produce — il rientro
    /// risultava impossibile pur essendo banale, e il log accusava i dati del livello.
    ///
    /// La BFS visita in ordine di distanza crescente, quindi la prima cella buona è già
    /// la migliore: una sola ricerca al posto di una A* per ogni cella dell'obiettivo.
    /// </summary>
    private HexCoordinates? FindReachablePostCell(PoliceRuntime police)
    {
        if (police.GuardedObjective == null) return null;

        HexCoordinates start = police.PositionCell.Coordinates;

        HashSet<HexCoordinates> seen = new() { start };
        Queue<HexCoordinates> queue = new();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            HexCoordinates current = queue.Dequeue();

            foreach (HexCoordinates dir in HexCoordinates.Directions)
            {
                HexCoordinates next = current + dir;
                if (!seen.Add(next)) continue;
                if (!_lvlManager.Map.TryGetCell(next, out HexCell cell)) continue;

                // Stesso filtro del PathFinter: se la BFS ci arriva, l'A* trova la strada.
                if (!TacticalQuery.IsCellAvailable(cell)) continue;

                if (IsWithinLeash(police, next)) return next;

                queue.Enqueue(next);
            }
        }

        return null;
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
        List<HexCell> path = new();

        // ⚠ Il guinzaglio serve a impedirti di ALLONTANARTI, non a dettarti la strada di
        // casa. Se sei già fuori raggio stai rientrando, e il percorso non va vincolato:
        // una deviazione attorno a un edificio ti allontana temporaneamente dal presidio,
        // ed è il caso NORMALE su una mappa con ostacoli, non l'eccezione.
        // Una versione precedente di questa guardia ammetteva solo i passi che avvicinavano,
        // e bastava un muro fra l'unità e il posto per rimetterla nel blocco permanente.
        bool returningToPost = !IsWithinLeash(police, police.PositionCell.Coordinates);

        for (int i = 1; i <= maxSteps; i++)
        {
            // break e non continue: saltare una cella spezzerebbe la continuità del percorso.
            if (!_lvlManager.Map.TryGetCell(pathCoords[i], out HexCell cell)) break;

            if (!returningToPost && !IsWithinLeash(police, cell.Coordinates)) break;

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
        List<SpezzoneRuntime> targets = new();

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
}

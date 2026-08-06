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

                SpezzoneRuntime nearestSpezzone = FoundNearestSpezzone(police);
                if (nearestSpezzone == null) break;

                int distance = police.PositionCell.Coordinates.Distance(nearestSpezzone.PositionCell.Coordinates);

                if (distance == 1)
                {
                    int atk = CombatResolver.GetEffectiveAtk(police, _lvlManager.Map);
                    int def = CombatResolver.GetEffectiveDef(nearestSpezzone, _lvlManager.Map);

                    if (atk <= def) break;

                    yield return StartCoroutine(_turnManager.ExecuteSkirmish(police, nearestSpezzone));
                    actedThisTurn = true;
                }
                else if (distance == 3 && _turnManager.CanCharge(police, nearestSpezzone, out _))
                {
                    yield return StartCoroutine(_turnManager.ExecuteCharge(police, nearestSpezzone));
                    actedThisTurn = true;
                }
                else
                {
                    Debug.Log($"Police a {police.PositionCell.Coordinates}, spezzone a {nearestSpezzone.PositionCell.Coordinates}, distanza: {distance}");

                    HexCoordinates? targetCell = _turnManager.FindBestAdjacentCell(police.PositionCell.Coordinates, nearestSpezzone.PositionCell.Coordinates);
                    if (targetCell == null)
                    {
                        Debug.Log($"Police a {police.PositionCell.Coordinates}: nessuna cella adiacente libera verso lo spezzone");
                        break;
                    }

                    List<HexCoordinates> pathCoords = _turnManager.PathFinder.FindPath(
                        police.PositionCell.Coordinates,
                        targetCell.Value,
                        _lvlManager.Map
                    );

                    if (pathCoords.Count <= 1)
                    {
                        Debug.Log($"Police a {police.PositionCell.Coordinates} non trova percorso verso lo spezzone");
                        break;
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
                    if (!finishMovement) Debug.LogWarning($"[IA]  {police} movement not complete, i continue"); actedThisTurn = success;
                
                    Debug.Log(success ? $"Police: Movement Accomplish ({path.Count} cells)" : "Police: Moviment fail");
                }
            }
        }
    }

    public SpezzoneRuntime FoundNearestSpezzone(PoliceRuntime police)
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

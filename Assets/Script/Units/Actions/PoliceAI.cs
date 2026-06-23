using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoliceAI : MonoBehaviour
{

    [SerializeField] private LVLManager _lvlManager;

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
            if (police.Status == UnitsStatus.Disperse) continue;

            bool actedThisTurn = true;

            while (actedThisTurn && police.ActionPoints > 0)
            {
                actedThisTurn = false;

                SpezzoneRuntime nearestSpezzone = FoundNearestSpezzone(police);
                if (nearestSpezzone == null) break;

                int distance = police.PositionCell.Coordinates.Distance(nearestSpezzone.PositionCell.Coordinates);

                if (distance == 1)
                {
                    yield return StartCoroutine(_turnManager.ExecuteSkirmish(police, nearestSpezzone));
                    actedThisTurn = true;
                }
                else if (distance == 3)
                {
                    actedThisTurn = _turnManager.ExecuteCharge(police, nearestSpezzone);
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
                   
                    yield return new WaitUntil(() => finishMovement);
                    actedThisTurn = success;
                
                    Debug.Log(success ? $"Police: Movimento riuscito ({path.Count} celle)" : "Police: Movimento fallito");
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
            if (spezzone.Status == UnitsStatus.Disperse) continue;
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

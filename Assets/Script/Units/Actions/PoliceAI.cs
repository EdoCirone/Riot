using UnityEngine;
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

    public void ExecutePoliceActions()
    {
        foreach (var police in _lvlManager.Police)
        {
            SpezzoneRuntime nearestSpezzone = FoundNearestSpezzone(police);
            if (nearestSpezzone == null) continue;
            int distance = police.PositionCell.Coordinates.Distance(nearestSpezzone.PositionCell.Coordinates);

            if (distance == 1)
            {
                Debug.Log($"Police attacca spezzone a distanza {distance}");
                _turnManager.ExecuteSkirmish(police, nearestSpezzone);
            }
            else if (distance == 3)
            {
                _turnManager.ExecuteCharge(police, nearestSpezzone);
            }
            else
            {
                HexCell moveCell = FindBestMoveCell(police, nearestSpezzone);
                if (moveCell == null)
                {
                    Debug.Log($"Police a {police.PositionCell.Coordinates} è circondata, nessuna mossa disponibile");
                    continue;
                }
                _turnManager.ExecuteMovement(police, new List<HexCell> { moveCell });
            }
            Debug.Log($"Police a {police.PositionCell.Coordinates}, spezzone a {nearestSpezzone.PositionCell.Coordinates}, distanza: {distance}");
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

    private HexCell FindBestMoveCell(PoliceRuntime police, SpezzoneRuntime target)
    {
        HexCoordinates[] neighbors = police.PositionCell.Coordinates.GetNeighbors();
        HexCell bestCell = null;
        int minDistance = int.MaxValue;
        foreach (var neighbor in neighbors)
        {
            if (!_lvlManager.Map.TryGetCell(neighbor, out HexCell cell)) continue;

            if (!cell.Type.IsWalkable) continue;

            if (cell.OccupiedBy != null) continue;

            int distance = neighbor.Distance(target.PositionCell.Coordinates);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestCell = cell;
            }


        }
        return bestCell;
    }
}

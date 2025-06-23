using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public static UnitController Instance;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Pathfinder pathfinder;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void CommandMove(List<CorteoUnit> units, Vector2Int destination)
    {
        foreach (var unit in units)
        {
            Vector2Int start = gridManager.WorldToGridCoordinates(unit.transform.position);
            Debug.Log($"🚶 {unit.name} si muove da {start} a {destination}");

            if (!gridManager.GetNodeAt(destination)?.isWalkable ?? true)
            {
                Debug.LogWarning($"🚫 Destinazione {destination} fuori griglia o bloccata.");
                continue;
            }

            List<Node> path = pathfinder.FindPath(start, destination);
            if (path == null)
            {
                Debug.LogWarning($"❌ Nessun path trovato da {start} a {destination}");
                continue;
            }

            if (unit.TryGetComponent(out UnitMover mover))
            {
                mover.FollowPath(path);
            }
        }
    }
}

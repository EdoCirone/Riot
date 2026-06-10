using System.Collections.Generic;
using UnityEngine;

public class PathFinderTest : MonoBehaviour
{
    [SerializeField] private HexGrid _grid;
    [SerializeField] private PathFinder _pathFinder;

    private void Start()
    {
        // coordinate hardcoded per il test
        HexCoordinates start = new HexCoordinates(0, 0);
        HexCoordinates end = new HexCoordinates(3, 0);

        List<HexCoordinates> path = _pathFinder.FindPath(start, end, _grid);

        foreach (HexCoordinates cell in path)
            Debug.Log(cell);
    }
}

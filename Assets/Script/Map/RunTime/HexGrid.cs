using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _widthHexs;
    [SerializeField] private int _heightHexs;
    [SerializeField] private float _cellSize;

    [Header("References")]
    [SerializeField] private HexTypeSO _defaultHexType;

    public float CellSize => _cellSize;

    Dictionary<HexCoordinates, HexCell> _cells = new Dictionary<HexCoordinates, HexCell>();

    private void Awake()
    {
        if (_defaultHexType == null) return;

        GenerateGrid(_defaultHexType);
    }


    public void GenerateGrid(HexTypeSO defaultType)
    {
        _cells.Clear();
        for (int col = 0; col < _widthHexs; col++)
        {
            int parity = col & 1;
            for (int row = 0; row < _heightHexs; row++)
            {
                int q = col;
                int r = row - (col - parity) / 2;
                HexCoordinates coords = new HexCoordinates(q, r);
                _cells[coords] = new HexCell(coords, defaultType);
            }
        }
    }
    public bool TryGetCell(HexCoordinates coords, out HexCell cell)
        => _cells.TryGetValue(coords, out cell);

    public IEnumerable<HexCell> GetAllCells() => _cells.Values;

}

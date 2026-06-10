using UnityEngine;

[CreateAssetMenu(fileName = "HexMapSO", menuName = "RIOT/Maps/HexMapSO")]
public class HexMapSO : ScriptableObject
{
    [Header("Map Info")]
    [SerializeField] private int _width;
    [SerializeField] private int _height;

    [SerializeField]private HexTypeSO[] _cells; 

    public int Width => _width;
    public int Height => _height;

    // if you change the size of the map, you need to call this method to reinitialize the cells array,
    //it's not a real problem because we use the editor to set the size of the map,
    //so we can call this method in the editor when we change the size of the map
    //But good to know that if you change the size of the map, you need to reinitialize the cells array
    public void Initialize( int width, int height, HexTypeSO defaultType )
    {
        _width = width;
        _height = height;
        _cells = new HexTypeSO[width * height];

        for (int i = 0; i < _cells.Length; i++)
            _cells[i] = defaultType;
    }

    public HexTypeSO GetCellType( int col, int row )
    {
        if (col < 0 || col >= _width || row < 0 || row >= _height)
            return null; // fuori dai limiti
        return _cells[row * _width + col];
    }

    public void SetCellType( int col, int row, HexTypeSO type )
    {
        if (col < 0 || col >= _width || row < 0 || row >= _height)
            return; // fuori dai limiti
        _cells[row * _width + col] = type;
    }
}

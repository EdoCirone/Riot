using UnityEngine;

//<summary>
//RunTime Class per le celle della griglia esagonale
//Contiene le coordinate assiali e il riferimento al tipo di esagono (HexTypeSO) che definisce le proprietà della cella
//</summary>

public class HexCell
{

    private  HexCoordinates _coordinates;
    private  HexTypeSO _type;

    public HexCoordinates Coordinates => _coordinates;
    public HexTypeSO Type => _type;

    public HexCell(HexCoordinates coordinates, HexTypeSO type)
    {

        _coordinates = coordinates;
        _type = type;
    }


}

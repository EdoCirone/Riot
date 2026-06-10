using UnityEngine;

/// <summary>
///Ho commentato molto questo script perchè è la prima volta che sviluppo un sistema di coordinate per griglia esagonale.
/// Coordinate assiali (q, r) per griglia esagonale flat-top.
/// s è implicito, ricavato dal vincolo q + r + s = 0.
/// Usata sia come posizione assoluta che come vettore di spostamento.
/// </summary>
[System.Serializable]
public struct HexCoordinates : System.IEquatable<HexCoordinates>
{
    public int Q;
    public int R;
    public int S => -Q - R; // terza coordinata cube, sempre derivata

    public HexCoordinates(int q, int r)
    {
        Q = q;
        R = r;
    }

    // Le 6 direzioni flat-top in coordinate axial.
    // Ordine: E, NE, NW, W, SW, SE
    public static readonly HexCoordinates[] Directions = new HexCoordinates[]
    {
        new HexCoordinates( 1,  0), // Est
        new HexCoordinates( 1, -1), // Nord-Est
        new HexCoordinates( 0, -1), // Nord-Ovest
        new HexCoordinates(-1,  0), // Ovest
        new HexCoordinates(-1,  1), // Sud-Ovest
        new HexCoordinates( 0,  1), // Sud-Est
    };

    // Somma di due coordinate — serve per GetNeighbors
    public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
        => new HexCoordinates(a.Q + b.Q, a.R + b.R);

    // Distanza in celle tra due coordinate (formula cube distance)
    // Usa Max perché ogni movimento cambia due coordinate contemporaneamente
    public int Distance(HexCoordinates other)
        => Mathf.Max(
            Mathf.Abs(Q - other.Q),
            Mathf.Abs(R - other.R),
            Mathf.Abs(S - other.S)
        );

    // Restituisce le 6 celle adiacenti a questa coordinata
    public HexCoordinates[] GetNeighbors()
    {
        HexCoordinates[] neighbors = new HexCoordinates[6];
        for (int i = 0; i < 6; i++)
            neighbors[i] = this + Directions[i];
        return neighbors;
    }

    public Vector3 ToWorldPosition(float cellSize)
    {
        // converto axial → offset oddq (inverso della generazione)
        int col = Q;
        int row = R + (Q - (Q & 1)) / 2;

        // proiezione offset oddq flat-top
        float x = cellSize * 1.5f * col;
        float y = cellSize * Mathf.Sqrt(3f) * (row + 0.5f * (col & 1));
        return new Vector3(x, y, 0f);
    }

    public override string ToString() => $"({Q}, {R}, {S})";

    public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;

    public override bool Equals(object obj)
        => obj is HexCoordinates other && Equals(other);

    public override int GetHashCode() => Q * 31 + R;

    public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);
    public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);
}
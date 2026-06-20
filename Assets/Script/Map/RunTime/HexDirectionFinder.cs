using UnityEngine;

public static class HexDirectionFinder
{
    // Trova quale delle 6 direzioni esagonali, applicata "distance" volte, porta da "from" a "to".
    // Restituisce null se le due celle non sono allineate su una direzione pura.
    public static HexCoordinates? FindDirection(HexCoordinates from, HexCoordinates to)
    {
        int distance = from.Distance(to);

        foreach (var dir in HexCoordinates.Directions)
        {
            HexCoordinates candidate = new HexCoordinates(
                from.Q + dir.Q * distance,
                from.R + dir.R * distance
            );

            if (candidate.Equals(to))
                return dir;
        }

        return null;
    }
}

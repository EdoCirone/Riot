/// <summary>
/// Condotta del presidio. Oggi si sceglie a mano nell'Inspector; domani la deciderà la
/// Tensione (GDD 8.6), e il codice dell'IA non cambierà — cambia solo chi scrive questo valore.
/// </summary>
public enum EngagementRules
{
    Containment,   // presidia, non inizia mai lo scontro
    Engage,        // ingaggia chi entra nel raggio di presidio
    Sweep          // esce a prendere il corteo: nessun guinzaglio, attacco a vista
}
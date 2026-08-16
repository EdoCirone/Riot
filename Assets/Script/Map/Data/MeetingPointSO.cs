using UnityEngine;

/// <summary>
/// Un punto di ritrovo: l'appuntamento che il volantino può dare (GDD 20.3). Decide da
/// quali celle parte il corteo, e la sua dimensione decide QUANTE unità si possono portare.
/// </summary>
[CreateAssetMenu(fileName = "MeetingPointSO", menuName = "RIOT/Maps/MeetingPointSO")]
public class MeetingPointSO : ScriptableObject
{
    [Header("Info")]
    [Tooltip("Il nome che comparirà sul volantino: \"ci vediamo in piazza delle Camelie\".")]
    [SerializeField] private string _displayName = "Piazza";

    [Header("Posizione")]
    [Tooltip("UNA cella qualsiasi del ritrovo. Le altre vengono raccolte per adiacenza " +
             "fra le celle dipinte come terreno di ritrovo.")]
    [SerializeField] private HexCoordinates _anchor;

    public string DisplayName => _displayName;
    public HexCoordinates Anchor => _anchor;

    public override string ToString()
        => string.IsNullOrEmpty(_displayName) ? name : _displayName;
}
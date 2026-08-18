using UnityEngine;

[CreateAssetMenu(fileName = "ObjectiveSO", menuName = "RIOT/Maps/ObjectiveSO")]
// This ScriptableObject represents an objective in the game.
// It contains information about the objective's display name, anchor position, points awarded for claiming it,
// and whether it requires simultaneous occupation of all cells.
//No Graphic representation is needed for this ScriptableObject, as it is used for data storage and logic purposes only.
public class ObjectiveSO : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string _displayName = "Obiettivo";

    [Header("Position")]
    [Tooltip("One Cell for Objective the others are chapted by adiacence.")]
    [SerializeField] private HexCoordinates _anchor;

    [Header("Rules")]
    [Tooltip("Secondary Objective points.")]
    [SerializeField] private int _points = 10;

    [Tooltip("If Active you have to Occupy every celL. If off you need n turn for n occupy cell")]
    [SerializeField] private bool _requiresSimultaneous = false;

    public string DisplayName => _displayName;
    public HexCoordinates Anchor => _anchor;
    public int Points => _points;
    public bool RequiresSimultaneous => _requiresSimultaneous;

    public override string ToString()
        => string.IsNullOrEmpty(_displayName) ? name : _displayName;
}
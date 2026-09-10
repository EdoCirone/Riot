using UnityEngine;

/// <summary>
/// Selezione confermata nel Volantino.
/// Conserva lo stato runtime condiviso tra Assemblea e livello.
/// </summary>
[CreateAssetMenu(
    fileName = "FlyerSelectionSO",
    menuName = "RIOT/Assembly/FlyerSelectionSO")]
public sealed class FlyerSelectionSO : ScriptableObject
{
    private const int MinutesPerDay = 24 * 60;

    [System.NonSerialized]
    private ObjectiveSO _declaredObjective;

    [System.NonSerialized]
    private MeetingPointSO _meetingPoint;

    [System.NonSerialized]
    private int _startMinutesFromMidnight = -1;

    public ObjectiveSO DeclaredObjective => _declaredObjective;
    public MeetingPointSO MeetingPoint => _meetingPoint;
    public int StartMinutesFromMidnight => _startMinutesFromMidnight;

    public bool HasSelection =>
        _declaredObjective != null
        && _meetingPoint != null
        && _startMinutesFromMidnight >= 0
        && _startMinutesFromMidnight < MinutesPerDay;

    public bool TrySetSelection(
        ObjectiveSO declaredObjective,
        MeetingPointSO meetingPoint,
        int startMinutesFromMidnight)
    {
        if (declaredObjective == null
            || meetingPoint == null
            || startMinutesFromMidnight < 0
            || startMinutesFromMidnight >= MinutesPerDay)
        {
            return false;
        }

        _declaredObjective = declaredObjective;
        _meetingPoint = meetingPoint;
        _startMinutesFromMidnight = startMinutesFromMidnight;

        return true;
    }

    public void ClearSelection()
    {
        _declaredObjective = null;
        _meetingPoint = null;
        _startMinutesFromMidnight = -1;
    }
}

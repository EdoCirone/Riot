using UnityEngine;

public sealed class FlyerSelectionController : MonoBehaviour
{
    [Header("Runtime state")]
    [SerializeField] private FlyerSelectionSO _selection;

    [Header("Map")]
    [SerializeField] private HexMapSO _mapData;

    private void Awake()
    {
        if (_selection != null)
            _selection.ClearSelection();
    }

    public bool TryConfirmFlyer(
        ObjectiveSO declaredObjective,
        MeetingPointSO meetingPoint,
        int startMinutesFromMidnight)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ASSEMBLY] The flyer can only be confirmed in Play Mode",
                this);
            return false;
        }

        if (_selection == null || _mapData == null)
        {
            Debug.LogError(
                "[ASSEMBLY] Selection state or map data not assigned",
                this);
            return false;
        }

        if (declaredObjective == null || meetingPoint == null)
        {
            Debug.LogError(
                "[ASSEMBLY] Objective or meeting point not selected",
                this);
            return false;
        }

        if (_mapData.Objectives == null
            || System.Array.IndexOf(
                _mapData.Objectives,
                declaredObjective) < 0)
        {
            Debug.LogError(
                $"[ASSEMBLY] Objective '{declaredObjective}' " +
                "is not available on this map",
                this);
            return false;
        }

        if (_mapData.MeetingPoints == null
            || System.Array.IndexOf(
                _mapData.MeetingPoints,
                meetingPoint) < 0)
        {
            Debug.LogError(
                $"[ASSEMBLY] Meeting point '{meetingPoint}' " +
                "is not available on this map",
                this);
            return false;
        }

        if (!FlyerTimeRules.IsValidStartTime(
                startMinutesFromMidnight,
                declaredObjective.DeadlineMinutesFromMidnight))
        {
            Debug.LogError(
                $"[ASSEMBLY] Start time " +
                $"{FormatTime(startMinutesFromMidnight)} is not valid " +
                $"for objective '{declaredObjective}'",
                this);
            return false;
        }

        if (!_selection.TrySetSelection(
                declaredObjective,
                meetingPoint,
                startMinutesFromMidnight))
        {
            Debug.LogError(
                "[ASSEMBLY] Flyer selection could not be saved",
                this);
            return false;
        }

        Debug.Log(
            $"[ASSEMBLY] Flyer confirmed: {declaredObjective}, " +
            $"{meetingPoint}, {FormatTime(startMinutesFromMidnight)}",
            this);

        return true;
    }

    private static string FormatTime(int minutesFromMidnight)
    {
        int hours = minutesFromMidnight / 60;
        int minutes = minutesFromMidnight % 60;

        return $"{hours:00}:{minutes:00}";
    }
}

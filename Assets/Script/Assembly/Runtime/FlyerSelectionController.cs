using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FlyerSelectionController : MonoBehaviour
{
    [Header("Runtime state")]
    [SerializeField] private FlyerSelectionSO _selection;

    [Header("Level")]
    [SerializeField] private HexMapSO _mapData;
    [SerializeField] private string _levelSceneName = "LVLTest";

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

        if (string.IsNullOrWhiteSpace(_levelSceneName)
            || !Application.CanStreamedLevelBeLoaded(_levelSceneName))
        {
            Debug.LogError(
                $"[ASSEMBLY] Scene '{_levelSceneName}' cannot be loaded",
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

        SceneManager.LoadScene(_levelSceneName);
        return true;
    }

    private static string FormatTime(int minutesFromMidnight)
    {
        int hours = minutesFromMidnight / 60;
        int minutes = minutesFromMidnight % 60;

        return $"{hours:00}:{minutes:00}";
    }
}

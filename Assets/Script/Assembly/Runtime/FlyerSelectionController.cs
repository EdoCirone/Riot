using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FlyerSelectionController : MonoBehaviour
{
    [Header("Runtime state")]
    [SerializeField] private FlyerSelectionSO _selection;

    [Header("MVP flyer")]
    [SerializeField] private ObjectiveSO _declaredObjective;
    [SerializeField] private MeetingPointSO _meetingPoint;

    [Range(0, 23)]
    [SerializeField] private int _startHour = 12;

    [Range(0, 59)]
    [SerializeField] private int _startMinute;

    [Header("Scene transition")]
    [SerializeField] private string _levelSceneName = "LVLTest";

    private void Awake()
    {
        if (_selection != null)
            _selection.ClearSelection();
    }

    [ContextMenu("Confirm flyer")]
    public void ConfirmFlyer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[ASSEMBLY] The flyer can only be confirmed in Play Mode",
                this);
            return;
        }

        if (_selection == null)
        {
            Debug.LogError(
                "[ASSEMBLY] Current flyer selection not assigned",
                this);
            return;
        }

        if (_startMinute % 15 != 0)
        {
            Debug.LogError(
                "[ASSEMBLY] Start time must use 15-minute intervals",
                this);
            return;
        }

        if (string.IsNullOrWhiteSpace(_levelSceneName)
            || !Application.CanStreamedLevelBeLoaded(_levelSceneName))
        {
            Debug.LogError(
                $"[ASSEMBLY] Scene '{_levelSceneName}' cannot be loaded",
                this);
            return;
        }

        int startMinutesFromMidnight =
            (_startHour * 60) + _startMinute;

        if (!_selection.TrySetSelection(
                _declaredObjective,
                _meetingPoint,
                startMinutesFromMidnight))
        {
            Debug.LogError(
                "[ASSEMBLY] Flyer incomplete or invalid",
                this);
            return;
        }

        Debug.Log(
            $"[ASSEMBLY] Flyer confirmed: {_declaredObjective}, " +
            $"{_meetingPoint}, {_startHour:00}:{_startMinute:00}");

        SceneManager.LoadScene(_levelSceneName);
    }
}

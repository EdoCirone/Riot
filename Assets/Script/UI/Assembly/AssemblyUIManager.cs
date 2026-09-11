using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class AssemblyUIManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private HexMapSO _mapData;

    [Header("Objective UI")]
    [SerializeField] private TMP_Dropdown _objectiveDropdown;

    [Header("Meeting Point UI")]
    [SerializeField] private TMP_Dropdown _meetingPointDropdown;

    [Header("Start Time UI")]
    [SerializeField] private TMP_Dropdown _startTimeDropdown;

    [Header("Flow")]
    [SerializeField] private FlyerSelectionController _selectionController;

    private readonly List<int> _startTimeOptions = new();

    public int SelectedStartMinutesFromMidnight { get; private set; } = -1;

    private readonly List<ObjectiveSO> _objectiveOptions = new();

    public ObjectiveSO SelectedObjective { get; private set; }

    private readonly List<MeetingPointSO> _meetingPointOptions = new();

    public MeetingPointSO SelectedMeetingPoint { get; private set; }

    private void Awake()
    {
        PopulateObjectiveDropdown();
        PopulateMeetingPointDropdown();
    }

    private void OnEnable()
    {
        if (_objectiveDropdown != null)
        {
            _objectiveDropdown.onValueChanged.AddListener(
                HandleObjectiveChanged);
        }
        if (_meetingPointDropdown != null)
        {
            _meetingPointDropdown.onValueChanged.AddListener(
                HandleMeetingPointChanged);
        }
        if (_startTimeDropdown != null)
        {
            _startTimeDropdown.onValueChanged.AddListener(
                HandleStartTimeChanged);
        }
    }

    private void OnDisable()
    {
        if (_objectiveDropdown != null)
        {
            _objectiveDropdown.onValueChanged.RemoveListener(
                HandleObjectiveChanged);
        }
        if (_meetingPointDropdown != null)
        {
            _meetingPointDropdown.onValueChanged.RemoveListener(
                HandleMeetingPointChanged);
        }
        if (_startTimeDropdown != null)
        {
            _startTimeDropdown.onValueChanged.RemoveListener(
                HandleStartTimeChanged);
        }
    }

    private void PopulateObjectiveDropdown()
    {
        if (_mapData == null || _objectiveDropdown == null)
        {
            Debug.LogError(
                "[ASSEMBLY UI] Map data or objective dropdown not assigned",
                this);
            return;
        }

        _objectiveOptions.Clear();
        _objectiveDropdown.ClearOptions();

        var labels = new List<string>();

        foreach (ObjectiveSO objective in _mapData.Objectives)
        {
            if (objective == null)
                continue;

            _objectiveOptions.Add(objective);
            labels.Add(objective.ToString());
        }

        if (_objectiveOptions.Count == 0)
        {
            Debug.LogError(
                "[ASSEMBLY UI] The map has no valid objectives",
                this);
            return;
        }

        _objectiveDropdown.AddOptions(labels);
        _objectiveDropdown.SetValueWithoutNotify(0);
        _objectiveDropdown.RefreshShownValue();

        HandleObjectiveChanged(0);
    }

    private void HandleObjectiveChanged(int index)
    {
        if (index < 0 || index >= _objectiveOptions.Count)
        {
            SelectedObjective = null;
            return;
        }

        SelectedObjective = _objectiveOptions[index];

        PopulateStartTimeDropdown();

        Debug.Log(
            $"[ASSEMBLY UI] Objective selected: {SelectedObjective}",
            this);
    }
    private void PopulateMeetingPointDropdown()
    {
        if (_mapData == null || _meetingPointDropdown == null)
        {
            Debug.LogError(
                "[ASSEMBLY UI] Map data or meeting point dropdown not assigned",
                this);
            return;
        }

        _meetingPointOptions.Clear();
        _meetingPointDropdown.ClearOptions();

        var labels = new List<string>();

        foreach (MeetingPointSO meetingPoint in _mapData.MeetingPoints)
        {
            if (meetingPoint == null)
                continue;

            _meetingPointOptions.Add(meetingPoint);
            labels.Add(meetingPoint.ToString());
        }

        if (_meetingPointOptions.Count == 0)
        {
            Debug.LogError(
                "[ASSEMBLY UI] The map has no valid meeting points",
                this);
            return;
        }

        _meetingPointDropdown.AddOptions(labels);
        _meetingPointDropdown.SetValueWithoutNotify(0);
        _meetingPointDropdown.RefreshShownValue();

        HandleMeetingPointChanged(0);
    }

    private void HandleMeetingPointChanged(int index)
    {
        if (index < 0 || index >= _meetingPointOptions.Count)
        {
            SelectedMeetingPoint = null;
            return;
        }

        SelectedMeetingPoint = _meetingPointOptions[index];

        Debug.Log(
            $"[ASSEMBLY UI] Meeting point selected: {SelectedMeetingPoint}",
            this);
    }

    private void PopulateStartTimeDropdown()
    {
        if (_startTimeDropdown == null || SelectedObjective == null)
        {
            Debug.LogError(
                "[ASSEMBLY UI] Start time dropdown or objective not available",
                this);
            return;
        }

        int previousSelection = SelectedStartMinutesFromMidnight;
        int deadline = SelectedObjective.DeadlineMinutesFromMidnight;
        int latestStart =
            FlyerTimeRules.CalculateLatestStartMinutes(deadline);

        _startTimeOptions.Clear();
        _startTimeDropdown.ClearOptions();

        var labels = new List<string>();

        for (int minutes = FlyerTimeRules.EarliestStartMinutes;
                 minutes <= latestStart;
                 minutes += FlyerTimeRules.TimeStepMinutes)
        {
            _startTimeOptions.Add(minutes);
            labels.Add(FormatTime(minutes));
        }

        if (_startTimeOptions.Count == 0)
        {
            SelectedStartMinutesFromMidnight = -1;

            Debug.LogError(
                        $"[ASSEMBLY UI] Objective '{SelectedObjective}' does not leave " +
                        $"at least {FlyerTimeRules.MinimumTimeBeforeDeadlineMinutes} minutes after " +
                        $"{FormatTime(FlyerTimeRules.EarliestStartMinutes)}",
                        this);
            return;
        }

        _startTimeDropdown.AddOptions(labels);

        int selectedIndex = _startTimeOptions.IndexOf(previousSelection);

        if (selectedIndex < 0)
            selectedIndex = 0;

        _startTimeDropdown.SetValueWithoutNotify(selectedIndex);
        _startTimeDropdown.RefreshShownValue();

        HandleStartTimeChanged(selectedIndex);
    }

    private void HandleStartTimeChanged(int index)
    {
        if (index < 0 || index >= _startTimeOptions.Count)
        {
            SelectedStartMinutesFromMidnight = -1;
            return;
        }

        SelectedStartMinutesFromMidnight = _startTimeOptions[index];

        Debug.Log(
            $"[ASSEMBLY UI] Start time selected: " +
            $"{FormatTime(SelectedStartMinutesFromMidnight)}",
            this);
    }

    private static string FormatTime(int minutesFromMidnight)
    {
        int hours = minutesFromMidnight / 60;
        int minutes = minutesFromMidnight % 60;

        return $"{hours:00}:{minutes:00}";
    }

    public void ConfirmSelection()
    {
        if (_selectionController == null)
        {
            Debug.LogError(
                "[ASSEMBLY UI] Flyer selection controller not assigned",
                this);
            return;
        }

        _selectionController.TryConfirmFlyer(
            SelectedObjective,
            SelectedMeetingPoint,
            SelectedStartMinutesFromMidnight);
    }
}

using TMPro;
using UnityEngine;

public class ObjectiveHUDView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private TextMeshProUGUI _objectiveText;

    private void Start()
    {
        if (_lvlManager == null || _objectiveText == null)
        {
            Debug.LogWarning("Reference missing in ObjectiveHUDView", this);
            enabled = false;
            return;
        }

        Refresh();
    }

    public void Refresh()
    {
        ObjectiveSO objective = _lvlManager.DeclaredObjectiveData;

        if (objective == null)
        {
            _objectiveText.text = "OBIETTIVO NON DISPONIBILE";
            return;
        }

        int deadline = objective.DeadlineMinutesFromMidnight;
        int hour = (deadline / 60) % 24;
        int minute = deadline % 60;

        _objectiveText.text =
            $"OBIETTIVO\n" +
            $"{objective.DisplayName}\n" +
            $"Occupazione entro le {hour:00}:{minute:00}";
    }
}

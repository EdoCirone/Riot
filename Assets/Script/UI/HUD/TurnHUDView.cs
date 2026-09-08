using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnHUDView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;

    [Header("UI Elements")]
    [SerializeField] private Image _clockFace;
    [SerializeField] private RectTransform _hourHand;
    [SerializeField] private RectTransform _minuteHand;
    [SerializeField] private TextMeshProUGUI _digitalTimeText;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _deadlineColor = Color.red;

    [Header("Events")]
    [SerializeField] private GameEventSO _startPlayerTurnEvent;

    private bool _isValid;

    private void Awake()
    {
        if (_lvlManager == null
            || _clockFace == null
            || _hourHand == null
            || _minuteHand == null
            || _digitalTimeText == null
            || _startPlayerTurnEvent == null)
        {
            Debug.LogWarning("Reference missing in TurnHUDView");
            return;
        }
        _isValid = true;
    }

    private void OnEnable()
    {
        if (!_isValid) return;

        _startPlayerTurnEvent.Subscribe(Refresh);
        Refresh();
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _startPlayerTurnEvent.Unsubscribe(Refresh);
    }

    private void Refresh()
    {
        int totalMinutes = _lvlManager.CurrentTimeMinutes;
        int hour = (totalMinutes / 60) % 24;
        int minute = totalMinutes % 60;

        float minuteAngle = -minute * 6f;
        float hourAngle = -(totalMinutes % 720) * 0.5f;

        _minuteHand.localRotation = Quaternion.Euler(0f, 0f, minuteAngle);
        _hourHand.localRotation = Quaternion.Euler(0f, 0f, hourAngle);

        _digitalTimeText.text = $"{hour:00}:{minute:00}";

        _clockFace.color = _lvlManager.IsDeadlineRound
            ? _deadlineColor
            : _normalColor;

        _digitalTimeText.color = _lvlManager.IsDeadlineRound
            ? _deadlineColor
            : _normalColor;
    }
}

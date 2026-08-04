using TMPro;
using UnityEngine;

public class TurnHUDView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private TextMeshProUGUI _turnText;

    [Header("Events")]
    [SerializeField] private GameEventSO _startPlayerTurnEvent;

    private bool _isValid;

    private void Awake()
    {
        if (_lvlManager == null || _turnText == null || _startPlayerTurnEvent == null)
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
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _startPlayerTurnEvent.Unsubscribe(Refresh);
    }

    private void Refresh()
    {
        int t = _lvlManager.CurrentTurn;
        string color = t <= 2 ? "#FF5555" : t <= 4 ? "#FFCC55" : "#FFFFFF";
        _turnText.text = $"TURN <color={color}>{t}</color>";
    }
}
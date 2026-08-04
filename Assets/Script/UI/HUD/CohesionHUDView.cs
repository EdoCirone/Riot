using TMPro;
using UnityEngine;

public class CohesionHUDView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private TextMeshProUGUI _cohesionText;

    [Header("Events")]
    [SerializeField] private GameEventSO _boardChangedEvent;

    private bool _isValid;

    private void Awake()
    {
        if (_lvlManager == null || _cohesionText == null || _boardChangedEvent == null)
        {
            Debug.LogWarning("Reference missing in CohesionHUDView");
            return;
        }
        _isValid = true;
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _boardChangedEvent.Subscribe(Refresh);
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _boardChangedEvent.Unsubscribe(Refresh);
    }

    private void Start()
    {
        if (_isValid) Refresh();
    }

    private void Refresh()
    {
        int c = _lvlManager.Cohesion;
        string color = c == 0 ? "#FF5555" : c < 40 ? "#FFCC55" : "#7CFF7C";
        _cohesionText.text = $"COESIONE <color={color}>{c}</color>";
    }
}
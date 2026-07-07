using UnityEngine;
using TMPro;
using DG.Tweening;

public class AlertPanelView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private StringEventSO _alertEvent;
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeDuration = 0.3f;

    private bool _isValid;
    private Tween _hideTween;

    private void Awake()
    {
        if (_canvasGroup == null || _messageText == null || _alertEvent == null)
        {
            Debug.LogWarning("Refs missing in AlertPanelView");
            return;
        }
        _isValid = true;
        _canvasGroup.alpha = 0f;   
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _alertEvent.Subscribe(ShowMessage);
    }
    private void OnDisable()
    {
        if (!_isValid) return;
        _alertEvent.Unsubscribe(ShowMessage);  
    }

    private void ShowMessage(string message)
    {
        _hideTween?.Kill();             
        _messageText.text = message;
        _canvasGroup.alpha = 1f;         

        // dopo displayDuration, svanisci
        _hideTween = DOVirtual.DelayedCall(_displayDuration, () =>
        {
            _canvasGroup.DOFade(0f, _fadeDuration);
        });
    }
}
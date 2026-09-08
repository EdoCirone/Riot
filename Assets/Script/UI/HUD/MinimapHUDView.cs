using DG.Tweening;
using UnityEngine;

public class MinimapHUDView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _minimapContent;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Camera _minimapCamera;

    [Header("State")]
    [SerializeField] private bool _startsOpen;

    [Header("Animation")]
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private float _closedScale = 0.85f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InCubic;

    private bool _isOpen;
    private Sequence _sequence;

    private void Awake()
    {
        if (_minimapContent == null
            || _canvasGroup == null
            || _minimapCamera == null)
        {
            Debug.LogWarning("Reference missing in MinimapHUDView", this);
            enabled = false;
            return;
        }

        SetImmediate(_startsOpen);
    }

    private void OnDisable()
    {
        _sequence?.Kill();
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (_isOpen) return;

        _sequence?.Kill();
        _isOpen = true;

        _minimapContent.gameObject.SetActive(true);
        _minimapCamera.enabled = true;

        _minimapContent.localScale = Vector3.one * _closedScale;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(_minimapContent.DOScale(1f, _duration).SetEase(_openEase))
            .Join(_canvasGroup.DOFade(1f, _duration))
            .OnComplete(() => _canvasGroup.interactable = true);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _sequence?.Kill();
        _isOpen = false;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(_minimapContent.DOScale(_closedScale, _duration).SetEase(_closeEase))
            .Join(_canvasGroup.DOFade(0f, _duration))
            .OnComplete(() =>
            {
                if (_isOpen) return;

                _minimapContent.gameObject.SetActive(false);
                _minimapCamera.enabled = false;
            });
    }

    private void SetImmediate(bool isOpen)
    {
        _isOpen = isOpen;
        _sequence?.Kill();

        _minimapContent.gameObject.SetActive(isOpen);
        _minimapContent.localScale = isOpen
            ? Vector3.one
            : Vector3.one * _closedScale;

        _canvasGroup.alpha = isOpen ? 1f : 0f;
        _canvasGroup.interactable = isOpen;
        _canvasGroup.blocksRaycasts = isOpen;
        _minimapCamera.enabled = isOpen;
    }
}

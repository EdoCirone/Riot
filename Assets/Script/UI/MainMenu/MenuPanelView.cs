using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class MenuPanelView : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private RectTransform _panelRect;
    [SerializeField] private CanvasGroup _panelGroup;

    [Header("Slide Settings")]
    [SerializeField] private float _hiddenOffsetX = 600f;
    [SerializeField] private float _showDuration = 0.5f;
    [SerializeField] private float _hideDuration = 0.5f;
    [SerializeField] private Ease _showEase = Ease.OutCubic;
    [SerializeField] private Ease _hideEase = Ease.InCubic;

    private Vector2 _showPosition;
    private Vector2 _hiddenPosition;
    private Tween _slideTween;

    private void Awake()
    {

        if (_panelRect == null)
            _panelRect = GetComponent<RectTransform>();

        if (_panelGroup == null)
            _panelGroup = GetComponent<CanvasGroup>();

        if (_panelGroup == null || _panelRect == null)
        {
            Debug.LogError("MenuPanelView requires both a CanvasGroup or a RectTransform component.");
            return;
        }


        _showPosition = _panelRect.anchoredPosition;
        _hiddenPosition = _showPosition + Vector2.right * _hiddenOffsetX;

        _panelRect.anchoredPosition = _hiddenPosition;
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;

    }

    private void OnDisable()
    {
        _slideTween?.Kill();
        _slideTween = null;
    }

    public void Show()
    {
        Slide(true);
    }

    public void Hide()
    {
        Slide(false);
    }

    private void Slide(bool visible)
    {
        if (_panelRect == null || _panelGroup == null)
            return;

        _slideTween?.Kill();

        _panelGroup.interactable = visible;
        _panelGroup.blocksRaycasts = visible;

        _slideTween = _panelRect.DOAnchorPos(visible ? _showPosition : _hiddenPosition, visible ? _showDuration : _hideDuration)
            .SetEase(visible ? _showEase : _hideEase)
            .OnComplete(() =>
            {
                _slideTween = null;
            });
    }
}

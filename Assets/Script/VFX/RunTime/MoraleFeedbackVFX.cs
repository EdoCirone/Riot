using UnityEngine;
using DG.Tweening;
using TMPro;

public class MoraleFeedbackVFX : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup feedbackCanvasGroup;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private RectTransform feedbackRectTransform;

    [Header("Grid Settings")]
    [SerializeField] private float _cellSize = 1.0f;

    [Header("Animation Settings")]
    [SerializeField] private float floatHeight = 2.0f;
    [SerializeField] private float floatDuration = 1.2f;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float verticalOffset = 1.5f;

    [Header("Visual Settings")]
    [SerializeField] private Color gainColor = Color.green;
    [SerializeField] private Color lossColor = Color.red;
    [SerializeField] private float scaleMultiplier = 1.3f;

    [Header("Audio")]
    [SerializeField] private StringEventSO sfxEvent;
    [SerializeField] private string gainSfx = "MoraleGain";
    [SerializeField] private string lossSfx = "MoraleLoss";

    private Sequence _currentSequence;
    private Camera _mainCamera;
    private System.Action _onCompleteCallback;
    private Canvas _parentCanvas;
    private Vector3 _initialLocalPosition;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _parentCanvas = GetComponentInParent<Canvas>();
        _initialLocalPosition = feedbackRectTransform.localPosition;

        if (_parentCanvas == null)
            Debug.LogWarning("[MoraleFeedbackVFX] No Canvas found in parent!");
    }

    public void ShowMoraleFeedback(MoraleChangeData data, System.Action onComplete = null)
    {
        _onCompleteCallback = onComplete;

        ConfigureFeedback(data);
        PositionFeedback(data.Unit);
        AnimateFeedback();
        PlayMoraleSFX(data.IsGain);
    }

    private void ConfigureFeedback(MoraleChangeData data)
    {
        string sign = data.IsGain ? "+" : "-";
        Color color = data.IsGain ? gainColor : lossColor;

        feedbackText.text = $"{sign}{data.Amount}";
        feedbackText.color = color;

        feedbackCanvasGroup.alpha = 1f;
        feedbackRectTransform.localScale = Vector3.one;
        feedbackRectTransform.localPosition = _initialLocalPosition;
    }

    private void PositionFeedback(AbstractUnitsRunTime unit)
    {
        if (unit?.PositionCell == null)
        {
            Debug.LogWarning("[MoraleFeedbackVFX] Unit or PositionCell is null!");
            return;
        }

        Vector3 worldPos = GetCellWorldPosition(unit.PositionCell);
        worldPos.y += verticalOffset; 

        if (_parentCanvas == null) return;

        if (_parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ||
            _parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (_mainCamera == null) return;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            feedbackRectTransform.position = screenPos;
        }
        else // WorldSpace
        {
            feedbackRectTransform.position = worldPos;
        }
    }

    private Vector3 GetCellWorldPosition(HexCell cell)
    {
        if (cell == null) return Vector3.zero;
        return cell.Coordinates.ToWorldPosition(_cellSize);
    }

    private void AnimateFeedback()
    {
        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();

        bool isWorldSpace = _parentCanvas != null &&
                           _parentCanvas.renderMode == RenderMode.WorldSpace;

        _currentSequence.Append(feedbackRectTransform.DOScale(scaleMultiplier, 0.2f)
            .SetEase(Ease.OutBack));

        if (isWorldSpace)
        {
            Vector3 startPos = feedbackRectTransform.position;
            Vector3 targetPos = startPos + Vector3.up * floatHeight;

            _currentSequence.Append(feedbackRectTransform.DOMove(targetPos, floatDuration)
                .SetEase(Ease.OutQuad));
        }
        else
        {
            _currentSequence.Append(feedbackRectTransform.DOAnchorPosY(
                feedbackRectTransform.anchoredPosition.y + floatHeight,
                floatDuration
            ).SetEase(Ease.OutQuad));
        }

        _currentSequence.Join(feedbackCanvasGroup.DOFade(0f, fadeDuration)
            .SetDelay(Mathf.Max(0, floatDuration - fadeDuration)));


        _currentSequence.Insert(floatDuration * 0.5f,
            feedbackRectTransform.DOScale(1f, 0.3f));

       
        _currentSequence.OnComplete(() => {
            _onCompleteCallback?.Invoke();
        });
    }

    private void PlayMoraleSFX(bool isGain)
    {
        if (sfxEvent == null) return;
        string sfxName = isGain ? gainSfx : lossSfx;
        sfxEvent.Raise(sfxName);
    }
}
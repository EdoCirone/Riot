using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatsPanelView : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject _panelRoot;   
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Avatar")]
    [SerializeField] private Image _avatarImage;

    [Header("Morale")]
    [SerializeField] private Slider _morBar;
    [SerializeField] private TextMeshProUGUI _morValueText;

    [Header("Action Points")]
    [SerializeField] private Slider _aptBar;
    [SerializeField] private TextMeshProUGUI _aptValueText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI _atkText;
    [SerializeField] private TextMeshProUGUI _defText;

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private GameEventSO _endPlayerTurnEvent;
    [SerializeField] private GameEventSO _startPlayerTurnEvent;

    

    private AbstractUnitsRunTime _currentUnit;
    private bool _isValid = false;

    private void Awake()
    {
        if (_unitSelectedEvent == null || _unitDeselectedEvent == null || _startPlayerTurnEvent == null || _endPlayerTurnEvent == null)
        {
            Debug.LogWarning("Events missing in UnitStatsPanelView");
            return;
        }
        if (_panelRoot == null)
        {
            Debug.LogWarning("Panel Root missing in UnitStatsPanelView");
            return;
        }
        _isValid = true;
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;   
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Subscribe(Show);
        _unitDeselectedEvent.Subscribe(Hide);
        _startPlayerTurnEvent.Subscribe(Hide);   
        _endPlayerTurnEvent.Subscribe(Hide);   
    }
    private void OnDisable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Unsubscribe(Show);
        _unitDeselectedEvent.Unsubscribe(Hide);
        _startPlayerTurnEvent.Unsubscribe(Hide);
        _endPlayerTurnEvent.Unsubscribe(Hide);
    }

    private void Show(AbstractUnitsRunTime unit)   
    {
        if (unit == null) return;
        _currentUnit = unit;
        Refresh();
        if (_canvasGroup.alpha >= 1f) return;  
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(1, 0.2f);
        _panelRoot.transform.DOScale(1f, 0.2f).From(0.9f);  
    }

    private void Hide()   
    {
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.DOFade(0, 0.15f);
        _currentUnit = null;
    }

    public void Refresh()
    {
        if (_currentUnit == null) return;

        _morBar.value = _currentUnit.MaxMorale > 0
            ? (float)_currentUnit.Morale / _currentUnit.MaxMorale
            : 0f;
        _morValueText.text = _currentUnit.Morale.ToString();

        _aptBar.value = _currentUnit.MaxActionPoints > 0
            ? (float)_currentUnit.ActionPoints / _currentUnit.MaxActionPoints
            : 0f;
        _aptValueText.text = _currentUnit.ActionPoints.ToString();

        _atkText.text = $"{_currentUnit.Atk}";
        _defText.text = $"{_currentUnit.Def}";
    }
}
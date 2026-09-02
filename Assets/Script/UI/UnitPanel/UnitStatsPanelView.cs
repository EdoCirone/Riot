using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UnitStatsPanelView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private LVLManager _lvlManager;

    [Header("Panel Root")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Avatar")]
    [SerializeField] private Image _avatarImage;

    [Header("Morale")]
    [SerializeField] private Slider _morBar;
    [SerializeField] private TextMeshProUGUI _morValueText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI _atkText;
    [SerializeField] private TextMeshProUGUI _defText;

    [Header("Action Points")]
    [SerializeField] private Slider _aptBar;
    [SerializeField] private TextMeshProUGUI _aptValueText;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _alarmTurnsText;

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private GameEventSO _endPlayerTurnEvent;
    [SerializeField] private GameEventSO _startPlayerTurnEvent;

    private AbstractUnitsRunTime _currentUnit;
    private bool _isValid = false;

    private void Awake()
    {
        if (_lvlManager == null)
        {
            Debug.LogWarning("LVLManager missing in UnitStatsPanelView");
            return;
        }

        if (_unitSelectedEvent == null
            || _unitDeselectedEvent == null
            || _startPlayerTurnEvent == null
            || _endPlayerTurnEvent == null)
        {
            Debug.LogWarning("Events missing in UnitStatsPanelView");
            return;
        }
        if (_panelRoot == null)
        {
            Debug.LogWarning("Panel Root missing in UnitStatsPanelView");
            return;
        }
        _avatarImage.preserveAspect = true;
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

        _avatarImage.sprite = _currentUnit.Avatar;

        _morBar.value = _currentUnit.MaxMorale > 0 ? (float)_currentUnit.Morale / _currentUnit.MaxMorale : 0f;

        if (_aptBar != null)
        {
            _aptBar.value = _currentUnit.MaxActionPoints > 0
                ? (float)_currentUnit.ActionPoints / _currentUnit.MaxActionPoints
                : 0f;
        }
        if (_aptValueText != null)
            _aptValueText.text = _currentUnit.ActionPoints.ToString();

        if (_statusText != null)
            _statusText.text = DescribeStatus(_currentUnit);

        if (_alarmTurnsText != null)
            _alarmTurnsText.text = DescribeAlarm(_currentUnit);

        TacticalQuery.AuraBonus aura = TacticalQuery.GetAuraBonus(_currentUnit, _lvlManager.Map);

        _atkText.text = FormatStat(_currentUnit.Atk, aura.Atk);
        _defText.text = FormatStat(_currentUnit.Def, aura.Def);
        _morValueText.text = FormatStat(_currentUnit.BaseMorale, aura.Mor);
    }

    private string FormatStat(int baseValue, int auraValue)
    {
        if (auraValue == 0) return baseValue.ToString();
        string sign = auraValue > 0 ? "+" : "";
        return $"{baseValue} <color=#7CFF7C>({sign}{auraValue})</color>";
    }
    private static string DescribeStatus(AbstractUnitsRunTime unit)
    {
        if (unit.IsPanicked)
            return $"PANICKED — {unit.PanicTurnsLeft} turn(s)";

        if (unit.IsSeated)
            return "SEATED — can only stand up or chant";

        return "";
    }
    private static string DescribeAlarm(AbstractUnitsRunTime unit)
    {
        if (unit is not PoliceRuntime police || !police.IsAlarmed)
            return "";

        string turns = police.AlarmTurnsLeft == 1 ? "TURN" : "TURNS";
        return $"ALARM — {police.AlarmTurnsLeft} {turns}";
    }
}

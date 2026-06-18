using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitStatsPanelView : MonoBehaviour
{
    [Header("Avatar")]
    [SerializeField] private Image _avatarImage;

    [Header("Morale")]
    [SerializeField] private Slider _morBar;
    [SerializeField] private TextMeshProUGUI _morValueText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI _atkText;
    [SerializeField] private TextMeshProUGUI _defText;
    [SerializeField] private TextMeshProUGUI _aptText;

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;

    private AbstractUnitsRunTime _currentUnit;
    private bool _isValid = false;

    private void Awake()
    {
        if (_unitSelectedEvent == null || _unitDeselectedEvent == null)
        {
            Debug.LogWarning("Events missing in UnitStatsPanelView");
            return;
        }
        _isValid = true;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Subscribe(OnUnitSelected);
        _unitDeselectedEvent.Subscribe(OnUnitDeselected);
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        _currentUnit = unit;
        gameObject.SetActive(true);
        Refresh();
    }

    private void OnUnitDeselected()
    {
        _currentUnit = null;
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (_currentUnit == null) return;

        _morBar.value = _currentUnit.MaxMorale > 0
            ? (float)_currentUnit.Morale / _currentUnit.MaxMorale
            : 0f;
        _morValueText.text = _currentUnit.Morale.ToString();
        _atkText.text = $"ATK: {_currentUnit.Atk}";
        _defText.text = $"DEF: {_currentUnit.Def}";
        _aptText.text = $"APT: {_currentUnit.ActionPoints}";
    }
}
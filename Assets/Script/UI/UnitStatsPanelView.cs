using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitStatsPanelView : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject _panelRoot;   // UnitSelectedPanel, l'oggetto da mostrare/nascondere

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
    [SerializeField] private GameEventSO _endTurnEvent;
    

    private AbstractUnitsRunTime _currentUnit;
    private bool _isValid = false;

    private void Awake()
    {
        if (_unitSelectedEvent == null || _unitDeselectedEvent == null || _endTurnEvent == null)
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
        _panelRoot.SetActive(false);   // spengo il PANEL, non me stesso
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Subscribe(OnUnitSelected);
        _unitDeselectedEvent.Subscribe(OnUnitDeselected);
        _endTurnEvent.Subscribe(OnEndTurn);
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
        _endTurnEvent.Unsubscribe(OnEndTurn);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        _currentUnit = unit;
        _panelRoot.SetActive(true);
        Refresh();
    }

    private void OnUnitDeselected()
    {
        _currentUnit = null;
        _panelRoot.SetActive(false);
    }

    private void OnEndTurn()
    {
        _currentUnit = null;
        _panelRoot.SetActive(false);

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
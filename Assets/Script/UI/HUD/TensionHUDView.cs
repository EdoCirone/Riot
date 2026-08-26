using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TensionHUDView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LVLManager _lvlManager;
    [SerializeField] private TextMeshProUGUI _tensionText;
    [SerializeField] private Image _fillImage;

    [Header("Band colors")]
    [SerializeField]
    private Color _containmentColor = new Color32(70, 150, 255, 255);

    [SerializeField]
    private Color _engageColor = new Color32(255, 190, 60, 255);

    [SerializeField]
    private Color _sweepColor = new Color32(255, 65, 95, 255);

    [Header("Events")]
    [SerializeField] private GameEventSO _tensionChangedEvent;

    private bool _isValid;

    private void Awake()
    {
        if (_lvlManager == null
            || _tensionText == null
            || _fillImage == null
            || _tensionChangedEvent == null)
        {
            Debug.LogWarning("Reference missing in TensionHUDView");

            return;
        }

        _isValid = true;
    }

    private void OnEnable()
    {
        if (!_isValid)
            return;

        _tensionChangedEvent.Subscribe(Refresh);
    }

    private void OnDisable()
    {
        if (!_isValid)
            return;

        _tensionChangedEvent.Unsubscribe(Refresh);
    }

    private void Start()
    {
        if (_isValid)
            Refresh();
    }

    private void Refresh()
    {
        int value = _lvlManager.CurrentTension;

        _tensionText.text = $"TENSIONE {value} / {TensionRules.MaxValue}";

        _fillImage.fillAmount = value / (float)TensionRules.MaxValue;

        _fillImage.color = GetBandColor(value);
    }

    private Color GetBandColor(int tension)
    {
        return TensionRules.GetEngagementRules(tension) switch
        {
            EngagementRules.Containment =>
                _containmentColor,

            EngagementRules.Engage =>
                _engageColor,

            EngagementRules.Sweep =>
                _sweepColor,

            _ => _containmentColor
        };
    }
}

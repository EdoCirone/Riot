using UnityEngine;

public class InGamePanelManager : MonoBehaviour
{
    [Header("Panels Reference")]
    [SerializeField] GameObject _losePanel;
    [SerializeField] GameObject _winPanel;
    [SerializeField] GameObject _menuPanel;
    [SerializeField] GameObject _optionPanel;
    [SerializeField] private MinimapHUDView _minimapHUDView;

    [Header("Input")]
    [SerializeField] private GameplayInputGate _inputGate;

    [Header("Events")]
    [SerializeField] GameEventSO _loseEvent;
    [SerializeField] GameEventSO _winEvent;

    private bool _isValid = false;

    private void Awake()
    {
        if (_losePanel == null
         || _winPanel == null
         || _menuPanel == null
         || _optionPanel == null
         || _minimapHUDView == null
         || _inputGate == null)
        {
            Debug.LogWarning("Reference missing in InGamePanelManager", this);
            return;
        }

        if (_loseEvent == null || _winEvent == null)
        {
            Debug.Log("Event Missing in InGamePanelManager");
            return;
        }
        _isValid = true;
    }

    private void Start()
    {
        if (_isValid) CloseAllPanel();
    }
    private void OnEnable()
    {
        if (!_isValid) return;
        _winEvent.Subscribe(OnWin);
        _loseEvent.Subscribe(OnLose);
    }

    private void OnDisable()
    {
        _inputGate?.Release(this);

        if (!_isValid) return;

        _winEvent.Unsubscribe(OnWin);
        _loseEvent.Unsubscribe(OnLose);
    }

    private void OnWin()
    {
        CloseAllPanel();
        _inputGate.Acquire(this);
        _winPanel.SetActive(true);
    }

    private void OnLose()
    {
        CloseAllPanel();
        _inputGate.Acquire(this);
        _losePanel.SetActive(true);
    }

    public void OnMenuButtonClick()
    {
        CloseAllPanel();
        _inputGate.Acquire(this);
        _menuPanel.SetActive(true);
    }

    public void OnOptionButtonClick()
    {
        CloseAllPanel();
        _inputGate.Acquire(this);

        _optionPanel.SetActive(true);
        _optionPanel.GetComponent<OptionPanelView>()?.Open();
        _optionPanel.GetComponent<MenuPanelView>()?.Show();
    }

    public void CloseAllPanel()
    {
        _losePanel?.SetActive(false);
        _winPanel?.SetActive(false);
        _menuPanel?.SetActive(false);
        _minimapHUDView?.Close();

        _optionPanel?.GetComponent<OptionPanelView>()?.Close();
        _optionPanel?.GetComponent<MenuPanelView>()?.Hide();

        _inputGate?.Release(this);
    }
}

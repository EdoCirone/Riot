using UnityEngine;

public class InGamePanelManager : MonoBehaviour
{
    [Header("Panels Reference")]
    [SerializeField] GameObject _losePanel;
    [SerializeField] GameObject _winPanel;
    [SerializeField] GameObject _menuPanel;
    [SerializeField] GameObject _optionPanel;

    [Header("Events")]
    [SerializeField] GameEventSO _loseEvent;
    [SerializeField] GameEventSO _winEvent;

    private bool _isValid = false;

    private void Awake()
    {
        if (_losePanel == null || _winPanel == null)
        {
            Debug.Log("Panels Missing in InGamePanelManager");
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
        if (!_isValid) return;
        _winEvent.Unsubscribe(OnWin);
        _loseEvent.Unsubscribe(OnLose);
    }
    private void OnWin() { _winPanel.SetActive(true); }
    private void OnLose() { _losePanel.SetActive(true); }

    public void OnMenuButtonClick() { _menuPanel.SetActive(true); }

    public void OnOptionButtonClick()
    {
        CloseAllPanel();

        _optionPanel.SetActive(true);
        _optionPanel.GetComponent<OptionPanelView>()?.Open();
        _optionPanel.GetComponent<MenuPanelView>()?.Show();
    }

    public void CloseAllPanel()
    {
        _losePanel?.SetActive(false);
        _winPanel?.SetActive(false);
        _menuPanel?.SetActive(false);

        _optionPanel?.GetComponent<OptionPanelView>()?.Close();
        _optionPanel.GetComponent<MenuPanelView>()?.Hide();
    }
}


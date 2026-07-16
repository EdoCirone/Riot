using UnityEngine;

public class MainMenuPanelManager : MonoBehaviour
{

    private MenuPanelView _openPanel;

    public void OpenPanel(MenuPanelView panel)
    {
        if (panel == null) return;

        if (_openPanel == panel)
        {
            CloseAll();
            return;
        }

        if(_openPanel != null)
        {
            _openPanel.Hide();
        }

        panel.Show();
        _openPanel = panel;
    }

    public void CloseAll()
    {
        if (_openPanel == null) return;
        
            _openPanel.Hide();
            _openPanel = null;
        
    }
}

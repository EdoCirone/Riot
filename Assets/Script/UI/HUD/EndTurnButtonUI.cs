using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private GameEventSO _endTurnButtonClickedEvent;

    public void OnClick()
    {
        if (_endTurnButtonClickedEvent == null)
        {
            Debug.LogWarning("End turn event not assigned in EndTurnButtonUI");
            return;
        }

        _endTurnButtonClickedEvent.Raise();
    }
}
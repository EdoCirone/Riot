using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ActionType _action;   
    [SerializeField] private Image _iconImage;     
    [SerializeField] private TextMeshProUGUI _hotkeyText; 

    private ActionButtonPanel _owner;

    public ActionType Action => _action;

    public void Init(ActionButtonPanel owner) => _owner = owner;

    public void SetActive(bool active)
    {
        _iconImage.color = active ? Color.white : new Color(1, 1, 1, 0.5f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _owner?.OnSlotClicked(_action);
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _quantityText;

    private ItemSO _currentItem;
    private InventoryView _owner;

    public void Init(InventoryView owner) => _owner = owner;

    public void SetItem(ItemSO item, int quantity)
    {
        _currentItem = item;
        _iconImage.sprite = item.InventoryIcon;
        _iconImage.enabled = true;
        if (_quantityText != null)
            _quantityText.text = quantity > 1 ? quantity.ToString() : "";   
    }

    public bool IsCompatibleWith(ActionType action)
    {
        return _currentItem != null && _currentItem.Action == action;   
    }

    public void SetHighlighted(bool highlighted)
    {
        
        _iconImage.color = highlighted ? Color.white : new Color(1, 1, 1, 0.3f);
    }

    public void Clear()
    {
        _currentItem = null;
        _iconImage.sprite = null;
        _iconImage.enabled = false;
        if (_quantityText != null)
            _quantityText.text = "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_currentItem == null) return;          // slot vuoto: click ignorato
        if (_iconImage.color.a < 1f) return;       // slot grigiato (non compatibile): ignora
        _owner?.OnSlotClicked(_currentItem);
    }

}
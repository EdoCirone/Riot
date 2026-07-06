using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _quantityText;

    private ItemSO _currentItem;

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
}
using UnityEngine;
using DG.Tweening;

public class ThrowObjectVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid _map;
    [SerializeField] private UnitsRenderer _unitsRenderer;

    [Header("Prefabs")]
    [SerializeField] private GameObject _trowhObjectPrefab;

    [Header ("Events")]
    [SerializeField] private UnitEventSO _throwObjectEvent;
    [SerializeField] private UnitEventSO _unitSelected;
    [SerializeField] private ItemEventSO _itemSelectedEvent;
    
    private ItemSO _selectedItem;
    private void SaveItem(ItemSO item) => _selectedItem = item;

    private AbstractUnitsRunTime _selectedUnit;

    private void Awake()
    {
        if (_throwObjectEvent == null)
        {
            Debug.LogWarning("Throw Object Event missing in ThrowObjectVFX");
            return;
        }
        if (_unitSelected == null)
        {
            Debug.LogWarning("Unit Selected Event missing in ThrowObjectVFX");
            return;
        }
        if (_itemSelectedEvent == null)
        {
            Debug.LogWarning("Item Selected Event missing in ThrowObjectVFX");
            return;
        }
    }

    private void OnEnable()
    {
        if (_throwObjectEvent == null || _unitSelected == null || _itemSelectedEvent == null) return;
        _unitSelected.Subscribe(SaveSelection);
        _throwObjectEvent.Subscribe(PlayThrowVFX);
        _itemSelectedEvent.Subscribe(SaveItem);
    }

    private void OnDisable()
    {
        if (_throwObjectEvent == null || _unitSelected == null || _itemSelectedEvent == null) return;
        _unitSelected.Unsubscribe(SaveSelection);
        _throwObjectEvent.Unsubscribe(PlayThrowVFX);
        _itemSelectedEvent.Unsubscribe(SaveItem);
    }

    private void SaveSelection(AbstractUnitsRunTime unit)
    {
        _selectedUnit = unit;
    }

    private void PlayThrowVFX(AbstractUnitsRunTime unit)
    {
        if (_trowhObjectPrefab == null)
        {
            Debug.LogWarning("Throw Object Prefab missing in ThrowObjectVFX");
            return;
        }

        if (_selectedUnit == null)
        {
            Debug.LogWarning("No selected unit to throw from in ThrowObjectVFX");
            return;
        }

        Vector3 selectedUnitPos = _map.GridToWorld(_selectedUnit.PositionCell.Coordinates);
        Vector3 targetUnitPos = _map.GridToWorld(unit.PositionCell.Coordinates);

        GameObject prefab = (_selectedItem != null && _selectedItem.GraphicPrefab != null)
            ? _selectedItem.GraphicPrefab
            : _trowhObjectPrefab;

        GameObject throwObject = Instantiate(prefab, selectedUnitPos, Quaternion.identity);

        throwObject.transform.DOJump(targetUnitPos, 1f, 1, 0.5f).OnComplete(() =>
        {
            _unitsRenderer?.FlashDamage(unit);
            Destroy(throwObject);
        });
    }

}



using UnityEngine;
using DG.Tweening;

public class T : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid _map;

    [Header("Prefabs")]
    [SerializeField] private GameObject _trowhObjectPrefab;

    [Header ("Events")]
    [SerializeField] private UnitEventSO _throwObjectEvent;
    [SerializeField] private UnitEventSO _unitSelected;

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
    }

    private void OnEnable()
    {
        if (_throwObjectEvent == null || _unitSelected == null) return;
        _unitSelected.Subscribe(SaveSelection);
        _throwObjectEvent.Subscribe(PlayThrowVFX);
    }

    private void OnDisable()
    {
        if (_throwObjectEvent == null || _unitSelected == null) return;
        _unitSelected.Unsubscribe(SaveSelection);
        _throwObjectEvent.Unsubscribe(PlayThrowVFX);
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
        
        Vector3 selectedUnitPos = _selectedUnit.PositionCell.Coordinates.ToWorldPosition(_map.CellSize); 
        Vector3 targetUnitPos = unit.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);

        GameObject throwObject = Instantiate(_trowhObjectPrefab, selectedUnitPos, Quaternion.identity);
        throwObject.transform.DOJump(targetUnitPos, 1f, 1, 0.5f).OnComplete(() =>
        {
            Destroy(throwObject);
        });
    }

}



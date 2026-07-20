using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SelectionOutline : MonoBehaviour
{
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private Color _outlineColor = Color.green;

    [Header("Events")]
    [SerializeField] private UnitEventSO _unitSelectedEvent;
    [SerializeField] private UnitEventSO _policeSelectedEvent;
    [SerializeField] private GameEventSO _unitDeselectedEvent;
    [SerializeField] private GameEventSO _policeDeselectedEvent;

    private bool _isSelectedAsUnit;
    private bool _isSelectedAsTarget;

    private AbstractUnitsRunTime _boundUnit;
    private List<GameObject> _outlineObjects = new();
    public void Initialize(AbstractUnitsRunTime unit)
    {
        _boundUnit = unit;
        BuildOutlineRenderers();
        Debug.Log($"[SelectionOutline] Initialize chiamato su {gameObject.name}, creati {_outlineObjects.Count} outline object");
    }

    private void BuildOutlineRenderers()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            GameObject go = new GameObject("Outline_" + sr.name);
            go.transform.SetParent(sr.transform, false);

            SpriteRenderer outlineSr = go.AddComponent<SpriteRenderer>();
            outlineSr.sprite = sr.sprite;
            outlineSr.sharedMaterial = _outlineMaterial;
            outlineSr.sortingLayerID = sr.sortingLayerID;
            outlineSr.sortingOrder = sr.sortingOrder - 1;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            outlineSr.GetPropertyBlock(mpb);
            mpb.SetColor("_OutlineColor", _outlineColor);
            outlineSr.SetPropertyBlock(mpb);
            Debug.Log($"[SelectionOutline] {gameObject.name}: setting _OutlineColor to {_outlineColor}");

            go.SetActive(false);
            _outlineObjects.Add(go);
        }
    }

    private void OnEnable()
    {
        _unitSelectedEvent.Subscribe(OnUnitSelected);
        _policeSelectedEvent.Subscribe(OnPoliceSelected);
        _unitDeselectedEvent.Subscribe(OnUnitDeselected);
        _policeDeselectedEvent.Subscribe(OnPoliceDeselected);
    }

    private void OnDisable()
    {
        _unitSelectedEvent.Unsubscribe(OnUnitSelected);
        _policeSelectedEvent.Unsubscribe(OnPoliceSelected);
        _unitDeselectedEvent.Unsubscribe(OnUnitDeselected);
        _policeDeselectedEvent.Unsubscribe(OnPoliceDeselected);
    }

    private void OnUnitSelected(AbstractUnitsRunTime unit)
    {
        _isSelectedAsUnit = (unit == _boundUnit);
        UpdateVisibility();
    }

    private void OnPoliceSelected(AbstractUnitsRunTime unit)
    {
        _isSelectedAsTarget = (unit == _boundUnit);
        UpdateVisibility();
    }

    private void OnUnitDeselected()
    {
        _isSelectedAsUnit = false;
        UpdateVisibility();
    }

    private void OnPoliceDeselected()
    {
        _isSelectedAsTarget = false;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool show = _isSelectedAsUnit || _isSelectedAsTarget;
        foreach (var go in _outlineObjects) go.SetActive(show);
    }
}
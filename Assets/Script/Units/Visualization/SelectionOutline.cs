using System.Collections.Generic;
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
        _unitSelectedEvent.Subscribe(OnAnyUnitSelected);
        _policeSelectedEvent.Subscribe(OnAnyUnitSelected);
        _unitDeselectedEvent.Subscribe(Hide);
        _policeDeselectedEvent.Subscribe(Hide);
    }

    private void OnDisable()
    {
        _unitSelectedEvent.Unsubscribe(OnAnyUnitSelected);
        _policeSelectedEvent.Unsubscribe(OnAnyUnitSelected);
        _unitDeselectedEvent.Unsubscribe(Hide);
        _policeDeselectedEvent.Unsubscribe(Hide);
    }

    private void OnAnyUnitSelected(AbstractUnitsRunTime unit)
    {
        bool isMe = unit == _boundUnit;
        foreach (var go in _outlineObjects) go.SetActive(isMe);
    }
    private void Hide()
    {
        foreach (var go in _outlineObjects) go.SetActive(false);
    }
}
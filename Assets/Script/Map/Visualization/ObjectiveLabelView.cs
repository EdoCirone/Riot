using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Un'etichetta in world space per ogni obiettivo, al centro del suo gruppo di celle.
/// Mostra nome e progresso: è il riscontro che il GDD 19.5 chiedeva per poter pianificare
/// un'occupazione, non solo un aiuto di sviluppo.
/// </summary>
public class ObjectiveLabelView : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private HexGrid _grid;
    [SerializeField] private TextMeshPro _labelPrefab;
    [SerializeField] private Transform _labelsParent;

    [Header("Layout")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.4f, 0f);

    [Header("Events")]
    [SerializeField] private GameEventSO _startPlayerTurnEvent;
    [SerializeField] private GameEventSO _boardChangedEvent;

    private readonly List<(ObjectiveRuntime objective, TextMeshPro label)> _labels = new();
    private bool _isValid;

    private void Awake()
    {
        if (_grid == null || _labelPrefab == null || _labelsParent == null)
        {
            Debug.LogWarning("Reference missing in ObjectiveLabelView", this);
            return;
        }

        _isValid = true;
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _startPlayerTurnEvent?.Subscribe(Refresh);
        _boardChangedEvent?.Subscribe(Refresh);
    }

    private void OnDisable()
    {
        if (!_isValid) return;
        _startPlayerTurnEvent?.Unsubscribe(Refresh);
        _boardChangedEvent?.Unsubscribe(Refresh);
    }

    private void Start()
    {
        if (!_isValid) return;

        foreach (ObjectiveRuntime objective in _grid.Objectives)
        {
            TextMeshPro label = Instantiate(
                _labelPrefab,
                Centroid(objective) + _offset,
                Quaternion.identity,
                _labelsParent
            );

            _labels.Add((objective, label));
        }

        Debug.Log($"Obiettivi trovati: {_grid.Objectives.Count}");
        Refresh();
    }

    private Vector3 Centroid(ObjectiveRuntime objective)
    {
        Vector3 sum = Vector3.zero;
        foreach (HexCell cell in objective.Cells)
            sum += _grid.GridToWorld(cell.Coordinates);
        return sum / Mathf.Max(1, objective.Cells.Count);
    }

    private void Refresh()
    {
        foreach (var (objective, label) in _labels)
        {
            if (label == null) continue;

            label.text = objective.IsClaimed
                ? $"{objective}\n<color=#66FF66>RIVENDICATO</color>"
                : $"{objective}\n{objective.Progress}/{objective.Required}";
        }
    }
}

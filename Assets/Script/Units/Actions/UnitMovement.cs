using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnitMovement : MonoBehaviour
{
    [SerializeField] private MovementSettingsSO _movementSettings;
    [SerializeField] private Transform _rootTransform;
    [SerializeField] private Transform _graphicsTransform;


    private AbstractUnitsRunTime _unit;
    private Coroutine _currentMove;
    private bool _isMoving;

    public bool IsMoving => _isMoving;

    public void Initialize(AbstractUnitsRunTime unit)
    {
        _unit = unit;
    }

    #region Movement
    public void MoveAlongPath(List<HexCell> path, HexGrid grid, Action onComplete)
    {
        if (_unit == null)
        {
            Debug.LogError("UnitMovement: _unit è null! Chiama Initialize() prima.");
            onComplete?.Invoke();
            return;
        }

        if (_isMoving || path == null || path.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (_currentMove != null)
            StopCoroutine(_currentMove);

        _currentMove = StartCoroutine(MoveCoroutine(path, grid, onComplete));
    }

    private IEnumerator MoveCoroutine(List<HexCell> path, HexGrid grid, Action onComplete)
    {
        _isMoving = true;


        foreach (HexCell cell in path)
        {
            Vector3 startPos = _rootTransform.position;
            Vector3 endPos = grid.transform.position + cell.Coordinates.ToWorldPosition(grid.CellSize);
            
            MustFlip(endPos);

            float elapsed = 0f;
            while (elapsed < _movementSettings.MoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _movementSettings.MoveDuration;
                t = t * t * (3f - 2f * t);

                _rootTransform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            _rootTransform.position = endPos;

            // Aggiorna la posizione logica DOPO aver raggiunto la cella
            _unit.SetPosition(cell);
        }

        _isMoving = false;
        onComplete?.Invoke();
    }

    public void StopMovement()
    {
        if (_currentMove != null)
        {
            StopCoroutine(_currentMove);
            _currentMove = null;
        }
        _isMoving = false;
    }

    #endregion

    #region Charge

    public void PlayCharge(HexCell cellDestination, Vector3 defenderWorldPos, HexCell defenderDestination, AbstractUnitsRunTime defender, HexGrid grid, Action onComplete)
    {
        Debug.Log("PlayCharge");
        if (_unit == null)
        {
            Debug.LogError("UnitMovement: _unit è null!");
            onComplete?.Invoke();
            return;
        }
        if (cellDestination == null)
        {
            Debug.LogError("UnitMovement: cellDestination è null!");
            onComplete?.Invoke();
            return;
        }
        if (_isMoving)
        {
            onComplete?.Invoke();
            return;
        }

        MustFlip(defenderWorldPos);

        Vector3 windupDir = (_rootTransform.position - defenderWorldPos).normalized;
        Vector3 windupTarget = _rootTransform.position + windupDir * _movementSettings.WindupDistance;

        StartCoroutine(ChargeSequence(windupTarget, cellDestination, defenderWorldPos,
            defenderDestination, defender, grid, onComplete));

        Debug.Log("PlayCharge: StartCoroutine");

    }

    //Coroutine

    private IEnumerator ChargeSequence(
    Vector3 windupTarget,
    HexCell cellDestination,
    Vector3 defenderWorldPos,
    HexCell defenderDestination,
    AbstractUnitsRunTime defender,
    HexGrid grid,
    Action onComplete)
    {
        Debug.Log("ChargeSequence: Start windup");
        _isMoving = true;

        // Windup
        yield return _rootTransform
            .DOMove(windupTarget, _movementSettings.WindupDuration)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        // Rincorsa
        Debug.Log("ChargeSequence: Start Run");
        Vector3 chargeEndPos = grid.transform.position + cellDestination.Coordinates.ToWorldPosition(grid.CellSize);
        float elapsed = 0f;
        while (elapsed < _movementSettings.ChargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _movementSettings.ChargeDuration;
            t = t * t * (3f - 2f * t);
            _rootTransform.position = Vector3.Lerp(windupTarget, chargeEndPos, t);
            yield return null;
        }
        _rootTransform.position = chargeEndPos;

        _unit.SetPosition(cellDestination);

        _isMoving = false;
        onComplete?.Invoke();
    }
    #endregion

    #region Flip
    private void MustFlip(Vector3 directionWorldPos)
    {
        float dirX = directionWorldPos.x - _rootTransform.position.x;

        bool shouldFaceLeft = dirX < 0;
        float currentScaleX = _graphicsTransform.localScale.x;

        if ((shouldFaceLeft && currentScaleX > 0) || (!shouldFaceLeft && currentScaleX < 0))
            _graphicsTransform.localScale = new Vector3(-currentScaleX, _graphicsTransform.localScale.y, _graphicsTransform.localScale.z);

    }

    public void FlipTowards(Vector3 targetWorldPos)
    {
        MustFlip(targetWorldPos);
    }
    #endregion

    public void StopEveryMovement()
    {
        StopAllCoroutines();
    }

}
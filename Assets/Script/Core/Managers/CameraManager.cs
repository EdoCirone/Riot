using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private HexGrid _map;

    [Header("Movement Settings")]
    [SerializeField] private float _cameraMoveMaxSpeed = 5f;
    [SerializeField] private float _cameraAcceleration = 10f;
    [SerializeField] private float _damping = 0.9f;

    [Header("Center Settings")]
    [SerializeField] private float _centerSelectionTime = 0.2f;
    [SerializeField] private Ease _easeType = Ease.InOutSine;

    [Header("Zoom Settings")]
    [SerializeField] private float _stepSize = 2f;
    [SerializeField] private float _minZoom = 3f;
    [SerializeField] private float _maxZoom = 10f;
    [SerializeField] private float _zoomSpeed = 3f;

    [Header("Events")]
    [SerializeField] private UnitEventSO _onSelectedEvent;

    private InputSystem_Actions _inputSystem;
    private AbstractUnitsRunTime _lastCenteredUnit;
    private Tweener _cameraTween;

    private Vector2 _moveInput;
    private float _currentMoveSpeed;
    private float _currentZoomSpeed; 

    private void Awake()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) Debug.LogWarning("Main Camera not found.");
        if (_map == null) Debug.LogWarning("HexGrid not assigned.");

        _inputSystem = new InputSystem_Actions();
        _cameraTween?.Kill();
    }

    private void OnEnable()
    {
        // Abilita gli action map
        _inputSystem.Camera.Zoom.Enable();
        _inputSystem.Camera.CameraMovement.Enable();

        // --- MOVIMENTO ---
        _inputSystem.Camera.CameraMovement.performed += OnMovementPerformed;
        _inputSystem.Camera.CameraMovement.canceled += OnMovementCanceled;

        // --- ZOOM ---
        _inputSystem.Camera.Zoom.performed += OnZoomPerformed;
        _inputSystem.Camera.Zoom.canceled += OnZoomCanceled;

        // --- SELEZIONE UNITÀ ---
        if (_onSelectedEvent != null)
            _onSelectedEvent.Subscribe(CenterCamera);
    }

    private void OnDisable()
    {
        // Rimuovi i callback
        _inputSystem.Camera.CameraMovement.performed -= OnMovementPerformed;
        _inputSystem.Camera.CameraMovement.canceled -= OnMovementCanceled;
        _inputSystem.Camera.Zoom.performed -= OnZoomPerformed;
        _inputSystem.Camera.Zoom.canceled -= OnZoomCanceled;

        // Disabilita le action
        _inputSystem.Camera.Zoom.Disable();
        _inputSystem.Camera.CameraMovement.Disable();

        if (_onSelectedEvent != null)
        {
            _onSelectedEvent.Unsubscribe(CenterCamera);
            _cameraTween?.Kill();
        }
    }

    #region Callbacks Input

    private void OnMovementPerformed(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMovementCanceled(InputAction.CallbackContext ctx)
    {
        _moveInput = Vector2.zero;
    }

    private void OnZoomPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 zoomInput = ctx.ReadValue<Vector2>();
        _currentZoomSpeed = zoomInput.y * _stepSize;
    }

    private void OnZoomCanceled(InputAction.CallbackContext ctx)
    {
        _currentZoomSpeed = 0f;
    }

    #endregion

    #region Update

    private void Update()
    {
        // ---- MOVIMENTO ----
        float targetSpeed = _moveInput.magnitude * _cameraMoveMaxSpeed;
        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetSpeed, _cameraAcceleration * Time.deltaTime);

        if (_currentMoveSpeed > 0.01f)
        {
            Vector3 moveDir = _moveInput.normalized;
            Vector3 delta = moveDir * _currentMoveSpeed * Time.deltaTime;
            _mainCamera.transform.position += delta;
        }
        // Damping quando non c'è input
        else if (_moveInput.magnitude < 0.01f && _currentMoveSpeed > 0.01f)
        {
            _currentMoveSpeed = Mathf.Lerp(_currentMoveSpeed, 0f, _damping * Time.deltaTime);
        }

        // ---- ZOOM ----
        if (Mathf.Abs(_currentZoomSpeed) > 0.001f && _mainCamera.orthographic)
        {
            float targetSize = Mathf.Clamp(
                _mainCamera.orthographicSize - _currentZoomSpeed,
                _minZoom,
                _maxZoom
            );
            _mainCamera.orthographicSize = Mathf.Lerp(
                _mainCamera.orthographicSize,
                targetSize,
                Time.deltaTime * _zoomSpeed
            );
        }
    }

    #endregion

    #region Centratura su unità

    private void CenterCamera(AbstractUnitsRunTime unit)
    {
        if (unit == null || _mainCamera == null || _map == null) return;
        if (unit == _lastCenteredUnit) return;
        _lastCenteredUnit = unit;

        Vector3 targetPos = unit.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);
        targetPos.z = _mainCamera.transform.position.z;

        if (Vector3.Distance(_mainCamera.transform.position, targetPos) < 0.1f) return;

        _cameraTween?.Kill();
        _cameraTween = _mainCamera.transform
            .DOMove(targetPos, _centerSelectionTime)
            .SetEase(_easeType);
    }

    #endregion
}
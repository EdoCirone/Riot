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

    [Header("EdgeScroll Settings")]
    [SerializeField] private bool _edgeScrollEnabled = true;
    [SerializeField] private float _edgeScrollMargin = 15f;

    [Header("Follow Settings")]
    [SerializeField] private float _centerSelectionTime = 0.2f;
    [SerializeField] private Ease _easeType = Ease.InOutSine;
    [SerializeField] private float _followSpeed = 5f;

    [Header("Zoom Settings")]
    [SerializeField] private float _stepSize = 2f;
    [SerializeField] private float _minZoom = 3f;
    [SerializeField] private float _maxZoom = 10f;
    [SerializeField] private float _zoomSpeed = 3f;
    [SerializeField] private float _scrollZoomStep = 0.5f;

    [Header("Events")]
    [SerializeField] private GameEventSO _startPlayerTurnEvent;
    [SerializeField] private GameObjectEventSO _startFollowEvent;
    [SerializeField] private GameEventSO _stopFollowEvent;
    [SerializeField] private UnitEventSO _onSelectedEvent;

    private Transform _followTarget;
    private AbstractUnitsRunTime _lastPlayerUnit;

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

        // --- SEGUI UNITÀ ---
        if (_onSelectedEvent != null)
            _onSelectedEvent.Subscribe(CenterCamera);
        _startPlayerTurnEvent.Subscribe(OnPlayerTurnStart);
        _startFollowEvent.Subscribe(StartFollow);
        _stopFollowEvent.Subscribe(StopFollow);
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
            _onSelectedEvent.Unsubscribe(CenterCamera);
        _startFollowEvent.Unsubscribe(StartFollow);
        _stopFollowEvent.Unsubscribe(StopFollow);
        _startPlayerTurnEvent.Unsubscribe(OnPlayerTurnStart);


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


    #endregion

    #region Update

    private void Update()
    {
        if (_followTarget != null)
        {
            // FOLLOW: insegue il transform, pan manuale sospeso
            Vector3 targetPos = _followTarget.position;
            targetPos.z = _mainCamera.transform.position.z;
            _mainCamera.transform.position = Vector3.Lerp(
                _mainCamera.transform.position, targetPos, _followSpeed * Time.deltaTime);
            return;   // <-- salta il movimento manuale mentre segue
        }

        // ---- MOVIMENTO ----
        Vector2 edgeInput = _edgeScrollEnabled ? GetEdgeScrollInput() : Vector2.zero;
        Vector2 combineInput = Vector2.ClampMagnitude(_moveInput + edgeInput, 1f);

        float targetSpeed = combineInput.magnitude * _cameraMoveMaxSpeed;
        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetSpeed, _cameraAcceleration * Time.deltaTime);

        if (_currentMoveSpeed > 0.01f)
        {
            Vector3 moveDir = combineInput.normalized;
            Vector3 delta = moveDir * _currentMoveSpeed * Time.deltaTime;
            _mainCamera.transform.position += delta;
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

    #region Zoom

    private void OnZoomPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 zoomInput = ctx.ReadValue<Vector2>();
    
        if(ctx.control.device is Mouse)
        {
        
            ApplyScrollZoomStep(zoomInput.y);
            return;

        }
        
        _currentZoomSpeed = zoomInput.y * _stepSize;
    }

    private void OnZoomCanceled(InputAction.CallbackContext ctx)
    {
        if(ctx.control.device is Mouse) return;
        
        _currentZoomSpeed = 0f;
    }

    private void ApplyScrollZoomStep(float scrollValue)
    {
        if (!_mainCamera.orthographic || Mathf.Approximately(scrollValue, 0f)) return;
        
        float direction = Mathf.Sign(scrollValue);
        _mainCamera.orthographicSize = Mathf.Clamp(
            _mainCamera.orthographicSize - direction * _scrollZoomStep,
            _minZoom,
            _maxZoom
        );
    }

    #endregion

    #region EdgeScroll

    private Vector2 GetEdgeScrollInput()
    {
        if (Mouse.current == null) return Vector2.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 dir = Vector2.zero;

        if(mousePos.x <= _edgeScrollMargin) dir.x = -1f; 
        else if(mousePos.x >= Screen.width - _edgeScrollMargin) dir.x = 1f;

        if(mousePos.y <= _edgeScrollMargin) dir.y = -1f; 
        else if(mousePos.y >= Screen.height - _edgeScrollMargin) dir.y = 1f; 

        return dir.normalized;
    }

    #endregion

    #region Centratura su unità

    private void CenterCamera(AbstractUnitsRunTime unit)
    {
        if (unit is SpezzoneRuntime)
            _lastPlayerUnit = unit;

        if (_followTarget != null) return;
        if (unit == null || _mainCamera == null || _map == null) return;
        if (unit == _lastCenteredUnit) return;
        _lastCenteredUnit = unit;

        Vector3 targetPos = _map.transform.position + unit.PositionCell.Coordinates.ToWorldPosition(_map.CellSize);
        targetPos.z = _mainCamera.transform.position.z;

        if (Vector3.Distance(_mainCamera.transform.position, targetPos) < 0.1f) return;

        _cameraTween?.Kill();
        _cameraTween = _mainCamera.transform
            .DOMove(targetPos, _centerSelectionTime)
            .SetEase(_easeType);
    }

    private void StartFollow(GameObject target)
    {
        _cameraTween?.Kill();
        _followTarget = target != null ? target.transform : null;
    }
    private void StopFollow()
    {
        _followTarget = null;
        _lastCenteredUnit = null;
    }

    private void OnPlayerTurnStart()
    {
        if (_lastPlayerUnit == null) return;
        if (_lastPlayerUnit.Status == UnitsStatus.Disperse) return;   
        _lastCenteredUnit = null;              
        CenterCamera(_lastPlayerUnit);
    }

    #endregion
}
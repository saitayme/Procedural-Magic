using UnityEngine;
using Unity.Mathematics;

namespace ProceduralWorld.Simulation.Camera
{
    public class SimulationCameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float fastPanMultiplier = 2.5f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 50f;
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 500f;
        
        [Header("Edge Scrolling")]
        [SerializeField] private bool enableEdgeScrolling = true;
        [SerializeField] private float edgeScrollBorder = 50f;
        [SerializeField] private float edgeScrollSpeed = 15f;
        
        [Header("Mouse Controls")]
        [SerializeField] private bool enableMiddleMouseDrag = true;
        [SerializeField] private float mouseDragSensitivity = 2f;
        
        [Header("Bounds")]
        [SerializeField] private bool enableBounds = true;
        [SerializeField] private float worldSize = 500f;
        [SerializeField] private float boundsPadding = 50f;
        
        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float zoomSmoothTime = 0.05f;
        
        // Private variables
        private UnityEngine.Camera _camera;
        private Vector3 _targetPosition;
        private float _targetZoom;
        private Vector3 _velocity = Vector3.zero;
        private float _zoomVelocity = 0f;
        
        // Input state
        private Vector3 _inputVector = Vector3.zero;
        private bool _isDragging = false;
        private Vector3 _lastMousePosition;
        private float _currentPanSpeed;
        
        // Edge scrolling
        private Vector2 _edgeScrollVector = Vector2.zero;
        
        void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
            {
                Debug.LogError("[SimulationCameraController] No Camera component found!");
                enabled = false;
                return;
            }
            
            // Initialize target values
            _targetPosition = transform.position;
            _targetZoom = _camera.orthographic ? _camera.orthographicSize : _camera.fieldOfView;
            _currentPanSpeed = panSpeed;
            
            // Set up camera clipping planes for wide zoom range
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 2000f;
            
            Debug.Log("[SimulationCameraController] Camera controller initialized");
        }
        
        void Update()
        {
            HandleInput();
            UpdateMovement();
            UpdateZoom();
            ApplyBounds();
            
            // Dynamically adjust clipping planes based on zoom level
            UpdateClippingPlanes();
            
            if (enableSmoothing)
            {
                ApplySmoothMovement();
            }
            else
            {
                ApplyDirectMovement();
            }
        }
        
        void HandleInput()
        {
            // Reset input
            _inputVector = Vector3.zero;
            _edgeScrollVector = Vector2.zero;
            
            // WASD/Arrow Key Movement
            HandleKeyboardInput();
            
            // Edge Scrolling
            if (enableEdgeScrolling)
            {
                HandleEdgeScrolling();
            }
            
            // Mouse Wheel Zoom
            HandleMouseWheelZoom();
            
            // Middle Mouse Drag
            if (enableMiddleMouseDrag)
            {
                HandleMiddleMouseDrag();
            }
        }
        
        void HandleKeyboardInput()
        {
            // Get input axes
            float horizontal = 0f;
            float vertical = 0f;
            
            // WASD
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                vertical -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                horizontal += 1f;
            
            // Create movement vector
            _inputVector = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Speed boost with Shift
            bool fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _currentPanSpeed = fastMode ? panSpeed * fastPanMultiplier : panSpeed;
        }
        
        void HandleEdgeScrolling()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            // Check edges
            if (mousePos.x <= edgeScrollBorder)
                _edgeScrollVector.x = -1f;
            else if (mousePos.x >= screenSize.x - edgeScrollBorder)
                _edgeScrollVector.x = 1f;
                
            if (mousePos.y <= edgeScrollBorder)
                _edgeScrollVector.y = -1f;
            else if (mousePos.y >= screenSize.y - edgeScrollBorder)
                _edgeScrollVector.y = 1f;
            
            // Apply edge scrolling to input
            if (_edgeScrollVector.magnitude > 0f)
            {
                _inputVector += new Vector3(_edgeScrollVector.x, 0f, _edgeScrollVector.y).normalized;
                _currentPanSpeed = edgeScrollSpeed;
            }
        }
        
        void HandleMouseWheelZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float zoomDelta = -scroll * zoomSpeed;
                
                if (_camera.orthographic)
                {
                    _targetZoom = Mathf.Clamp(_targetZoom + zoomDelta, minZoom, maxZoom);
                }
                else
                {
                    _targetZoom = Mathf.Clamp(_targetZoom + zoomDelta, minZoom, maxZoom);
                }
            }
        }
        
        void HandleMiddleMouseDrag()
        {
            if (Input.GetMouseButtonDown(2)) // Middle mouse button
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _isDragging = false;
            }
            
            if (_isDragging && Input.GetMouseButton(2))
            {
                Vector3 currentMousePosition = Input.mousePosition;
                Vector3 mouseDelta = currentMousePosition - _lastMousePosition;
                
                // Convert screen space movement to world space movement for isometric camera
                // Scale by zoom level for consistent drag sensitivity
                float zoomFactor = _camera.orthographic ? _camera.orthographicSize / 20f : _targetZoom / 60f;
                float dragScale = mouseDragSensitivity * zoomFactor * 0.01f;
                
                // Apply drag movement (inverted for natural feel) - only move in X and Z
                Vector3 worldDelta = new Vector3(-mouseDelta.x * dragScale, 0f, -mouseDelta.y * dragScale);
                _targetPosition += worldDelta;
                
                _lastMousePosition = currentMousePosition;
            }
        }
        
        void UpdateMovement()
        {
            if (_inputVector.magnitude > 0.01f)
            {
                // Calculate movement based on current zoom level for consistent speed
                float zoomFactor = _camera.orthographic ? _camera.orthographicSize / 20f : _targetZoom / 60f;
                float adjustedSpeed = _currentPanSpeed * zoomFactor;
                
                // Use world-space directions for isometric camera movement
                // This ensures WASD moves in predictable world directions regardless of camera angle
                Vector3 right = Vector3.right;    // World X-axis (A/D keys)
                Vector3 forward = Vector3.forward; // World Z-axis (W/S keys)
                
                // Calculate world-space movement vector
                Vector3 worldMovement = (right * _inputVector.x + forward * _inputVector.z) * adjustedSpeed * Time.unscaledDeltaTime;
                _targetPosition += worldMovement;
            }
        }
        
        void UpdateZoom()
        {
            // Zoom is handled in HandleMouseWheelZoom
        }
        
        void UpdateClippingPlanes()
        {
            // Keep clipping planes stable to prevent terrain clipping issues
            // Use fixed values that work well for our isometric setup
            _camera.nearClipPlane = 1f;
            _camera.farClipPlane = 3000f;
        }
        
        void ApplyBounds()
        {
            if (!enableBounds) return;
            
            float halfWorld = worldSize * 0.5f;
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, -halfWorld + boundsPadding, halfWorld - boundsPadding);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, -halfWorld + boundsPadding, halfWorld - boundsPadding);
        }
        
        void ApplySmoothMovement()
        {
            // Smooth position using unscaled time
            transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, positionSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            
            // Smooth zoom using unscaled time
            if (_camera.orthographic)
            {
                _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _targetZoom, ref _zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            else
            {
                _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, _targetZoom, ref _zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }
        }
        
        void ApplyDirectMovement()
        {
            // Direct movement (no smoothing)
            transform.position = _targetPosition;
            
            if (_camera.orthographic)
            {
                _camera.orthographicSize = _targetZoom;
            }
            else
            {
                _camera.fieldOfView = _targetZoom;
            }
        }
        
        // Public methods for external control
        public void SetPosition(Vector3 position)
        {
            _targetPosition = position;
            if (!enableSmoothing)
            {
                transform.position = position;
            }
        }
        
        public void SetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            if (!enableSmoothing)
            {
                if (_camera.orthographic)
                {
                    _camera.orthographicSize = _targetZoom;
                }
                else
                {
                    _camera.fieldOfView = _targetZoom;
                }
            }
        }
        
        public void FocusOnPosition(Vector3 position, float zoom = -1f)
        {
            SetPosition(position);
            if (zoom > 0f)
            {
                SetZoom(zoom);
            }
        }
        
        public void SetWorldSize(float size)
        {
            worldSize = size;
        }
        
        // Debug info - DISABLED: Now managed by SimulationUIManager
        /*
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
#if UNITY_EDITOR
            GUILayout.Label("Camera Controls:", UnityEditor.EditorGUIUtility.isProSkin ? GUI.skin.box : GUI.skin.label);
#else
            GUILayout.Label("Camera Controls:", GUI.skin.label);
#endif
            GUILayout.Label("WASD/Arrows: Move camera");
            GUILayout.Label("Mouse Wheel: Zoom in/out");
            GUILayout.Label("Middle Mouse: Drag to pan");
            GUILayout.Label("Shift: Fast movement");
            GUILayout.Label("Edge Scrolling: Move mouse to screen edges");
            GUILayout.Space(10);
            GUILayout.Label($"Position: {transform.position:F1}");
            GUILayout.Label($"Zoom: {(_camera.orthographic ? _camera.orthographicSize : _camera.fieldOfView):F1}");
            GUILayout.Label($"Input: {_inputVector:F2}");
            GUILayout.EndArea();
        }
        */
    }
} 
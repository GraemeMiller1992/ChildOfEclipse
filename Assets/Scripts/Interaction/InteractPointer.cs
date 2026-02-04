using UnityEngine;
using UnityEngine.InputSystem;

namespace ChildOfEclipse
{
    /// <summary>
    /// Handles pointer-based interaction with 3D objects in the scene.
    /// Uses raycasting from the camera to detect and interact with IInteractable objects.
    /// </summary>
    public class InteractPointer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input References")]
        [Tooltip("Reference to the Point input action (mouse/touch position).")]
        [SerializeField] private InputActionReference pointActionReference;
        
        [Tooltip("Reference to the Click input action (left mouse button/tap).")]
        [SerializeField] private InputActionReference clickActionReference;

        [Header("Camera Reference")]
        [Tooltip("Reference to the camera for raycasting. If null, will use Camera.main.")]
        [SerializeField] private Camera raycastCamera;

        [Header("Raycast Settings")]
        [Tooltip("Maximum distance for raycasting.")]
        [SerializeField] private float maxRayDistance = 100f;
        
        [Tooltip("Layer mask for raycasting (which layers to check for interactables).")]
        [SerializeField] private LayerMask interactableLayerMask = 1; // Default to "Default" layer only
        
        [Tooltip("Whether to use a visual ray for debugging.")]
        [SerializeField] private bool showDebugRay = false;

        [Header("Interaction Settings")]
        [Tooltip("Whether to require the object to be in view to interact.")]
        [SerializeField] private bool requireLineOfSight = true;

        [Header("Distance Settings")]
        [Tooltip("Maximum distance from this InteractPointer object (e.g., player) to interactable objects.")]
        [SerializeField] private float maxInteractionDistance = 5f;

        [Tooltip("Whether to clamp the hit point to the maximum interaction distance instead of invalidating it.")]
        [SerializeField] private bool clampToMaxDistance = false;

        [Header("World UI Settings")]
        [Tooltip("Prefab to spawn at pointer position (e.g., cursor, interaction prompt).")]
        [SerializeField] private GameObject worldUIPrefab;
        
        [Tooltip("Distance to offset UI above surface along normal to prevent clipping.")]
        [SerializeField] private float surfaceOffset = 0.1f;
        
        [Tooltip("Whether to hide UI when not pointing at anything.")]
        [SerializeField] private bool hideUIWhenNoHit = true;

        #endregion

        #region Private Fields

        // Input Actions
        private InputAction _pointAction;
        private InputAction _clickAction;

        // State
        private IInteractable _currentHoveredInteractable;
        private IInteractable _previousHoveredInteractable;
        private Vector2 _pointerPosition;
        private GameObject _worldUIInstance;
        private Vector3 _currentHitPoint;
        private Vector3 _currentHitNormal;
        private bool _hasValidHit;
        private Collider _currentHitCollider;

        #endregion

        #region Properties

        /// <summary>
        /// Returns the currently hovered interactable object.
        /// </summary>
        public IInteractable CurrentHoveredInteractable => _currentHoveredInteractable;

        /// <summary>
        /// Returns the current pointer position in screen coordinates.
        /// </summary>
        public Vector2 PointerPosition => _pointerPosition;

        /// <summary>
        /// Returns the current world UI instance.
        /// </summary>
        public GameObject WorldUIInstance => _worldUIInstance;

        /// <summary>
        /// Returns the current hit point in world space.
        /// </summary>
        public Vector3 CurrentHitPoint => _currentHitPoint;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get camera if not assigned
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
                if (raycastCamera == null)
                {
                    Debug.LogError("No camera assigned or found in scene!", this);
                }
            }
        }

        private void Start()
        {
            // Get PlayerInput from singleton
            var playerInput = PlayerInputSingleton.Instance?.PlayerInput;

            if (playerInput == null)
            {
                Debug.LogError("PlayerInputSingleton.Instance or PlayerInput component not found!", this);
                return;
            }

            // Retrieve InputActions using their IDs
            _pointAction = playerInput.actions.FindAction(pointActionReference.action.id);
            _clickAction = playerInput.actions.FindAction(clickActionReference.action.id);

            // Validate actions
            if (_pointAction == null) Debug.LogError("Point action not found!", this);
            if (_clickAction == null) Debug.LogError("Click action not found!", this);

            // Spawn world UI if prefab is assigned
            if (worldUIPrefab != null)
            {
                _worldUIInstance = Instantiate(worldUIPrefab);
                _worldUIInstance.SetActive(false);
            }
        }

        private void Update()
        {
            HandlePointerInput();
            HandleRaycast();
            HandleHover();
            HandleClick();
        }

        #endregion

        #region Input Handling

        private void HandlePointerInput()
        {
            // Read pointer position
            if (_pointAction != null)
            {
                _pointerPosition = _pointAction.ReadValue<Vector2>();
            }
        }

        #endregion

        #region Raycasting

        private void HandleRaycast()
        {
            _previousHoveredInteractable = _currentHoveredInteractable;
            _currentHoveredInteractable = null;
            _hasValidHit = false;

            if (raycastCamera == null) return;

            // Create ray from camera through pointer position
            Ray ray = raycastCamera.ScreenPointToRay(_pointerPosition);

            // Perform raycast
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayerMask))
            {
                _currentHitPoint = hit.point;
                _currentHitNormal = hit.normal;
                _currentHitCollider = hit.collider;

                // Check distance from InteractPointer to the hit point
                float distanceFromPlayer = Vector3.Distance(transform.position, hit.point);
                bool isWithinRange = distanceFromPlayer <= maxInteractionDistance;

                if (isWithinRange)
                {
                    // Within range - valid hit
                    _hasValidHit = true;
                    
                    // Check if the hit object has an IInteractable component
                    _currentHoveredInteractable = hit.collider.GetComponent<IInteractable>();

                    // Check line of sight if required
                    if (_currentHoveredInteractable != null && requireLineOfSight)
                    {
                        Vector3 directionToCamera = (raycastCamera.transform.position - hit.point).normalized;
                        if (Physics.Raycast(hit.point, directionToCamera, out RaycastHit sightHit,
                            Vector3.Distance(hit.point, raycastCamera.transform.position), interactableLayerMask))
                        {
                            // Something is blocking the line of sight
                            if (sightHit.collider != hit.collider)
                            {
                                _currentHoveredInteractable = null;
                            }
                        }
                    }

                    // Check if the interactable can be interacted with
                    if (_currentHoveredInteractable != null && !_currentHoveredInteractable.CanInteract)
                    {
                        _currentHoveredInteractable = null;
                    }
                }
                else
                {
                    // Out of range - always invalidate the interactable
                    _currentHoveredInteractable = null;
                    
                    if (clampToMaxDistance)
                    {
                        // Clamp the hit point to the maximum allowed distance from the player for visual purposes
                        _hasValidHit = true;
                        Vector3 directionFromPlayer = (hit.point - transform.position).normalized;
                        _currentHitPoint = transform.position + directionFromPlayer * maxInteractionDistance;
                    }
                    else
                    {
                        // No clamping - invalidate the hit entirely
                        _hasValidHit = false;
                        _currentHitCollider = null;
                    }
                }

                // Draw debug ray if enabled
                if (showDebugRay)
                {
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance,
                        _currentHoveredInteractable != null ? Color.green : Color.yellow);
                }
            }
            else if (showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.red);
            }

            // Update world UI position
            UpdateWorldUI();
        }

        #endregion

        #region World UI

        private void UpdateWorldUI()
        {
            if (_worldUIInstance == null) return;
            
            if (_hasValidHit)
            {
                // Calculate position offset along the surface normal to prevent clipping
                Vector3 uiPosition = _currentHitPoint + _currentHitNormal * surfaceOffset;
                _worldUIInstance.transform.position = uiPosition;
                
                // Rotate UI to align with surface normal (decal effect)
                Quaternion targetRotation = Quaternion.LookRotation(_currentHitNormal, Vector3.up);
                _worldUIInstance.transform.rotation = targetRotation;
                
                _worldUIInstance.SetActive(true);
            }
            else if (hideUIWhenNoHit)
            {
                _worldUIInstance.SetActive(false);
            }
        }

        #endregion

        #region Hover Handling

        private void HandleHover()
        {
            // Handle hover exit
            if (_previousHoveredInteractable != null && _previousHoveredInteractable != _currentHoveredInteractable)
            {
                _previousHoveredInteractable.OnHoverExit(gameObject);
            }

            // Handle hover enter
            if (_currentHoveredInteractable != null && _currentHoveredInteractable != _previousHoveredInteractable)
            {
                _currentHoveredInteractable.OnHoverEnter(gameObject);
            }
        }

        #endregion

        #region Click Handling

        private void HandleClick()
        {
            // Check for click input
            if (_clickAction != null && _clickAction.WasPressedThisFrame())
            {
                if (_currentHoveredInteractable != null)
                {
                    // Perform raycast again to get hit info for interaction
                    Ray ray = raycastCamera.ScreenPointToRay(_pointerPosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayerMask))
                    {
                        // Verify the hit object is still the same interactable
                        if (hit.collider.GetComponent<IInteractable>() == _currentHoveredInteractable)
                        {
                            _currentHoveredInteractable.OnInteract(gameObject, hit);
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually triggers an interaction with the currently hovered object.
        /// </summary>
        public void TriggerInteraction()
        {
            if (_currentHoveredInteractable != null)
            {
                Ray ray = raycastCamera.ScreenPointToRay(_pointerPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayerMask))
                {
                    if (hit.collider.GetComponent<IInteractable>() == _currentHoveredInteractable)
                    {
                        _currentHoveredInteractable.OnInteract(gameObject, hit);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the interaction description of the currently hovered object.
        /// </summary>
        public string GetCurrentInteractionDescription()
        {
            return _currentHoveredInteractable?.InteractionDescription ?? string.Empty;
        }

        /// <summary>
        /// Sets the camera for raycasting.
        /// </summary>
        public void SetCamera(Camera camera)
        {
            raycastCamera = camera;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (raycastCamera != null && showDebugRay)
            {
                Ray ray = raycastCamera.ScreenPointToRay(_pointerPosition);
                Gizmos.color = _currentHoveredInteractable != null ? Color.green : Color.red;
                Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * maxRayDistance);
            }

            // Draw max interaction distance sphere around the InteractPointer (player)
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxInteractionDistance);
            
            // Draw the hit point
            if (_hasValidHit)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(_currentHitPoint, 0.1f);
                
                // Draw line from player to hit point
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, _currentHitPoint);
            }
        }

        #endregion
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace ChildOfEclipse
{
    /// <summary>
    /// Rigidbody-based player controller using InputSystem with PlayerInputSingleton.
    /// Supports movement, jumping, sprinting, and crouching.
    /// Camera is handled separately by Cinemachine 3.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RigidbodyPlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input References")]
        [Tooltip("Reference to the Move input action.")]
        [SerializeField] private InputActionReference moveActionReference;
        
        [Tooltip("Reference to the Jump input action.")]
        [SerializeField] private InputActionReference jumpActionReference;
        
        [Tooltip("Reference to the Sprint input action.")]
        [SerializeField] private InputActionReference sprintActionReference;
        
        [Tooltip("Reference to the Crouch input action.")]
        [SerializeField] private InputActionReference crouchActionReference;

        [Header("Movement Settings")]
        [Tooltip("Normal walking speed.")]
        [SerializeField] private float walkSpeed = 5f;
        
        [Tooltip("Speed multiplier when sprinting.")]
        [SerializeField] private float sprintSpeedMultiplier = 1.8f;
        
        [Tooltip("Speed multiplier when crouching.")]
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;
        
        [Tooltip("How quickly the player accelerates.")]
        [SerializeField] private float acceleration = 10f;
        
        [Tooltip("How quickly the player decelerates.")]
        [SerializeField] private float deceleration = 10f;
        
        [Tooltip("Maximum angle the player can walk up (in degrees).")]
        [SerializeField] private float maxSlopeAngle = 45f;

        [Tooltip("How quickly the player rotates to face movement direction.")]
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Jump Settings")]
        [Tooltip("Initial jump velocity.")]
        [SerializeField] private float jumpForce = 8f;
        
        [Tooltip("Maximum time to hold jump for variable height.")]
        [SerializeField] private float jumpHoldTime = 0.2f;
        
        [Tooltip("Gravity multiplier while falling (for heavier feel).")]
        [SerializeField] private float fallGravityMultiplier = 1.5f;
        
        [Tooltip("Coyote time: how long after leaving ground player can still jump.")]
        [SerializeField] private float coyoteTime = 0.1f;
        
        [Tooltip("Jump buffer: how long before landing a jump input is remembered.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Crouch Settings")]
        [Tooltip("Height of the collider when crouching.")]
        [SerializeField] private float crouchHeight = 1f;
        
        [Tooltip("Normal height of the collider.")]
        [SerializeField] private float normalHeight = 2f;
        
        [Tooltip("How fast the player crouches/stands up.")]
        [SerializeField] private float crouchSpeed = 5f;

        [Header("Physics Settings")]
        [Tooltip("Ground check distance.")]
        [SerializeField] private float groundCheckDistance = 0.1f;
        
        [Tooltip("Layer mask for ground detection.")]
        [SerializeField] private LayerMask groundLayer = 1; // Default to "Default" layer
        
        [Tooltip("Offset for ground check from bottom of collider.")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);

        [Header("Camera Reference")]
        [Tooltip("Reference to the main camera for movement direction calculation.")]
        [SerializeField] private Transform cameraTransform;

        #endregion

        #region Private Fields

        // Input Actions
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;

        // Components
        private Rigidbody _rb;
        private CapsuleCollider _capsuleCollider;

        // State
        private Vector2 _moveInput;
        private bool _isJumping;
        private bool _isSprinting;
        private bool _isCrouching;
        private bool _isGrounded;
        private bool _wasGrounded;
        private float _currentHeight;
        private float _jumpHoldTimer;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // Movement
        private Vector3 _targetVelocity;
        private Vector3 _currentVelocity;
        private Vector3 _groundNormal;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the player is currently on the ground.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// Returns true if the player is currently sprinting.
        /// </summary>
        public bool IsSprinting => _isSprinting;

        /// <summary>
        /// Returns true if the player is currently crouching.
        /// </summary>
        public bool IsCrouching => _isCrouching;

        /// <summary>
        /// Returns the current movement speed.
        /// </summary>
        public float CurrentSpeed => _rb.linearVelocity.magnitude;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get required components
            _rb = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();

            // Validate components
            if (_rb == null)
            {
                Debug.LogError("Rigidbody component not found!", this);
            }
            if (_capsuleCollider == null)
            {
                Debug.LogError("CapsuleCollider component not found!", this);
            }

            // Initialize height
            _currentHeight = normalHeight;

            // Find camera if not assigned
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogWarning("Camera transform not assigned and main camera not found. Movement will be world-space only.", this);
                }
            }

            // Configure Rigidbody
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0.05f;
            _rb.freezeRotation = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
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
            _moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
            _jumpAction = playerInput.actions.FindAction(jumpActionReference.action.id);
            _sprintAction = playerInput.actions.FindAction(sprintActionReference.action.id);
            _crouchAction = playerInput.actions.FindAction(crouchActionReference.action.id);

            // Validate actions
            if (_moveAction == null) Debug.LogError("Move action not found!", this);
            if (_jumpAction == null) Debug.LogError("Jump action not found!", this);
            if (_sprintAction == null) Debug.LogError("Sprint action not found!", this);
            if (_crouchAction == null) Debug.LogError("Crouch action not found!", this);
        }

        private void Update()
        {
            HandleInput();
            HandleJumpBuffer();
            HandleCrouch();
        }

        private void FixedUpdate()
        {
            CheckGrounded();
            HandleCoyoteTime();
            HandleMovement();
            HandleRotation();
            HandleJump();
            ApplyGravity();
            HandleCrouchPhysics();
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Read movement input
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            // Check sprint input
            if (_sprintAction != null)
            {
                _isSprinting = _sprintAction.IsPressed() && _moveInput.magnitude > 0.1f;
            }

            // Check crouch input toggle
            if (_crouchAction != null && _crouchAction.WasPressedThisFrame())
            {
                _isCrouching = !_isCrouching;
            }

            // Check jump input
            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
            {
                _jumpBufferTimer = jumpBufferTime;
            }

            // Track jump hold for variable height
            if (_jumpAction != null && _jumpAction.IsPressed() && _isJumping)
            {
                _jumpHoldTimer += Time.deltaTime;
            }
        }

        private void HandleJumpBuffer()
        {
            // Decrease jump buffer timer
            if (_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Ground Detection

        private void CheckGrounded()
        {
            _wasGrounded = _isGrounded;

            // Perform ground check using sphere cast
            Vector3 checkPosition = transform.position + groundCheckOffset;
            float checkRadius = _capsuleCollider.radius * 0.9f;

            if (Physics.SphereCast(checkPosition, checkRadius, Vector3.down, out RaycastHit hit, 
                groundCheckDistance + checkRadius, groundLayer))
            {
                _isGrounded = true;
                _groundNormal = hit.normal;

                // Reset jump state when landing
                if (!_wasGrounded)
                {
                    _isJumping = false;
                    _jumpHoldTimer = 0f;
                }
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
            }
        }

        private void HandleCoyoteTime()
        {
            // Update coyote timer
            if (_isGrounded)
            {
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.fixedDeltaTime;
            }
        }

        #endregion

        #region Movement

        private void HandleMovement()
        {
            // Calculate target speed based on state
            float targetSpeed = walkSpeed;
            if (_isSprinting)
            {
                targetSpeed *= sprintSpeedMultiplier;
            }
            if (_isCrouching)
            {
                targetSpeed *= crouchSpeedMultiplier;
            }

            // Calculate movement direction relative to camera
            Vector3 moveDirection = Vector3.zero;
            if (cameraTransform != null && _moveInput.magnitude > 0.01f)
            {
                // Get camera forward and right vectors (flattened to XZ plane)
                Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

                // Calculate movement direction
                moveDirection = (cameraForward * _moveInput.y + cameraRight * _moveInput.x).normalized;

                // Adjust for slope
                if (_isGrounded && Vector3.Angle(_groundNormal, Vector3.up) < maxSlopeAngle)
                {
                    moveDirection = Vector3.ProjectOnPlane(moveDirection, _groundNormal).normalized;
                }
            }
            else if (_moveInput.magnitude > 0.01f)
            {
                // Fallback to world-space movement if no camera
                moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            }

            // Calculate target velocity
            _targetVelocity = moveDirection * targetSpeed;

            // Apply acceleration/deceleration
            float speed = _moveInput.magnitude > 0.01f ? acceleration : deceleration;
            _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, speed * Time.fixedDeltaTime);

            // Apply movement to Rigidbody (preserve Y velocity)
            Vector3 newVelocity = _currentVelocity;
            newVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = newVelocity;
        }

        private void HandleRotation()
        {
            // Only rotate if there's movement input and we're not crouching
            if (_moveInput.magnitude > 0.01f && !_isCrouching)
            {
                // Calculate target rotation based on movement direction
                Vector3 moveDirection = _currentVelocity.normalized;
                
                // Only rotate on the XZ plane (ignore Y component)
                moveDirection.y = 0f;
                
                if (moveDirection.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                    
                    // Smoothly rotate towards target
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.fixedDeltaTime
                    );
                }
            }
        }

        #endregion

        #region Jump

        private void HandleJump()
        {
            // Check if we can jump (grounded or coyote time) and have buffered input
            bool canJump = (_isGrounded || _coyoteTimer > 0) && _jumpBufferTimer > 0;

            if (canJump && !_isJumping)
            {
                PerformJump();
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }
        }

        private void PerformJump()
        {
            _isJumping = true;
            _jumpHoldTimer = 0f;

            // Apply jump force
            Vector3 jumpVelocity = _rb.linearVelocity;
            jumpVelocity.y = jumpForce;
            _rb.linearVelocity = jumpVelocity;
        }

        private void ApplyGravity()
        {
            // Apply variable jump height by reducing upward velocity while holding jump
            if (_isJumping && _jumpHoldTimer < jumpHoldTime && _jumpAction != null && _jumpAction.IsPressed())
            {
                // Continue applying upward force for variable height
                _rb.AddForce(Vector3.up * Physics.gravity.y * 0.5f * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
            else if (_rb.linearVelocity.y < 0)
            {
                // Apply heavier gravity while falling
                _rb.AddForce(Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
        }

        #endregion

        #region Crouch

        private void HandleCrouch()
        {
            // Smoothly interpolate height
            float targetHeight = _isCrouching ? crouchHeight : normalHeight;
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, crouchSpeed * Time.deltaTime);

            // Check if we can stand up (raycast above)
            if (!_isCrouching && _currentHeight > crouchHeight)
            {
                float checkHeight = normalHeight - crouchHeight;
                if (Physics.SphereCast(transform.position, _capsuleCollider.radius * 0.9f, 
                    Vector3.up, out _, checkHeight, groundLayer))
                {
                    // Something is above, stay crouched
                    _isCrouching = true;
                }
            }
        }

        private void HandleCrouchPhysics()
        {
            // Update collider height
            _capsuleCollider.height = _currentHeight;

            // Adjust center to keep feet at the same position
            _capsuleCollider.center = new Vector3(0, _currentHeight / 2f, 0);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the player to jump (can be called by other systems).
        /// </summary>
        public void ForceJump()
        {
            if (_isGrounded || _coyoteTimer > 0)
            {
                PerformJump();
            }
        }

        /// <summary>
        /// Sets the crouch state externally.
        /// </summary>
        public void SetCrouch(bool crouching)
        {
            _isCrouching = crouching;
        }

        /// <summary>
        /// Adds external force to the player (e.g., from explosions, knockback).
        /// </summary>
        public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            _rb.AddForce(force, mode);
        }

        /// <summary>
        /// Teleports the player to a position.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            _rb.position = position;
            _rb.linearVelocity = Vector3.zero;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw ground check
            if (_capsuleCollider != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Vector3 checkPosition = transform.position + groundCheckOffset;
                Gizmos.DrawWireSphere(checkPosition, _capsuleCollider.radius * 0.9f);
                Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * (groundCheckDistance + _capsuleCollider.radius * 0.9f));
            }

            // Draw movement direction
            if (_currentVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + _currentVelocity);
            }
        }

        #endregion
    }
}

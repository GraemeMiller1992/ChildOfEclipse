using UnityEngine;

namespace ChildOfEclipse
{
    /// <summary>
    /// Makes the player stick to moving platforms without parenting.
    /// Tracks platform movement and applies the delta to the player using math.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlatformSticker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Detection Settings")]
        [SerializeField]
        [Tooltip("Layer mask for detecting platforms.")]
        private LayerMask _platformLayer = 1;

        [SerializeField]
        [Tooltip("Distance below the player to check for platforms.")]
        private float _groundCheckDistance = 0.2f;

        [SerializeField]
        [Tooltip("Radius of the ground check sphere.")]
        private float _groundCheckRadius = 0.3f;

        [SerializeField]
        [Tooltip("Offset from player center for ground check.")]
        private Vector3 _groundCheckOffset = new Vector3(0, -0.5f, 0);

        [Header("Sticking Settings")]
        [SerializeField]
        [Tooltip("How quickly the player snaps to platform movement (0-1).")]
        private float _stickiness = 1f;

        [SerializeField]
        [Tooltip("Whether to apply platform rotation to player.")]
        private bool _applyPlatformRotation = false;

        [SerializeField]
        [Tooltip("How quickly the player follows platform rotation.")]
        private float _rotationFollowSpeed = 10f;

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Whether to draw debug gizmos.")]
        private bool _showDebugGizmos = true;

        [SerializeField]
        [Tooltip("Color for platform detection gizmo.")]
        private Color _debugColor = Color.green;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private Transform _currentPlatform;
        private Vector3 _previousPlatformPosition;
        private Quaternion _previousPlatformRotation;
        private bool _isOnPlatform;
        private Vector3 _platformVelocity;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current platform the player is standing on.
        /// </summary>
        public Transform CurrentPlatform => _currentPlatform;

        /// <summary>
        /// Gets whether the player is currently on a moving platform.
        /// </summary>
        public bool IsOnPlatform => _isOnPlatform;

        /// <summary>
        /// Gets the current velocity of the platform.
        /// </summary>
        public Vector3 PlatformVelocity => _platformVelocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            CheckForPlatform();
            ApplyPlatformMovement();
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            // Draw ground check sphere
            Vector3 checkPosition = transform.position + _groundCheckOffset;
            Gizmos.color = _isOnPlatform ? _debugColor : Color.red;
            Gizmos.DrawWireSphere(checkPosition, _groundCheckRadius);

            // Draw line to current platform
            if (_isOnPlatform && _currentPlatform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _currentPlatform.position);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if the player is standing on a platform.
        /// </summary>
        private void CheckForPlatform()
        {
            Vector3 checkPosition = transform.position + _groundCheckOffset;

            if (Physics.SphereCast(checkPosition, _groundCheckRadius, Vector3.down, 
                out RaycastHit hit, _groundCheckDistance + _groundCheckRadius, _platformLayer))
            {
                Transform hitTransform = hit.collider.transform;

                // Check if this is a new platform
                if (_currentPlatform != hitTransform)
                {
                    // Transitioning to a new platform
                    if (_currentPlatform != null)
                    {
                        OnPlatformExit();
                    }
                    _currentPlatform = hitTransform;
                    _previousPlatformPosition = _currentPlatform.position;
                    _previousPlatformRotation = _currentPlatform.rotation;
                    OnPlatformEnter();
                }
                else
                {
                    // Still on the same platform
                    _isOnPlatform = true;
                }
            }
            else
            {
                // Not on any platform
                if (_isOnPlatform)
                {
                    OnPlatformExit();
                }
            }
        }

        /// <summary>
        /// Applies the platform's movement to the player.
        /// </summary>
        private void ApplyPlatformMovement()
        {
            if (!_isOnPlatform || _currentPlatform == null)
            {
                _platformVelocity = Vector3.zero;
                return;
            }

            // Calculate platform delta movement
            Vector3 platformDelta = _currentPlatform.position - _previousPlatformPosition;
            
            // Calculate platform velocity for external use
            _platformVelocity = platformDelta / Time.fixedDeltaTime;

            // Apply platform movement to player
            if (platformDelta.magnitude > 0.001f)
            {
                // Use MovePosition for smooth movement that respects physics
                Vector3 targetPosition = _rb.position + platformDelta * _stickiness;
                _rb.MovePosition(targetPosition);
            }

            // Apply platform rotation if enabled
            if (_applyPlatformRotation)
            {
                Quaternion platformRotationDelta = _currentPlatform.rotation * Quaternion.Inverse(_previousPlatformRotation);
                if (platformRotationDelta != Quaternion.identity)
                {
                    Quaternion targetRotation = platformRotationDelta * _rb.rotation;
                    _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, 
                        _rotationFollowSpeed * Time.fixedDeltaTime));
                }
            }

            // Update previous platform position for next frame
            _previousPlatformPosition = _currentPlatform.position;
            _previousPlatformRotation = _currentPlatform.rotation;
        }

        /// <summary>
        /// Called when the player enters a platform.
        /// </summary>
        private void OnPlatformEnter()
        {
            _isOnPlatform = true;
            _previousPlatformPosition = _currentPlatform.position;
            _previousPlatformRotation = _currentPlatform.rotation;
        }

        /// <summary>
        /// Called when the player exits a platform.
        /// </summary>
        private void OnPlatformExit()
        {
            _isOnPlatform = false;
            _currentPlatform = null;
            _platformVelocity = Vector3.zero;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually sets the platform layer for detection.
        /// </summary>
        public void SetPlatformLayer(LayerMask layer)
        {
            _platformLayer = layer;
        }

        /// <summary>
        /// Forces the player to exit the current platform.
        /// </summary>
        public void ForceExitPlatform()
        {
            if (_isOnPlatform)
            {
                OnPlatformExit();
            }
        }

        /// <summary>
        /// Gets the world position of the platform directly below the player.
        /// </summary>
        /// <returns>The platform position, or player position if no platform.</returns>
        public Vector3 GetPlatformSurfacePosition()
        {
            if (!_isOnPlatform || _currentPlatform == null)
            {
                return transform.position;
            }

            Vector3 checkPosition = transform.position + _groundCheckOffset;
            if (Physics.SphereCast(checkPosition, _groundCheckRadius, Vector3.down, 
                out RaycastHit hit, _groundCheckDistance + _groundCheckRadius, _platformLayer))
            {
                return hit.point;
            }

            return transform.position;
        }

        #endregion
    }
}

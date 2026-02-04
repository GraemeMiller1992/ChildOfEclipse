using UnityEngine;

namespace World
{
    /// <summary>
    /// Controls a platform that moves between multiple waypoints.
    /// Supports different movement modes: Loop, PingPong, and Once.
    /// Players and other objects can stand on the platform and move with it.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Defines how the platform moves between waypoints.
        /// </summary>
        public enum MovementMode
        {
            /// <summary>Moves from first to last waypoint, then returns to first.</summary>
            Loop,
            /// <summary>Moves back and forth between first and last waypoint.</summary>
            PingPong,
            /// <summary>Moves from first to last waypoint, then stops.</summary>
            Once
        }

        #endregion

        #region Serialized Fields

        [Header("Waypoint Settings")]
        [SerializeField]
        [Tooltip("The waypoints the platform will move between. If empty, the platform will not move.")]
        private Transform[] _waypoints;

        [Header("Movement Settings")]
        [SerializeField]
        [Tooltip("The movement mode to use.")]
        private MovementMode _movementMode = MovementMode.Loop;

        [SerializeField]
        [Tooltip("Speed at which the platform moves between waypoints.")]
        private float _moveSpeed = 3f;

        [SerializeField]
        [Tooltip("Whether to start moving immediately on Awake.")]
        private bool _startOnAwake = true;

        [Header("Delay Settings")]
        [SerializeField]
        [Tooltip("Delay before the platform starts moving (in seconds).")]
        private float _startDelay = 0f;

        [Header("Stop Settings")]
        [SerializeField]
        [Tooltip("Whether the platform should stop at each waypoint before moving to the next.")]
        private bool _stopAtWaypoints = true;

        [SerializeField]
        [Tooltip("Time to stop at each waypoint (in seconds).")]
        private float _stopTime = 1f;

        [Header("Movement Settings")]
        [SerializeField]
        [Tooltip("Distance threshold to consider the platform at a waypoint.")]
        private float _arrivalThreshold = 0.1f;

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Whether to draw debug lines showing the movement path.")]
        private bool _showDebugLines = true;

        [SerializeField]
        [Tooltip("Color of the debug lines.")]
        private Color _debugLineColor = Color.cyan;

        [SerializeField]
        [Tooltip("Whether to draw spheres at waypoint positions.")]
        private bool _showWaypointSpheres = true;

        [SerializeField]
        [Tooltip("Size of the waypoint spheres.")]
        private float _waypointSphereSize = 0.5f;

        [SerializeField]
        [Tooltip("Color of the waypoint spheres.")]
        private Color _waypointSphereColor = Color.yellow;

        #endregion

        #region Private Fields

        private int _currentWaypointIndex = 0;
        private bool _isMoving = false;
        private bool _isStopped = false;
        private bool _isReversing = false; // For PingPong mode
        private float _stopTimer = 0f;
        private float _startDelayTimer = 0f;
        private bool _hasStarted = false;
        private Vector3 _velocity;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the platform is currently moving.
        /// </summary>
        public bool IsMoving
        {
            get => _isMoving;
            set
            {
                _isMoving = value;
                if (!_isMoving)
                {
                    _velocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Gets the current waypoint index the platform is moving toward.
        /// </summary>
        public int CurrentWaypointIndex => _currentWaypointIndex;

        /// <summary>
        /// Gets the total number of waypoints.
        /// </summary>
        public int WaypointCount => _waypoints?.Length ?? 0;

        /// <summary>
        /// Gets the current movement mode.
        /// </summary>
        public MovementMode CurrentMovementMode => _movementMode;

        /// <summary>
        /// Gets the current velocity of the platform.
        /// </summary>
        public Vector3 Velocity => _velocity;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"MovingPlatform: No waypoints assigned to {gameObject.name}. Platform will not move.", this);
                return;
            }

            if (_startOnAwake)
            {
                StartMovement();
            }
        }

        private void FixedUpdate()
        {
            if (!_hasStarted)
            {
                HandleStartDelay();
                return;
            }

            if (!_isMoving || _waypoints == null || _waypoints.Length == 0)
            {
                _velocity = Vector3.zero;
                return;
            }

            // Handle stopping at waypoint
            if (_isStopped)
            {
                _stopTimer -= Time.fixedDeltaTime;
                _velocity = Vector3.zero;
                if (_stopTimer <= 0f)
                {
                    _isStopped = false;
                }
                return;
            }

            MoveTowardsWaypoint();
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugLines || _waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            // Draw waypoint spheres
            if (_showWaypointSpheres)
            {
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] != null)
                    {
                        Gizmos.color = i == _currentWaypointIndex ? Color.green : _waypointSphereColor;
                        Gizmos.DrawWireSphere(_waypoints[i].position, _waypointSphereSize);
                        
                        // Draw waypoint index
#if UNITY_EDITOR
                        UnityEditor.Handles.Label(_waypoints[i].position + Vector3.up * _waypointSphereSize, i.ToString());
#endif
                    }
                }
            }

            // Draw movement path
            Gizmos.color = _debugLineColor;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;

                int nextIndex = GetNextWaypointIndex(i);
                if (_waypoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the platform movement.
        /// </summary>
        public void StartMovement()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"MovingPlatform: Cannot start movement - no waypoints assigned.", this);
                return;
            }

            _isMoving = true;
            _hasStarted = false;
            _startDelayTimer = _startDelay;
        }

        /// <summary>
        /// Stops the platform movement.
        /// </summary>
        public void StopMovement()
        {
            _isMoving = false;
            _isStopped = false;
            _velocity = Vector3.zero;
        }

        /// <summary>
        /// Pauses the platform movement at its current position.
        /// </summary>
        public void PauseMovement()
        {
            _isMoving = false;
        }

        /// <summary>
        /// Resumes the platform movement from its current position.
        /// </summary>
        public void ResumeMovement()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"MovingPlatform: Cannot resume movement - no waypoints assigned.", this);
                return;
            }

            _isMoving = true;
        }

        /// <summary>
        /// Sets the movement mode.
        /// </summary>
        /// <param name="mode">The new movement mode.</param>
        public void SetMovementMode(MovementMode mode)
        {
            _movementMode = mode;
            _isReversing = false;
        }

        /// <summary>
        /// Sets the move speed.
        /// </summary>
        /// <param name="speed">The new move speed.</param>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Adds a waypoint to the movement path.
        /// </summary>
        /// <param name="waypoint">The waypoint transform to add.</param>
        public void AddWaypoint(Transform waypoint)
        {
            if (waypoint == null)
            {
                Debug.LogWarning("MovingPlatform: Cannot add null waypoint.", this);
                return;
            }

            if (_waypoints == null)
            {
                _waypoints = new Transform[] { waypoint };
            }
            else
            {
                System.Array.Resize(ref _waypoints, _waypoints.Length + 1);
                _waypoints[_waypoints.Length - 1] = waypoint;
            }
        }

        /// <summary>
        /// Removes a waypoint from the movement path.
        /// </summary>
        /// <param name="index">The index of the waypoint to remove.</param>
        public void RemoveWaypoint(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length)
            {
                Debug.LogWarning($"MovingPlatform: Invalid waypoint index {index}.", this);
                return;
            }

            Transform[] newWaypoints = new Transform[_waypoints.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (i != index)
                {
                    newWaypoints[newIndex++] = _waypoints[i];
                }
            }
            _waypoints = newWaypoints;

            // Adjust current index if necessary
            if (_currentWaypointIndex >= _waypoints.Length)
            {
                _currentWaypointIndex = Mathf.Max(0, _waypoints.Length - 1);
            }
        }

        /// <summary>
        /// Clears all waypoints.
        /// </summary>
        public void ClearWaypoints()
        {
            _waypoints = new Transform[0];
            StopMovement();
        }

        /// <summary>
        /// Sets the current waypoint index and immediately moves to that waypoint.
        /// </summary>
        /// <param name="index">The waypoint index to move to.</param>
        public void SetCurrentWaypoint(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length)
            {
                Debug.LogWarning($"MovingPlatform: Invalid waypoint index {index}.", this);
                return;
            }

            _currentWaypointIndex = index;
            _isStopped = false;
        }

        /// <summary>
        /// Gets the waypoint at the specified index.
        /// </summary>
        /// <param name="index">The index of the waypoint.</param>
        /// <returns>The waypoint transform, or null if index is invalid.</returns>
        public Transform GetWaypoint(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length)
            {
                return null;
            }
            return _waypoints[index];
        }

        /// <summary>
        /// Teleports the platform to a specific waypoint instantly.
        /// </summary>
        /// <param name="index">The waypoint index to teleport to.</param>
        public void TeleportToWaypoint(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length)
            {
                Debug.LogWarning($"MovingPlatform: Invalid waypoint index {index}.", this);
                return;
            }

            Transform targetWaypoint = _waypoints[index];
            if (targetWaypoint != null)
            {
                transform.position = targetWaypoint.position;
                _currentWaypointIndex = index;
                _velocity = Vector3.zero;
                _isStopped = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the start delay before the platform begins moving.
        /// </summary>
        private void HandleStartDelay()
        {
            _startDelayTimer -= Time.fixedDeltaTime;
            if (_startDelayTimer <= 0f)
            {
                _hasStarted = true;
                _isMoving = true;
            }
        }

        /// <summary>
        /// Moves the platform towards the current waypoint.
        /// </summary>
        private void MoveTowardsWaypoint()
        {
            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint == null)
            {
                Debug.LogWarning($"MovingPlatform: Waypoint at index {_currentWaypointIndex} is null. Skipping to next.", this);
                AdvanceToNextWaypoint();
                return;
            }

            Vector3 direction = (targetWaypoint.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetWaypoint.position);

            if (distance <= _arrivalThreshold)
            {
                // Arrived at waypoint
                transform.position = targetWaypoint.position;
                _velocity = Vector3.zero;

                if (_stopAtWaypoints)
                {
                    StartStopping();
                }
                else
                {
                    AdvanceToNextWaypoint();
                }
            }
            else
            {
                // Move towards waypoint
                _velocity = direction * _moveSpeed;
                transform.position = transform.position + _velocity * Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// Advances to the next waypoint based on the movement mode.
        /// </summary>
        private void AdvanceToNextWaypoint()
        {
            // Check if we should stop (Once mode at last waypoint)
            if (_movementMode == MovementMode.Once && 
                _currentWaypointIndex == _waypoints.Length - 1 && 
                !_isReversing)
            {
                _isMoving = false;
                return;
            }

            _currentWaypointIndex = GetNextWaypointIndex(_currentWaypointIndex);
        }

        /// <summary>
        /// Gets the next waypoint index based on the movement mode.
        /// </summary>
        /// <param name="currentIndex">The current waypoint index.</param>
        /// <returns>The next waypoint index.</returns>
        private int GetNextWaypointIndex(int currentIndex)
        {
            return _movementMode switch
            {
                MovementMode.Loop => (currentIndex + 1) % _waypoints.Length,
                MovementMode.PingPong => GetPingPongNextIndex(currentIndex),
                MovementMode.Once => GetOnceNextIndex(currentIndex),
                _ => (currentIndex + 1) % _waypoints.Length
            };
        }

        /// <summary>
        /// Gets the next index for PingPong mode.
        /// </summary>
        /// <param name="currentIndex">The current waypoint index.</param>
        /// <returns>The next waypoint index.</returns>
        private int GetPingPongNextIndex(int currentIndex)
        {
            if (_isReversing)
            {
                if (currentIndex <= 0)
                {
                    _isReversing = false;
                    return 1;
                }
                return currentIndex - 1;
            }
            else
            {
                if (currentIndex >= _waypoints.Length - 1)
                {
                    _isReversing = true;
                    return _waypoints.Length - 2;
                }
                return currentIndex + 1;
            }
        }

        /// <summary>
        /// Gets the next index for Once mode.
        /// </summary>
        /// <param name="currentIndex">The current waypoint index.</param>
        /// <returns>The next waypoint index.</returns>
        private int GetOnceNextIndex(int currentIndex)
        {
            if (currentIndex >= _waypoints.Length - 1)
            {
                return currentIndex; // Stay at last waypoint
            }
            return currentIndex + 1;
        }

        /// <summary>
        /// Starts the stop timer at the current waypoint.
        /// </summary>
        private void StartStopping()
        {
            _isStopped = true;
            _stopTimer = _stopTime;
            AdvanceToNextWaypoint();
        }

        #endregion
    }
}

using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// Controls a NavMeshAgent to patrol between multiple waypoints.
    /// Supports different patrol modes: Loop, PingPong, and Random.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshAgentPatrol : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Defines how the agent moves between waypoints.
        /// </summary>
        public enum PatrolMode
        {
            /// <summary>Moves from first to last waypoint, then returns to first.</summary>
            Loop,
            /// <summary>Moves back and forth between first and last waypoint.</summary>
            PingPong,
            /// <summary>Moves to a random waypoint each time.</summary>
            Random
        }

        #endregion

        #region Serialized Fields

        [Header("Waypoint Settings")]
        [SerializeField]
        [Tooltip("The waypoints the agent will patrol between. If empty, the agent will not patrol.")]
        private Transform[] _waypoints;

        [Header("Patrol Settings")]
        [SerializeField]
        [Tooltip("The patrol mode to use.")]
        private PatrolMode _patrolMode = PatrolMode.Loop;

        [SerializeField]
        [Tooltip("Whether to shuffle the waypoints on start for Random mode.")]
        private bool _shuffleOnStart = false;

        [SerializeField]
        [Tooltip("Whether to start patrolling immediately on Awake.")]
        private bool _startOnAwake = true;

        [Header("Wait Settings")]
        [SerializeField]
        [Tooltip("Whether the agent should wait at each waypoint before moving to the next.")]
        private bool _waitAtWaypoints = true;

        [SerializeField]
        [Tooltip("Minimum time to wait at each waypoint.")]
        private float _minWaitTime = 1f;

        [SerializeField]
        [Tooltip("Maximum time to wait at each waypoint. If equal to minWaitTime, wait time is fixed.")]
        private float _maxWaitTime = 2f;

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Whether to draw debug lines showing the patrol path.")]
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

        private NavMeshAgent _navAgent;
        private int _currentWaypointIndex = 0;
        private bool _isWaiting = false;
        private float _waitTimer = 0f;
        private bool _isPatrolling = false;
        private bool _isReversing = false; // For PingPong mode

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the agent is currently patrolling.
        /// </summary>
        public bool IsPatrolling
        {
            get => _isPatrolling;
            set
            {
                _isPatrolling = value;
                if (_isPatrolling && !_isWaiting)
                {
                    SetNextDestination();
                }
                else if (!_isPatrolling)
                {
                    _navAgent.isStopped = true;
                }
            }
        }

        /// <summary>
        /// Gets the current waypoint index the agent is moving toward.
        /// </summary>
        public int CurrentWaypointIndex => _currentWaypointIndex;

        /// <summary>
        /// Gets the total number of waypoints.
        /// </summary>
        public int WaypointCount => _waypoints?.Length ?? 0;

        /// <summary>
        /// Gets the current patrol mode.
        /// </summary>
        public PatrolMode CurrentPatrolMode => _patrolMode;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();

            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"NavMeshAgentPatrol: No waypoints assigned to {gameObject.name}. Agent will not patrol.", this);
                return;
            }

            // Shuffle waypoints if enabled and in Random mode
            if (_shuffleOnStart && _patrolMode == PatrolMode.Random)
            {
                ShuffleWaypoints();
            }

            if (_startOnAwake)
            {
                StartPatrol();
            }
        }

        private void Update()
        {
            if (!_isPatrolling || _navAgent == null || _waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            // Handle waiting at waypoint
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    SetNextDestination();
                }
                return;
            }

            // Check if agent has reached the current waypoint
            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                if (_waitAtWaypoints)
                {
                    StartWaiting();
                }
                else
                {
                    SetNextDestination();
                }
            }
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
                        Gizmos.DrawSphere(_waypoints[i].position, _waypointSphereSize);
                        
                        // Draw waypoint index
#if UNITY_EDITOR
                        UnityEditor.Handles.Label(_waypoints[i].position + Vector3.up * _waypointSphereSize, i.ToString());
#endif
                    }
                }
            }

            // Draw patrol path
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
        /// Starts the patrol behavior.
        /// </summary>
        public void StartPatrol()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"NavMeshAgentPatrol: Cannot start patrol - no waypoints assigned.", this);
                return;
            }

            _isPatrolling = true;
            _navAgent.isStopped = false;
            _isWaiting = false;
            SetNextDestination();
        }

        /// <summary>
        /// Stops the patrol behavior.
        /// </summary>
        public void StopPatrol()
        {
            _isPatrolling = false;
            _navAgent.isStopped = true;
            _isWaiting = false;
        }

        /// <summary>
        /// Sets the patrol mode.
        /// </summary>
        /// <param name="mode">The new patrol mode.</param>
        public void SetPatrolMode(PatrolMode mode)
        {
            _patrolMode = mode;
            _isReversing = false;
        }

        /// <summary>
        /// Adds a waypoint to the patrol path.
        /// </summary>
        /// <param name="waypoint">The waypoint transform to add.</param>
        public void AddWaypoint(Transform waypoint)
        {
            if (waypoint == null)
            {
                Debug.LogWarning("NavMeshAgentPatrol: Cannot add null waypoint.", this);
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
        /// Removes a waypoint from the patrol path.
        /// </summary>
        /// <param name="index">The index of the waypoint to remove.</param>
        public void RemoveWaypoint(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length)
            {
                Debug.LogWarning($"NavMeshAgentPatrol: Invalid waypoint index {index}.", this);
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
            StopPatrol();
        }

        /// <summary>
        /// Sets the current waypoint index and immediately moves to that waypoint.
        /// </summary>
        /// <param name="index">The waypoint index to move to.</param>
        public void SetCurrentWaypoint(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length)
            {
                Debug.LogWarning($"NavMeshAgentPatrol: Invalid waypoint index {index}.", this);
                return;
            }

            _currentWaypointIndex = index;
            _isWaiting = false;
            SetNextDestination();
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the next destination for the NavMeshAgent.
        /// </summary>
        private void SetNextDestination()
        {
            if (_waypoints == null || _waypoints.Length == 0 || _navAgent == null)
            {
                return;
            }

            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint == null)
            {
                Debug.LogWarning($"NavMeshAgentPatrol: Waypoint at index {_currentWaypointIndex} is null. Skipping to next.", this);
                AdvanceToNextWaypoint();
                SetNextDestination();
                return;
            }

            _navAgent.SetDestination(targetWaypoint.position);
            _navAgent.isStopped = false;
        }

        /// <summary>
        /// Advances to the next waypoint based on the patrol mode.
        /// </summary>
        private void AdvanceToNextWaypoint()
        {
            _currentWaypointIndex = GetNextWaypointIndex(_currentWaypointIndex);
        }

        /// <summary>
        /// Gets the next waypoint index based on the patrol mode.
        /// </summary>
        /// <param name="currentIndex">The current waypoint index.</param>
        /// <returns>The next waypoint index.</returns>
        private int GetNextWaypointIndex(int currentIndex)
        {
            return _patrolMode switch
            {
                PatrolMode.Loop => (currentIndex + 1) % _waypoints.Length,
                PatrolMode.PingPong => GetPingPongNextIndex(currentIndex),
                PatrolMode.Random => Random.Range(0, _waypoints.Length),
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
        /// Starts the wait timer at the current waypoint.
        /// </summary>
        private void StartWaiting()
        {
            _isWaiting = true;
            _navAgent.isStopped = true;
            _waitTimer = Random.Range(_minWaitTime, _maxWaitTime);
            AdvanceToNextWaypoint();
        }

        /// <summary>
        /// Shuffles the waypoints array using Fisher-Yates shuffle algorithm.
        /// </summary>
        private void ShuffleWaypoints()
        {
            if (_waypoints == null || _waypoints.Length < 2)
            {
                return;
            }

            for (int i = _waypoints.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_waypoints[i], _waypoints[j]) = (_waypoints[j], _waypoints[i]);
            }
        }

        #endregion
    }
}

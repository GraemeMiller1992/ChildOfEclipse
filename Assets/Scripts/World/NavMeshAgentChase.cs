using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// Controls a NavMeshAgent to chase a target when detected.
    /// Supports field of view detection and configurable chase behavior.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshAgentChase : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Target Settings")]
        [SerializeField]
        [Tooltip("The target transform to chase.")]
        private Transform _target;

        [SerializeField]
        [Tooltip("Tag to automatically find the target. If set, will search for GameObject with this tag.")]
        private string _targetTag = "Player";

        [Header("Detection Settings")]
        [SerializeField]
        [Tooltip("Maximum distance at which the target can be detected.")]
        private float _detectionRange = 15f;

        [SerializeField]
        [Tooltip("Field of view angle in degrees. 360 means omnidirectional detection.")]
        [Range(0f, 360f)]
        private float _fieldOfView = 90f;

        [SerializeField]
        [Tooltip("Layer mask for raycasting to check line of sight.")]
        private LayerMask _obstacleLayers;

        [SerializeField]
        [Tooltip("Whether to check line of sight before detecting the target.")]
        private bool _requireLineOfSight = true;

        [Header("Chase Settings")]
        [SerializeField]
        [Tooltip("Speed multiplier when chasing. 1.0 = normal speed.")]
        private float _chaseSpeedMultiplier = 1.5f;

        [SerializeField]
        [Tooltip("Angular speed multiplier when chasing.")]
        private float _chaseAngularSpeedMultiplier = 2f;

        [SerializeField]
        [Tooltip("Distance at which to stop chasing and consider target reached.")]
        private float _stopDistance = 2f;

        [SerializeField]
        [Tooltip("Whether to stop chasing when the target is out of range.")]
        private bool _stopWhenOutOfRange = true;

        [SerializeField]
        [Tooltip("Time to wait after losing target before stopping chase.")]
        private float _loseTargetDelay = 2f;

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Whether to draw debug visualization.")]
        private bool _showDebugGizmos = true;

        [SerializeField]
        [Tooltip("Color for detection range gizmo.")]
        private Color _detectionRangeColor = new Color(1f, 0f, 0f, 0.2f);

        [SerializeField]
        [Tooltip("Color for field of view gizmo.")]
        private Color _fieldOfViewColor = new Color(1f, 1f, 0f, 0.3f);

        [SerializeField]
        [Tooltip("Color for line of sight gizmo.")]
        private Color _lineOfSightColor = Color.green;

        #endregion

        #region Private Fields

        private NavMeshAgent _navAgent;
        private bool _isChasing = false;
        private bool _hasTarget = false;
        private float _loseTargetTimer = 0f;
        private float _originalSpeed;
        private float _originalAngularSpeed;
        private EnemyAI _enemyAI;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the target is detected.
        /// </summary>
        public event System.Action OnTargetDetected;

        /// <summary>
        /// Fired when the target is lost.
        /// </summary>
        public event System.Action OnTargetLost;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the target to chase.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Gets whether the agent is currently chasing.
        /// </summary>
        public bool IsChasing => _isChasing;

        /// <summary>
        /// Gets whether the target is currently detected.
        /// </summary>
        public bool HasTarget => _hasTarget;

        /// <summary>
        /// Gets the distance to the target.
        /// </summary>
        public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();

            // Get EnemyAI if present
            _enemyAI = GetComponent<EnemyAI>();

            // Store original speed values
            _originalSpeed = _navAgent.speed;
            _originalAngularSpeed = _navAgent.angularSpeed;

            // Find target by tag if not assigned
            if (_target == null && !string.IsNullOrEmpty(_targetTag))
            {
                GameObject targetObj = GameObject.FindGameObjectWithTag(_targetTag);
                if (targetObj != null)
                {
                    _target = targetObj.transform;
                }
            }
        }

        private void Update()
        {
            // Skip chase updates if EnemyAI has override active
            if (_enemyAI != null && _enemyAI.IsStoppedOverride)
            {
                return;
            }

            if (_target == null)
            {
                if (_isChasing)
                {
                    StopChase();
                }
                return;
            }

            bool targetDetected = DetectTarget();

            if (targetDetected)
            {
                if (!_hasTarget)
                {
                    // Target was just detected
                    _hasTarget = true;
                    _loseTargetTimer = 0f;
                    OnTargetDetected?.Invoke();
                }

                if (!_isChasing)
                {
                    StartChase();
                }

                // Update destination while chasing
                if (_isChasing)
                {
                    UpdateChase();
                }
            }
            else
            {
                if (_hasTarget)
                {
                    _loseTargetTimer += Time.deltaTime;

                    if (_loseTargetTimer >= _loseTargetDelay)
                    {
                        // Target was just lost
                        _hasTarget = false;
                        OnTargetLost?.Invoke();
                        if (_stopWhenOutOfRange)
                        {
                            StopChase();
                        }
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos)
            {
                return;
            }

            // Draw detection range
            Gizmos.color = _detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // Draw field of view
            if (_fieldOfView < 360f)
            {
                Gizmos.color = _fieldOfViewColor;
                Vector3 forward = transform.forward;
                Vector3 leftDirection = Quaternion.Euler(0, -_fieldOfView / 2f, 0) * forward;
                Vector3 rightDirection = Quaternion.Euler(0, _fieldOfView / 2f, 0) * forward;

                Gizmos.DrawLine(transform.position, transform.position + leftDirection * _detectionRange);
                Gizmos.DrawLine(transform.position, transform.position + rightDirection * _detectionRange);

                // Draw field of view arc
                int segments = 20;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = -_fieldOfView / 2f + (_fieldOfView / segments) * i;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
                    Vector3 nextDirection = Quaternion.Euler(0, angle + (_fieldOfView / segments), 0) * forward;
                    
                    Vector3 point1 = transform.position + direction * _detectionRange;
                    Vector3 point2 = transform.position + nextDirection * _detectionRange;
                    
                    Gizmos.DrawLine(point1, point2);
                }
            }

            // Draw line of sight to target
            if (_target != null && _hasTarget)
            {
                Gizmos.color = _lineOfSightColor;
                Gizmos.DrawLine(transform.position, _target.position);
            }

            // Draw stop distance
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _stopDistance);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts chasing the target.
        /// </summary>
        public void StartChase()
        {
            if (_target == null)
            {
                Debug.LogWarning("NavMeshAgentChase: Cannot start chase - no target assigned.", this);
                return;
            }

            _isChasing = true;
            _navAgent.isStopped = false;
            
            // Apply chase speed modifiers
            _navAgent.speed = _originalSpeed * _chaseSpeedMultiplier;
            _navAgent.angularSpeed = _originalAngularSpeed * _chaseAngularSpeedMultiplier;
            
            UpdateChase();
        }

        /// <summary>
        /// Stops chasing the target.
        /// </summary>
        public void StopChase()
        {
            _isChasing = false;
            _navAgent.isStopped = true;
            
            // Restore original speed values
            _navAgent.speed = _originalSpeed;
            _navAgent.angularSpeed = _originalAngularSpeed;
        }

        /// <summary>
        /// Sets the target to chase.
        /// </summary>
        /// <param name="target">The target transform.</param>
        public void SetTarget(Transform target)
        {
            _target = target;
            _hasTarget = false;
            _loseTargetTimer = 0f;
        }

        /// <summary>
        /// Sets the target to chase by finding a GameObject with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to search for.</param>
        public void SetTargetByTag(string tag)
        {
            _targetTag = tag;
            GameObject targetObj = GameObject.FindGameObjectWithTag(tag);
            if (targetObj != null)
            {
                _target = targetObj.transform;
            }
            else
            {
                _target = null;
            }
            _hasTarget = false;
            _loseTargetTimer = 0f;
        }

        /// <summary>
        /// Checks if the target is within attack range.
        /// </summary>
        /// <param name="attackRange">The attack range to check against.</param>
        /// <returns>True if target is within attack range.</returns>
        public bool IsTargetInAttackRange(float attackRange)
        {
            return _hasTarget && DistanceToTarget <= attackRange;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Detects if the target is visible and within range.
        /// </summary>
        /// <returns>True if target is detected.</returns>
        private bool DetectTarget()
        {
            if (_target == null)
            {
                return false;
            }

            float distance = DistanceToTarget;

            // Check if target is within detection range
            if (distance > _detectionRange)
            {
                return false;
            }

            // Check field of view
            if (_fieldOfView < 360f)
            {
                Vector3 directionToTarget = (_target.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget > _fieldOfView / 2f)
                {
                    return false;
                }
            }

            // Check line of sight
            if (_requireLineOfSight)
            {
                if (!HasLineOfSight())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if there's a clear line of sight to the target.
        /// </summary>
        /// <returns>True if line of sight is clear.</returns>
        private bool HasLineOfSight()
        {
            if (_target == null)
            {
                return false;
            }

            Vector3 direction = (_target.position - transform.position).normalized;
            float distance = DistanceToTarget;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, distance, _obstacleLayers))
            {
                // Something is blocking the view
                return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the chase destination.
        /// </summary>
        private void UpdateChase()
        {
            if (_target == null || _navAgent == null)
            {
                return;
            }

            float distance = DistanceToTarget;

            // Check if we've reached the stop distance
            if (distance <= _stopDistance)
            {
                _navAgent.isStopped = true;
            }
            else
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(_target.position);
            }
        }

        #endregion
    }
}

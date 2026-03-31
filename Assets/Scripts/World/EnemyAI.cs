using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// Unified enemy AI that combines Patrol, Chase, Attack, and Flee behaviors into a single component.
    /// Uses bool flags to enable/disable each feature independently.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Defines the possible AI states.
        /// </summary>
        public enum AIState
        {
            /// <summary>Enemy is patrolling between waypoints.</summary>
            Patrol,
            /// <summary>Enemy is chasing the target.</summary>
            Chase,
            /// <summary>Enemy is attacking the target.</summary>
            Attack,
            /// <summary>Enemy is fleeing from the target.</summary>
            Flee,
            /// <summary>Enemy is idle (no active behavior).</summary>
            Idle
        }

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

        /// <summary>
        /// Defines the movement type during attack.
        /// </summary>
        public enum AttackMovementType
        {
            /// <summary>No movement during attack (stationary).</summary>
            None,
            /// <summary>Quick lunge forward during attack.</summary>
            Lunge,
            /// <summary>Fast dash forward during attack.</summary>
            Dash
        }

        /// <summary>
        /// Defines the current state of the attack sequence.
        /// </summary>
        public enum AttackState
        {
            /// <summary>Not attacking, ready to attack.</summary>
            Idle,
            /// <summary>Preparing to attack (windup).</summary>
            Preparing,
            /// <summary>Executing the attack (including lunge/dash).</summary>
            Attacking,
            /// <summary>Repositioning for next attack.</summary>
            Repositioning,
            /// <summary>Waiting for cooldown before next attack.</summary>
            Cooldown
        }

        #endregion

        #region Serialized Fields

        [Header("Feature Enable/Disable")]
        [SerializeField]
        [Tooltip("Enables patrol behavior.")]
        private bool _enablePatrol = true;

        [SerializeField]
        [Tooltip("Enables chase behavior.")]
        private bool _enableChase = true;

        [SerializeField]
        [Tooltip("Enables attack behavior.")]
        private bool _enableAttack = true;

        [SerializeField]
        [Tooltip("Enables flee behavior. The enemy will run away from the target when detected.")]
        private bool _enableFlee = false;

        [Header("Target Settings")]
        [SerializeField]
        [Tooltip("The target transform to chase/attack.")]
        private Transform _target;

        [SerializeField]
        [Tooltip("Tag to automatically find the target. If set, will search for GameObject with this tag.")]
        private string _targetTag = "Player";

        [Header("Patrol Settings")]
        [SerializeField]
        [Tooltip("The waypoints the agent will patrol between. If empty, agent will not patrol.")]
        private Transform[] _waypoints;

        [SerializeField]
        [Tooltip("The patrol mode to use.")]
        private PatrolMode _patrolMode = PatrolMode.Loop;

        [SerializeField]
        [Tooltip("Whether to shuffle the waypoints on start for Random mode.")]
        private bool _shuffleOnStart = false;

        [SerializeField]
        [Tooltip("Whether to start patrolling immediately on Awake.")]
        private bool _startPatrolOnAwake = true;

        [SerializeField]
        [Tooltip("Whether the agent should wait at each waypoint before moving to the next.")]
        private bool _waitAtWaypoints = true;

        [SerializeField]
        [Tooltip("Minimum time to wait at each waypoint.")]
        private float _minWaitTime = 1f;

        [SerializeField]
        [Tooltip("Maximum time to wait at each waypoint. If equal to minWaitTime, wait time is fixed.")]
        private float _maxWaitTime = 2f;

        [SerializeField]
        [Tooltip("Whether to draw debug lines showing the patrol path.")]
        private bool _showPatrolDebugLines = true;

        [SerializeField]
        [Tooltip("Color of the patrol debug lines.")]
        private Color _patrolDebugLineColor = Color.cyan;

        [Header("Chase Detection Settings")]
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

        [Header("Chase Movement Settings")]
        [SerializeField]
        [Tooltip("Speed multiplier when chasing. 1.0 = normal speed.")]
        private float _chaseSpeedMultiplier = 1.5f;

        [SerializeField]
        [Tooltip("Angular speed multiplier when chasing.")]
        private float _chaseAngularSpeedMultiplier = 2f;

        [SerializeField]
        [Tooltip("Distance at which to stop chasing and consider target reached.")]
        private float _chaseStopDistance = 2f;

        [SerializeField]
        [Tooltip("Whether to stop chasing when the target is out of range.")]
        private bool _stopChaseWhenOutOfRange = true;

        [SerializeField]
        [Tooltip("Time to wait after losing target before stopping chase.")]
        private float _loseTargetDelay = 2f;

        [SerializeField]
        [Tooltip("Whether to draw debug visualization for chase.")]
        private bool _showChaseDebugGizmos = true;

        [SerializeField]
        [Tooltip("Color for detection range gizmo.")]
        private Color _detectionRangeColor = new Color(1f, 0f, 0f, 0.2f);

        [SerializeField]
        [Tooltip("Color for field of view gizmo.")]
        private Color _fieldOfViewColor = new Color(1f, 1f, 0f, 0.3f);

        [SerializeField]
        [Tooltip("Color for line of sight gizmo.")]
        private Color _lineOfSightColor = Color.green;

        [Header("Attack Settings")]
        [SerializeField]
        [Tooltip("Range at which the enemy can attack.")]
        private float _attackRange = 2f;

        [SerializeField]
        [Tooltip("Damage dealt per attack.")]
        private float _damage = 10f;

        [SerializeField]
        [Tooltip("Time between attacks.")]
        private float _attackCooldown = 1f;

        [SerializeField]
        [Tooltip("Whether to look at the target before attacking.")]
        private bool _lookAtTarget = true;

        [SerializeField]
        [Tooltip("Rotation speed when looking at target.")]
        private float _rotationSpeed = 10f;

        [Header("Attack Movement")]
        [SerializeField]
        [Tooltip("Type of movement during attack.")]
        private AttackMovementType _attackMovementType = AttackMovementType.None;

        [SerializeField]
        [Tooltip("Duration of the lunge/dash movement.")]
        private float _attackMovementDuration = 0.3f;

        [SerializeField]
        [Tooltip("Distance to lunge/dash forward during attack.")]
        private float _attackMovementDistance = 2f;

        [SerializeField]
        [Tooltip("Speed multiplier for lunge/dash movement.")]
        private float _attackMovementSpeedMultiplier = 3f;

        [SerializeField]
        [Tooltip("Time to wait before starting the attack (windup).")]
        private float _preparationTime = 0.2f;

        [SerializeField]
        [Tooltip("Time to wait after attack before repositioning.")]
        private float _postAttackPause = 0.3f;

        [SerializeField]
        [Tooltip("Distance to maintain from target after repositioning.")]
        private float _repositionDistance = 1.5f;

        [Header("Attack Visuals")]
        [SerializeField]
        [Tooltip("GameObject to enable/disable during attack animation.")]
        private GameObject _attackVisual;

        [SerializeField]
        [Tooltip("Duration of the attack visual.")]
        private float _attackVisualDuration = 0.3f;

        [SerializeField]
        [Tooltip("Whether to draw debug visualization for attack.")]
        private bool _showAttackDebugGizmos = true;

        [SerializeField]
        [Tooltip("Color for attack range gizmo.")]
        private Color _attackRangeColor = new Color(1f, 0f, 0f, 0.3f);

        [Header("Flee Settings")]
        [SerializeField]
        [Tooltip("Maximum distance at which the target is considered a threat and triggers fleeing.")]
        private float _fleeDetectionRange = 10f;

        [SerializeField]
        [Tooltip("How far the enemy will try to run from the target when fleeing.")]
        private float _fleeDistance = 15f;

        [SerializeField]
        [Tooltip("Distance from the target at which the enemy considers itself safe and stops fleeing.")]
        private float _fleeSafeDistance = 20f;

        [SerializeField]
        [Tooltip("Speed multiplier when fleeing. Values > 1.0 make the enemy faster while fleeing.")]
        private float _fleeSpeedMultiplier = 1.8f;

        [SerializeField]
        [Tooltip("Angular speed multiplier when fleeing.")]
        private float _fleeAngularSpeedMultiplier = 2.5f;

        [SerializeField]
        [Tooltip("Whether to stop fleeing when the target is beyond the safe distance.")]
        private bool _stopFleeWhenSafe = true;

        [SerializeField]
        [Tooltip("Time to wait after reaching safety before returning to previous behavior.")]
        private float _fleeCooldownTime = 2f;

        [SerializeField]
        [Tooltip("Whether to draw debug visualization for flee behavior.")]
        private bool _showFleeDebugGizmos = true;

        [SerializeField]
        [Tooltip("Color for flee detection range gizmo.")]
        private Color _fleeDetectionRangeColor = new Color(0f, 0f, 1f, 0.2f);

        [SerializeField]
        [Tooltip("Color for flee safe distance gizmo.")]
        private Color _fleeSafeDistanceColor = new Color(0f, 1f, 0f, 0.2f);

        [Header("AI State Settings")]
        [SerializeField]
        [Tooltip("The initial AI state.")]
        private AIState _initialState = AIState.Patrol;

        [SerializeField]
        [Tooltip("Whether to enable AI on start.")]
        private bool _enableOnStart = true;

        [SerializeField]
        [Tooltip("Whether to return to patrol after losing target.")]
        private bool _returnToPatrolOnLoseTarget = true;

        [SerializeField]
        [Tooltip("Delay before returning to patrol after losing target.")]
        private float _returnToPatrolDelay = 3f;

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Whether to log state changes to console.")]
        private bool _logStateChanges = true;

        [SerializeField]
        [Tooltip("Whether to show current state in Gizmos.")]
        private bool _showStateInGizmos = true;

        #endregion

        #region Private Fields

        private NavMeshAgent _navAgent;
        private Rigidbody _rigidbody;
        private AIState _currentState = AIState.Idle;
        private bool _isEnabled = false;
        private bool _isStoppedOverride = false;

        // Patrol fields
        private bool _isPatrolling = false;
        private int _currentWaypointIndex = 0;
        private bool _isWaitingAtWaypoint = false;
        private float _waypointWaitTimer = 0f;
        private bool _isPatrolReversing = false;

        // Chase fields
        private bool _isChasing = false;
        private bool _hasTarget = false;
        private float _loseTargetTimer = 0f;
        private bool _wasChasing = false;

        // Attack fields
        private AttackState _attackState = AttackState.Idle;
        private float _attackStateTimer = 0f;
        private float _attackVisualTimer = 0f;
        private bool _canAttack = true;
        private Vector3 _attackStartPosition;
        private Vector3 _attackTargetPosition;
        private Vector3 _attackDirection; // Direction of lunge, calculated once at attack start
        private Vector3 _attackInitialTargetPosition; // Target's position at attack start
        private float _originalSpeed;
        private float _originalAngularSpeed;

        // Flee fields
        private bool _isFleeing = false;
        private Vector3 _fleeDestination;
        private float _fleeCooldownTimer = 0f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the AI state changes.
        /// </summary>
        public event System.Action<AIState, AIState> OnStateChanged;

        /// <summary>
        /// Fired when the AI is enabled.
        /// </summary>
        public event System.Action OnAIEnabled;

        /// <summary>
        /// Fired when the AI is disabled.
        /// </summary>
        public event System.Action OnAIDisabled;

        /// <summary>
        /// Fired when the target is detected.
        /// </summary>
        public event System.Action OnTargetDetected;

        /// <summary>
        /// Fired when the target is lost.
        /// </summary>
        public event System.Action OnTargetLost;

        /// <summary>
        /// Fired when an attack starts.
        /// </summary>
        public event System.Action OnAttackStarted;

        /// <summary>
        /// Fired when an attack hits a target.
        /// </summary>
        public event System.Action<Transform> OnAttackHit;

        /// <summary>
        /// Fired when an attack ends.
        /// </summary>
        public event System.Action OnAttackEnded;

        /// <summary>
        /// Fired when the attack state changes.
        /// </summary>
        public event System.Action<AttackState, AttackState> OnAttackStateChanged;

        /// <summary>
        /// Fired when the enemy starts fleeing.
        /// </summary>
        public event System.Action OnFleeStarted;

        /// <summary>
        /// Fired when the enemy stops fleeing.
        /// </summary>
        public event System.Action OnFleeEnded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether patrol is enabled.
        /// </summary>
        public bool EnablePatrol
        {
            get => _enablePatrol;
            set => _enablePatrol = value;
        }

        /// <summary>
        /// Gets or sets whether chase is enabled.
        /// </summary>
        public bool EnableChase
        {
            get => _enableChase;
            set => _enableChase = value;
        }

        /// <summary>
        /// Gets or sets whether attack is enabled.
        /// </summary>
        public bool EnableAttack
        {
            get => _enableAttack;
            set => _enableAttack = value;
        }

        /// <summary>
        /// Gets or sets whether flee is enabled.
        /// </summary>
        public bool EnableFlee
        {
            get => _enableFlee;
            set => _enableFlee = value;
        }

        /// <summary>
        /// Gets or sets the target to chase/attack.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Gets the current AI state.
        /// </summary>
        public AIState CurrentState => _currentState;

        /// <summary>
        /// Gets whether the AI is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Gets whether the agent is currently patrolling.
        /// </summary>
        public bool IsPatrolling => _isPatrolling;

        /// <summary>
        /// Gets whether the agent is currently chasing.
        /// </summary>
        public bool IsChasing => _isChasing;

        /// <summary>
        /// Gets whether the enemy is currently attacking.
        /// </summary>
        public bool IsAttacking => _attackState == AttackState.Attacking;

        /// <summary>
        /// Gets whether the enemy is in any attack-related state.
        /// </summary>
        public bool IsInAttackSequence => _attackState != AttackState.Idle;

        /// <summary>
        /// Gets whether the enemy is currently fleeing.
        /// </summary>
        public bool IsFleeing => _isFleeing;

        /// <summary>
        /// Gets the current attack state.
        /// </summary>
        public AttackState CurrentAttackState => _attackState;

        /// <summary>
        /// Gets whether the enemy can attack (not on cooldown and not in sequence).
        /// </summary>
        public bool CanAttack => _canAttack && _attackState == AttackState.Idle;

        /// <summary>
        /// Gets whether the target is currently detected.
        /// </summary>
        public bool HasTarget => _hasTarget;

        /// <summary>
        /// Gets the distance to the target.
        /// </summary>
        public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

        /// <summary>
        /// Gets the attack cooldown progress (0 to 1).
        /// </summary>
        public float CooldownProgress => _attackState == AttackState.Cooldown ? _attackStateTimer / _attackCooldown : 1f;

        /// <summary>
        /// Gets or sets whether the AI should be stopped due to external override (e.g., solar state).
        /// When true, the AI will not control the NavMeshAgent's isStopped property.
        /// </summary>
        public bool IsStoppedOverride
        {
            get => _isStoppedOverride;
            set
            {
                if (_isStoppedOverride != value)
                {
                    _isStoppedOverride = value;

                    if (_isStoppedOverride)
                    {
                        StopAllBehaviors();
                        if (_navAgent != null)
                        {
                            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
                            _navAgent.isStopped = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _rigidbody = GetComponent<Rigidbody>();

            // Ensure NavMeshAgent is enabled
            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _originalSpeed = _navAgent.speed;
                _originalAngularSpeed = _navAgent.angularSpeed;
            }

            // Find target by tag if not assigned
            if (_target == null && !string.IsNullOrEmpty(_targetTag))
            {
                GameObject targetObj = GameObject.FindGameObjectWithTag(_targetTag);
                if (targetObj != null)
                {
                    _target = targetObj.transform;
                }
            }

            // Initialize patrol
            if (_enablePatrol && _waypoints != null && _waypoints.Length > 0)
            {
                if (_shuffleOnStart && _patrolMode == PatrolMode.Random)
                {
                    ShuffleWaypoints();
                }

                if (_startPatrolOnAwake)
                {
                    StartPatrol();
                }
            }
            else if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"EnemyAI: No waypoints assigned to {gameObject.name}. Patrol will not work.", this);
            }

            // Initialize attack visual state
            if (_attackVisual != null)
            {
                _attackVisual.SetActive(false);
            }
        }

        private void Start()
        {
            if (_enableOnStart)
            {
                EnableAI();
            }
        }

        private void Update()
        {
            if (!_isEnabled)
            {
                return;
            }

            // Enforce override state
            if (_isStoppedOverride)
            {
                if (_navAgent != null)
                {
                    _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
                    _navAgent.isStopped = true;
                }
                return;
            }

            // Update behaviors
            UpdatePatrol();
            UpdateChase();
            UpdateFlee();
            UpdateAttackSequence();
            UpdateAttackVisual();

            // Handle automatic attack mode
            if (_enableAttack && _target != null)
            {
                if (CanAttack && IsTargetInRange())
                {
                    TryAttack();
                }
            }

            // Look at target only when chasing or during attack sequence
            // During patrol or idle, don't look at the target
            if (_lookAtTarget && _target != null && (_currentState == AIState.Chase || _attackState != AttackState.Idle))
            {
                LookAtTarget();
            }

            // Handle return to patrol logic
            if (_enablePatrol && _returnToPatrolOnLoseTarget && _wasChasing && _currentState == AIState.Idle)
            {
                _loseTargetTimer += Time.deltaTime;
                if (_loseTargetTimer >= _returnToPatrolDelay)
                {
                    _loseTargetTimer = 0f;
                    _wasChasing = false;
                    ChangeState(AIState.Patrol);
                }
            }

            // Update state based on conditions
            UpdateState();
        }

        private void OnDestroy()
        {
            // Clean up
        }

        private void OnDrawGizmos()
        {
            // Draw state label
            if (_showStateInGizmos)
            {
#if UNITY_EDITOR
                // Show attack state if in attack sequence, otherwise show AI state
                string stateLabel = _attackState != AttackState.Idle
                    ? $"{_currentState} (Attacking: {_attackState})"
                    : _currentState.ToString();
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"AI State: {stateLabel}");
#endif
            }

            // Draw patrol debug
            if (_enablePatrol && _showPatrolDebugLines && _waypoints != null && _waypoints.Length > 0)
            {
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] != null)
                    {
                        Gizmos.color = i == _currentWaypointIndex ? Color.green : Color.yellow;
                        Gizmos.DrawSphere(_waypoints[i].position, 0.5f);
                        
#if UNITY_EDITOR
                        UnityEditor.Handles.Label(_waypoints[i].position + Vector3.up * 0.5f, i.ToString());
#endif
                    }
                }

                Gizmos.color = _patrolDebugLineColor;
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

            // Draw chase debug
            if (_enableChase && _showChaseDebugGizmos)
            {
                Gizmos.color = _detectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, _detectionRange);

                if (_fieldOfView < 360f)
                {
                    Gizmos.color = _fieldOfViewColor;
                    Vector3 forward = transform.forward;
                    Vector3 leftDirection = Quaternion.Euler(0, -_fieldOfView / 2f, 0) * forward;
                    Vector3 rightDirection = Quaternion.Euler(0, _fieldOfView / 2f, 0) * forward;

                    Gizmos.DrawLine(transform.position, transform.position + leftDirection * _detectionRange);
                    Gizmos.DrawLine(transform.position, transform.position + rightDirection * _detectionRange);

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

                if (_target != null && _hasTarget)
                {
                    Gizmos.color = _lineOfSightColor;
                    Gizmos.DrawLine(transform.position, _target.position);
                }

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, _chaseStopDistance);
            }

            // Draw attack debug
            if (_enableAttack && _showAttackDebugGizmos)
            {
                Gizmos.color = _attackRangeColor;
                Gizmos.DrawWireSphere(transform.position, _attackRange);

                Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, _repositionDistance);

                if (_target != null)
                {
                    Gizmos.color = IsTargetInRange() ? Color.red : Color.green;
                    Gizmos.DrawLine(transform.position, _target.position);
                }
            }

            // Draw flee debug
            if (_enableFlee && _showFleeDebugGizmos)
            {
                // Flee detection range
                Gizmos.color = _fleeDetectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, _fleeDetectionRange);

                // Flee safe distance
                Gizmos.color = _fleeSafeDistanceColor;
                Gizmos.DrawWireSphere(transform.position, _fleeSafeDistance);

                // Draw flee destination and path when fleeing
                if (_isFleeing && _target != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(transform.position, _fleeDestination);
                    Gizmos.DrawSphere(_fleeDestination, 0.5f);

                    // Draw direction away from target
                    Vector3 directionAway = (transform.position - _target.position).normalized;
                    directionAway.y = 0f;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(transform.position, directionAway * 3f);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enables the AI and starts the initial state.
        /// </summary>
        public void EnableAI()
        {
            if (_isEnabled)
            {
                return;
            }

            _isEnabled = true;
            ChangeState(_initialState);
            OnAIEnabled?.Invoke();
        }

        /// <summary>
        /// Disables the AI and stops all behaviors.
        /// </summary>
        public void DisableAI()
        {
            if (!_isEnabled)
            {
                return;
            }

            _isEnabled = false;
            StopAllBehaviors();
            _currentState = AIState.Idle;
            OnAIDisabled?.Invoke();
        }

        /// <summary>
        /// Changes the AI state to the specified state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void ChangeState(AIState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            AIState oldState = _currentState;
            _currentState = newState;

            if (_logStateChanges)
            {
                Debug.Log($"EnemyAI: {gameObject.name} changing state from {oldState} to {newState}", this);
            }

            // Stop all behaviors first
            StopAllBehaviors();

            // Start the new behavior
            switch (newState)
            {
                case AIState.Patrol:
                    if (_enablePatrol)
                    {
                        StartPatrol();
                    }
                    break;
                case AIState.Chase:
                    if (_enableChase)
                    {
                        StartChase();
                    }
                    break;
                case AIState.Attack:
                    if (_enableAttack)
                    {
                        TryAttack();
                    }
                    break;
                case AIState.Flee:
                    if (_enableFlee)
                    {
                        StartFlee();
                    }
                    break;
                case AIState.Idle:
                    // No behavior to start
                    break;
            }

            OnStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Forces a state change regardless of current conditions.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void ForceState(AIState newState)
        {
            ChangeState(newState);
        }

        /// <summary>
        /// Sets the target by tag.
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
        }

        /// <summary>
        /// Checks if the target is within attack range.
        /// </summary>
        /// <returns>True if target is within attack range.</returns>
        public bool IsTargetInRange()
        {
            return _target != null && DistanceToTarget <= _attackRange;
        }

        /// <summary>
        /// Resets the attack cooldown.
        /// </summary>
        public void ResetCooldown()
        {
            _canAttack = true;
            if (_attackState == AttackState.Cooldown)
            {
                ChangeAttackState(AttackState.Idle);
            }
        }

        /// <summary>
        /// Sets the attack cooldown immediately.
        /// </summary>
        public void SetOnCooldown()
        {
            _canAttack = false;
            if (_attackState == AttackState.Idle)
            {
                ChangeAttackState(AttackState.Cooldown);
            }
        }

        /// <summary>
        /// Cancels the current attack sequence and returns to idle state.
        /// </summary>
        public void CancelAttack()
        {
            if (_attackState == AttackState.Idle)
            {
                return;
            }

            ChangeAttackState(AttackState.Idle);
            
            // Re-enable NavMeshAgent when canceling attack
            if (_navAgent != null)
            {
                _navAgent.enabled = true;
            }
            
            RestoreNavAgentSettings();

            if (_attackVisual != null)
            {
                _attackVisual.SetActive(false);
            }
        }

        /// <summary>
        /// Starts the patrol behavior.
        /// </summary>
        public void StartPatrol()
        {
            if (!_enablePatrol)
            {
                return;
            }

            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"EnemyAI: Cannot start patrol - no waypoints assigned.", this);
                return;
            }

            _isPatrolling = true;
            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
            _navAgent.isStopped = false;
            _isWaitingAtWaypoint = false;
            SetNextPatrolDestination();
        }

        /// <summary>
        /// Stops the patrol behavior.
        /// </summary>
        public void StopPatrol()
        {
            _isPatrolling = false;
            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
            _navAgent.isStopped = true;
            _isWaitingAtWaypoint = false;
        }

        /// <summary>
        /// Starts the chase behavior.
        /// </summary>
        public void StartChase()
        {
            if (!_enableChase)
            {
                return;
            }

            if (_target == null)
            {
                Debug.LogWarning("EnemyAI: Cannot start chase - no target assigned.", this);
                return;
            }

            _isChasing = true;
            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
            _navAgent.isStopped = false;
            
            // Apply chase speed modifiers
            _navAgent.speed = _originalSpeed * _chaseSpeedMultiplier;
            _navAgent.angularSpeed = _originalAngularSpeed * _chaseAngularSpeedMultiplier;
            
            UpdateChase();
        }

        /// <summary>
        /// Stops the chase behavior.
        /// </summary>
        public void StopChase()
        {
            _isChasing = false;
            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
            _navAgent.isStopped = true;
            
            // Restore original speed values
            _navAgent.speed = _originalSpeed;
            _navAgent.angularSpeed = _originalAngularSpeed;
        }

        /// <summary>
        /// Starts the flee behavior. The enemy will run away from the target.
        /// </summary>
        public void StartFlee()
        {
            if (!_enableFlee)
            {
                return;
            }

            if (_target == null)
            {
                Debug.LogWarning("EnemyAI: Cannot start flee - no target assigned.", this);
                return;
            }

            _isFleeing = true;
            _fleeCooldownTimer = 0f;
            _navAgent.enabled = true;
            _navAgent.isStopped = false;

            // Apply flee speed modifiers
            _navAgent.speed = _originalSpeed * _fleeSpeedMultiplier;
            _navAgent.angularSpeed = _originalAngularSpeed * _fleeAngularSpeedMultiplier;

            // Calculate initial flee destination
            CalculateFleeDestination();
            _navAgent.SetDestination(_fleeDestination);

            OnFleeStarted?.Invoke();
        }

        /// <summary>
        /// Stops the flee behavior.
        /// </summary>
        public void StopFlee()
        {
            if (!_isFleeing)
            {
                return;
            }

            _isFleeing = false;
            _fleeCooldownTimer = 0f;

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _navAgent.isStopped = true;

                // Restore original speed values
                _navAgent.speed = _originalSpeed;
                _navAgent.angularSpeed = _originalAngularSpeed;
            }

            OnFleeEnded?.Invoke();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the patrol behavior.
        /// </summary>
        private void UpdatePatrol()
        {
            if (!_enablePatrol || !_isPatrolling || _navAgent == null || _waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            // Handle waiting at waypoint
            if (_isWaitingAtWaypoint)
            {
                _waypointWaitTimer -= Time.deltaTime;
                if (_waypointWaitTimer <= 0f)
                {
                    _isWaitingAtWaypoint = false;
                    SetNextPatrolDestination();
                }
                return;
            }

            // Check if agent has reached the current waypoint
            if (_navAgent.enabled && !_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                if (_waitAtWaypoints)
                {
                    StartWaypointWaiting();
                }
                else
                {
                    SetNextPatrolDestination();
                }
            }
        }

        /// <summary>
        /// Updates the chase behavior.
        /// </summary>
        private void UpdateChase()
        {
            if (!_enableChase)
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
                    UpdateChaseDestination();
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
                        if (_stopChaseWhenOutOfRange)
                        {
                            StopChase();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the flee behavior. Continuously recalculates the flee destination
        /// to keep moving away from the target.
        /// </summary>
        private void UpdateFlee()
        {
            if (!_enableFlee || !_isFleeing || _navAgent == null)
            {
                return;
            }

            if (_target == null)
            {
                StopFlee();
                return;
            }

            // Don't update flee during attack sequence
            if (_attackState != AttackState.Idle)
            {
                return;
            }

            // Recalculate flee destination periodically to keep moving away
            CalculateFleeDestination();

            // Check if we've reached the flee destination
            if (_navAgent.enabled && !_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                // We've reached the flee destination, recalculate
                _navAgent.SetDestination(_fleeDestination);
            }
        }

        /// <summary>
        /// Calculates a destination point away from the target for fleeing.
        /// Uses NavMesh sampling to find a valid point on the NavMesh.
        /// </summary>
        private void CalculateFleeDestination()
        {
            if (_target == null)
            {
                return;
            }

            // Direction away from the target
            Vector3 directionAway = (transform.position - _target.position).normalized;
            directionAway.y = 0f; // Keep on horizontal plane

            // If directly on top of each other, pick a random direction
            if (directionAway == Vector3.zero)
            {
                directionAway = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            }

            // Calculate desired flee position
            Vector3 desiredFleePosition = transform.position + directionAway * _fleeDistance;

            // Try to find a valid point on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(desiredFleePosition, out hit, _fleeDistance, NavMesh.AllAreas))
            {
                _fleeDestination = hit.position;
            }
            else
            {
                // If we can't find a valid point directly away, try random offsets
                bool foundValidPoint = false;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                    Vector3 testPosition = desiredFleePosition + randomOffset;

                    if (NavMesh.SamplePosition(testPosition, out hit, _fleeDistance * 0.5f, NavMesh.AllAreas))
                    {
                        _fleeDestination = hit.position;
                        foundValidPoint = true;
                        break;
                    }
                }

                // If still no valid point found, just move in the opposite direction as much as possible
                if (!foundValidPoint)
                {
                    _fleeDestination = transform.position + directionAway * (_fleeDistance * 0.5f);
                }
            }

            // Update the NavMeshAgent destination
            if (_navAgent != null && _navAgent.enabled && _isFleeing)
            {
                _navAgent.SetDestination(_fleeDestination);
            }
        }

        /// <summary>
        /// Updates the attack sequence state machine.
        /// </summary>
        private void UpdateAttackSequence()
        {
            if (!_enableAttack)
            {
                return;
            }

            switch (_attackState)
            {
                case AttackState.Idle:
                    // Waiting for attack command
                    break;

                case AttackState.Preparing:
                    UpdatePreparingState();
                    break;

                case AttackState.Attacking:
                    UpdateAttackingState();
                    break;

                case AttackState.Repositioning:
                    UpdateRepositioningState();
                    break;

                case AttackState.Cooldown:
                    UpdateCooldownState();
                    break;
            }
        }

        /// <summary>
        /// Updates the AI state based on current conditions.
        /// </summary>
        private void UpdateState()
        {
            bool isInAttackSequence = _attackState != AttackState.Idle;
            
            // If we're in an attack sequence, don't change AI state - let the attack sequence complete
            // This prevents NavMeshAgent from interfering with attack movement
            if (isInAttackSequence)
            {
                return;
            }

            // Check flee conditions first (highest priority)
            // If flee is enabled and the target is within flee detection range, flee
            if (_enableFlee && _target != null && !_isFleeing)
            {
                float distanceToTarget = DistanceToTarget;
                if (distanceToTarget <= _fleeDetectionRange)
                {
                    if (_currentState != AIState.Flee)
                    {
                        ChangeState(AIState.Flee);
                    }
                    return;
                }
            }

            // If fleeing and reached safe distance, stop fleeing
            if (_isFleeing && _stopFleeWhenSafe)
            {
                float distanceToTarget = DistanceToTarget;
                if (distanceToTarget >= _fleeSafeDistance)
                {
                    _fleeCooldownTimer += Time.deltaTime;
                    if (_fleeCooldownTimer >= _fleeCooldownTime)
                    {
                        StopFlee();
                        // Return to patrol or idle
                        if (_enablePatrol)
                        {
                            ChangeState(AIState.Patrol);
                        }
                        else
                        {
                            ChangeState(AIState.Idle);
                        }
                        return;
                    }
                }
                else
                {
                    // Still too close, reset cooldown timer
                    _fleeCooldownTimer = 0f;
                }
            }

            // If currently fleeing, don't process other states
            if (_currentState == AIState.Flee)
            {
                return;
            }
            
            // Check attack conditions (second highest priority)
            // Only transition to Attack state if not already in an attack sequence
            if (_enableAttack && IsTargetInRange() && CanAttack && !isInAttackSequence)
            {
                if (_currentState != AIState.Attack)
                {
                    ChangeState(AIState.Attack);
                }
                return;
            }

            // Check chase conditions
            if (_enableChase && _hasTarget)
            {
                if (_currentState != AIState.Chase)
                {
                    ChangeState(AIState.Chase);
                }
                return;
            }

            // If we were chasing and lost the target, transition to patrol or idle
            if (_currentState == AIState.Chase && !_hasTarget)
            {
                _wasChasing = true;
                _loseTargetTimer = 0f;

                if (_returnToPatrolOnLoseTarget && _enablePatrol)
                {
                    // Don't change state immediately, wait for delay
                    ChangeState(AIState.Idle);
                }
                else
                {
                    ChangeState(AIState.Idle);
                }
                return;
            }

            // Default to patrol if available
            if (_currentState == AIState.Idle && _enablePatrol && !_wasChasing)
            {
                ChangeState(AIState.Patrol);
            }
        }

        /// <summary>
        /// Stops all AI behaviors.
        /// </summary>
        private void StopAllBehaviors()
        {
            StopPatrol();
            StopChase();
            StopFlee();
            
            // Re-enable NavMeshAgent if it was disabled during attack sequence
            if (_navAgent != null && !_navAgent.enabled)
            {
                _navAgent.enabled = true;
            }
            
            // Note: We don't stop the attack as it manages its own state
        }

        /// <summary>
        /// Sets the next patrol destination for the NavMeshAgent.
        /// </summary>
        private void SetNextPatrolDestination()
        {
            if (_waypoints == null || _waypoints.Length == 0 || _navAgent == null)
            {
                return;
            }

            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint == null)
            {
                Debug.LogWarning($"EnemyAI: Waypoint at index {_currentWaypointIndex} is null. Skipping to next.", this);
                AdvanceToNextWaypoint();
                SetNextPatrolDestination();
                return;
            }

            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
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
        private int GetPingPongNextIndex(int currentIndex)
        {
            if (_isPatrolReversing)
            {
                if (currentIndex <= 0)
                {
                    _isPatrolReversing = false;
                    return 1;
                }
                return currentIndex - 1;
            }
            else
            {
                if (currentIndex >= _waypoints.Length - 1)
                {
                    _isPatrolReversing = true;
                    return _waypoints.Length - 2;
                }
                return currentIndex + 1;
            }
        }

        /// <summary>
        /// Starts the wait timer at the current waypoint.
        /// </summary>
        private void StartWaypointWaiting()
        {
            _isWaitingAtWaypoint = true;
            _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
            _navAgent.isStopped = true;
            _waypointWaitTimer = Random.Range(_minWaitTime, _maxWaitTime);
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

        /// <summary>
        /// Detects if the target is visible and within range.
        /// </summary>
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
        private void UpdateChaseDestination()
        {
            if (_target == null || _navAgent == null || !_navAgent.enabled)
            {
                return;
            }

            // Don't update chase destination during attack sequence
            // This prevents NavMeshAgent from interfering with attack movement
            if (_attackState != AttackState.Idle)
            {
                return;
            }

            float distance = DistanceToTarget;

            // Check if we've reached the stop distance
            if (distance <= _chaseStopDistance)
            {
                _navAgent.isStopped = true;
            }
            else
            {
                _navAgent.enabled = true; // Ensure NavMeshAgent is enabled
                _navAgent.isStopped = false;
                _navAgent.SetDestination(_target.position);
            }
        }

        /// <summary>
        /// Updates the preparation state (windup before attack).
        /// </summary>
        private void UpdatePreparingState()
        {
            _attackStateTimer += Time.deltaTime;

            if (_attackStateTimer >= _preparationTime)
            {
                ChangeAttackState(AttackState.Attacking);
            }
        }

        /// <summary>
        /// Updates the attacking state (execute attack with lunge/dash).
        /// </summary>
        private void UpdateAttackingState()
        {
            _attackStateTimer += Time.deltaTime;
            
            // Handle lunge/dash movement - use direct movement in straight line
            if (_attackMovementType != AttackMovementType.None)
            {
                float progress = Mathf.Clamp01(_attackStateTimer / _attackMovementDuration);
                Vector3 newPosition = Vector3.Lerp(_attackStartPosition, _attackTargetPosition, progress);
                
                // Use direct movement (not NavMeshAgent) to move in straight line
                // This allows player to dodge by moving out of the way
                if (_rigidbody != null)
                {
                    _rigidbody.MovePosition(newPosition - transform.position);
                }
                else
                {
                    transform.position = newPosition;
                }
            }

            // Deal damage at the start of the attack
            if (_attackStateTimer >= 0f && _attackStateTimer < Time.deltaTime)
            {
                DealDamage();
                OnAttackHit?.Invoke(_target);
            }

            // End attack after movement duration
            if (_attackStateTimer >= _attackMovementDuration)
            {
                OnAttackEnded?.Invoke();
                _canAttack = false;
                _attackStateTimer = 0f;
                ChangeAttackState(AttackState.Repositioning);
            }
        }

        /// <summary>
        /// Updates the repositioning state (move to optimal position for next attack).
        /// </summary>
        private void UpdateRepositioningState()
        {
            _attackStateTimer += Time.deltaTime;

            // Wait for post-attack pause
            if (_attackStateTimer < _postAttackPause)
            {
                return;
            }

            // Check if we need to reposition
            if (_target != null && _navAgent != null)
            {
                float distance = DistanceToTarget;

                // If we're too close or too far, reposition
                if (distance < _repositionDistance * 0.5f || distance > _repositionDistance * 1.5f)
                {
                    // Calculate desired position (at reposition distance from target)
                    Vector3 direction = (transform.position - _target.position).normalized;
                    Vector3 desiredPosition = _target.position + direction * _repositionDistance;

                    // Re-enable NavMeshAgent before repositioning
                    _navAgent.enabled = true;
                    _navAgent.isStopped = false;
                    _navAgent.SetDestination(desiredPosition);

                    // Check if we've reached the desired position
                    if (Vector3.Distance(transform.position, desiredPosition) < 0.5f)
                    {
                        RestoreNavAgentSettings();
                        ChangeAttackState(AttackState.Cooldown);
                    }
                }
                else
                {
                    // Already in good position
                    RestoreNavAgentSettings();
                    ChangeAttackState(AttackState.Cooldown);
                }
            }
            else
            {
                // No target or no nav agent, just go to cooldown
                RestoreNavAgentSettings();
                ChangeAttackState(AttackState.Cooldown);
            }
        }

        /// <summary>
        /// Updates the cooldown state (wait before next attack).
        /// </summary>
        private void UpdateCooldownState()
        {
            _attackStateTimer += Time.deltaTime;

            if (_attackStateTimer >= _attackCooldown)
            {
                _canAttack = true;
                _attackStateTimer = 0f;
                ChangeAttackState(AttackState.Idle);
            }
        }

        /// <summary>
        /// Attempts to attack the target. Returns true if attack was successful.
        /// </summary>
        private bool TryAttack()
        {
            if (!CanAttack)
            {
                return false;
            }

            if (_target == null)
            {
                return false;
            }

            if (!IsTargetInRange())
            {
                return false;
            }

            StartAttackSequence();
            return true;
        }

        /// <summary>
        /// Starts the attack sequence.
        /// </summary>
        private void StartAttackSequence()
        {
            // Stop and disable NavMeshAgent from chasing
            // This prevents NavMeshAgent from interfering with attack movement
            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false; // Disable NavMeshAgent to prevent any interference
            }

            // Calculate attack positions
            _attackStartPosition = transform.position;
            
            if (_attackMovementType != AttackMovementType.None && _target != null)
            {
                // Store the target's initial position at the start of the attack
                // This ensures the lunge goes toward where the target was when the attack started,
                // giving the player a chance to move out of the way
                _attackInitialTargetPosition = _target.position;
                
                // Calculate direction once at attack start using the initial target position
                Vector3 direction = (_attackInitialTargetPosition - transform.position).normalized;
                direction.y = 0f; // Keep movement on horizontal plane
                _attackDirection = direction; // Store direction for use during attack
                _attackTargetPosition = _attackStartPosition + direction * _attackMovementDistance;
            }
            else
            {
                _attackDirection = Vector3.zero;
                _attackTargetPosition = _attackStartPosition;
            }

            // Apply movement speed modifier
            if (_navAgent != null && _attackMovementType != AttackMovementType.None)
            {
                _navAgent.speed = _originalSpeed * _attackMovementSpeedMultiplier;
                _navAgent.angularSpeed = _originalAngularSpeed * _attackMovementSpeedMultiplier;
            }

            // Show attack visual
            if (_attackVisual != null)
            {
                _attackVisual.SetActive(true);
                _attackVisualTimer = 0f;
            }

            // Fire attack started event
            OnAttackStarted?.Invoke();

            // Start with preparation state
            _attackStateTimer = 0f;
            ChangeAttackState(AttackState.Preparing);
        }

        /// <summary>
        /// Changes the attack state.
        /// </summary>
        private void ChangeAttackState(AttackState newState)
        {
            if (_attackState == newState)
            {
                return;
            }

            AttackState oldState = _attackState;
            _attackState = newState;
            _attackStateTimer = 0f;

            OnAttackStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Restores the NavMeshAgent settings to their original values.
        /// </summary>
        private void RestoreNavAgentSettings()
        {
            if (_navAgent != null)
            {
                _navAgent.enabled = true; // Re-enable NavMeshAgent after attack sequence
                _navAgent.speed = _originalSpeed;
                _navAgent.angularSpeed = _originalAngularSpeed;
                _navAgent.isStopped = true;
            }
        }

        /// <summary>
        /// Updates the attack visual timer.
        /// </summary>
        private void UpdateAttackVisual()
        {
            if (_attackVisual != null && _attackVisual.activeSelf)
            {
                _attackVisualTimer += Time.deltaTime;
                if (_attackVisualTimer >= _attackVisualDuration)
                {
                    _attackVisual.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Deals damage to the target if it has a HealthComponent.
        /// </summary>
        private void DealDamage()
        {
            if (_target == null)
            {
                return;
            }

            // Try to get HealthComponent from target
            ChildOfEclipse.Health.HealthComponent healthComponent = _target.GetComponent<ChildOfEclipse.Health.HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(_damage);
            }
            else
            {
                // Try to get HealthComponent from parent (in case target is a child)
                healthComponent = _target.GetComponentInParent<ChildOfEclipse.Health.HealthComponent>();
                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(_damage);
                }
            }
        }

        /// <summary>
        /// Rotates to face the target.
        /// During attack, looks at the attack target position (calculated at attack start).
        /// </summary>
        private void LookAtTarget()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 targetPosition;
            
            // During attack (preparing or attacking), look at the attack target position (not the player's current position)
            // This prevents glitching and ensures the lunge goes in a straight line toward the initial target position
            if (_attackState == AttackState.Preparing || _attackState == AttackState.Attacking)
            {
                targetPosition = _attackTargetPosition;
            }
            else
            {
                targetPosition = _target.position;
            }
            
            targetPosition.y = transform.position.y; // Keep rotation on Y axis only

            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        #endregion
    }
}

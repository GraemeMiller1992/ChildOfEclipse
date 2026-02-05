using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// State machine controller for enemy AI that coordinates Patrol, Chase, and Attack behaviors.
    /// Manages state transitions and ensures only one behavior is active at a time.
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
            /// <summary>Enemy is idle (no active behavior).</summary>
            Idle
        }

        #endregion

        #region Serialized Fields

        [Header("AI Components")]
        [SerializeField]
        [Tooltip("The patrol component. If null, will try to find one on this GameObject.")]
        private NavMeshAgentPatrol _patrolComponent;

        [SerializeField]
        [Tooltip("The chase component. If null, will try to find one on this GameObject.")]
        private NavMeshAgentChase _chaseComponent;

        [SerializeField]
        [Tooltip("The attack component. If null, will try to find one on this GameObject.")]
        private NavMeshAgentAttack _attackComponent;

        [Header("State Settings")]
        [SerializeField]
        [Tooltip("The initial AI state.")]
        private AIState _initialState = AIState.Patrol;

        [SerializeField]
        [Tooltip("Whether to enable AI on start.")]
        private bool _enableOnStart = true;

        [Header("Transition Settings")]
        [SerializeField]
        [Tooltip("Priority order for state transitions. Higher priority states will override lower ones.")]
        private AIState[] _statePriority = new AIState[] { AIState.Attack, AIState.Chase, AIState.Patrol };

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

        private AIState _currentState = AIState.Idle;
        private bool _isEnabled = false;
        private float _loseTargetTimer = 0f;
        private bool _wasChasing = false;
        private bool _isStoppedOverride = false;
        private NavMeshAgent _navAgent;

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

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current AI state.
        /// </summary>
        public AIState CurrentState => _currentState;

        /// <summary>
        /// Gets whether the AI is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Gets the patrol component.
        /// </summary>
        public NavMeshAgentPatrol PatrolComponent => _patrolComponent;

        /// <summary>
        /// Gets the chase component.
        /// </summary>
        public NavMeshAgentChase ChaseComponent => _chaseComponent;

        /// <summary>
        /// Gets the attack component.
        /// </summary>
        public NavMeshAgentAttack AttackComponent => _attackComponent;

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
                        // Stop all behaviors when override is enabled
                        StopAllBehaviors();
                        // Also directly stop the NavMeshAgent to prevent behavior components from overriding
                        if (_navAgent != null)
                        {
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
            // Get NavMeshAgent
            _navAgent = GetComponent<NavMeshAgent>();

            // Get components if not assigned
            if (_patrolComponent == null)
            {
                _patrolComponent = GetComponent<NavMeshAgentPatrol>();
            }

            if (_chaseComponent == null)
            {
                _chaseComponent = GetComponent<NavMeshAgentChase>();
            }

            if (_attackComponent == null)
            {
                _attackComponent = GetComponent<NavMeshAgentAttack>();
            }

            // Subscribe to chase component events
            if (_chaseComponent != null)
            {
                _chaseComponent.OnTargetDetected += HandleTargetDetected;
                _chaseComponent.OnTargetLost += HandleTargetLost;
            }

            // Subscribe to attack component events
            if (_attackComponent != null)
            {
                _attackComponent.OnAttackStarted += HandleAttackStarted;
                _attackComponent.OnAttackEnded += HandleAttackEnded;
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

            // Enforce override state - keep NavMeshAgent stopped when override is active
            if (_isStoppedOverride)
            {
                if (_navAgent != null)
                {
                    _navAgent.isStopped = true;
                }
                return;
            }

            // Handle return to patrol logic
            if (_returnToPatrolOnLoseTarget && _wasChasing && _currentState == AIState.Idle)
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
            // Unsubscribe from events
            if (_chaseComponent != null)
            {
                _chaseComponent.OnTargetDetected -= HandleTargetDetected;
                _chaseComponent.OnTargetLost -= HandleTargetLost;
            }

            if (_attackComponent != null)
            {
                _attackComponent.OnAttackStarted -= HandleAttackStarted;
                _attackComponent.OnAttackEnded -= HandleAttackEnded;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showStateInGizmos)
            {
                return;
            }

            // Draw state label above the enemy
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"AI State: {_currentState}");
#endif
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
                    StartPatrol();
                    break;
                case AIState.Chase:
                    StartChase();
                    break;
                case AIState.Attack:
                    StartAttack();
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
        /// Sets the target for chase and attack components.
        /// </summary>
        /// <param name="target">The target transform.</param>
        public void SetTarget(Transform target)
        {
            if (_chaseComponent != null)
            {
                _chaseComponent.SetTarget(target);
            }

            if (_attackComponent != null)
            {
                _attackComponent.SetTarget(target);
            }
        }

        /// <summary>
        /// Sets the target by tag for chase and attack components.
        /// </summary>
        /// <param name="tag">The tag to search for.</param>
        public void SetTargetByTag(string tag)
        {
            if (_chaseComponent != null)
            {
                _chaseComponent.SetTargetByTag(tag);
            }

            if (_attackComponent != null)
            {
                _attackComponent.SetTargetByTag(tag);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the AI state based on current conditions.
        /// </summary>
        private void UpdateState()
        {
            // Check attack conditions first (highest priority)
            if (_attackComponent != null && _attackComponent.IsTargetInRange() && _attackComponent.CanAttack)
            {
                if (_currentState != AIState.Attack)
                {
                    ChangeState(AIState.Attack);
                }
                return;
            }

            // Check chase conditions
            if (_chaseComponent != null && _chaseComponent.HasTarget)
            {
                if (_currentState != AIState.Chase)
                {
                    ChangeState(AIState.Chase);
                }
                return;
            }

            // If we were chasing and lost the target, transition to patrol or idle
            if (_currentState == AIState.Chase && (_chaseComponent == null || !_chaseComponent.HasTarget))
            {
                _wasChasing = true;
                _loseTargetTimer = 0f;

                if (_returnToPatrolOnLoseTarget && _patrolComponent != null)
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
            if (_currentState == AIState.Idle && _patrolComponent != null && !_wasChasing)
            {
                ChangeState(AIState.Patrol);
            }
        }

        /// <summary>
        /// Starts the patrol behavior.
        /// </summary>
        private void StartPatrol()
        {
            if (_patrolComponent != null)
            {
                _patrolComponent.IsPatrolling = true;
            }
        }

        /// <summary>
        /// Starts the chase behavior.
        /// </summary>
        private void StartChase()
        {
            if (_chaseComponent != null)
            {
                _chaseComponent.StartChase();
            }
        }

        /// <summary>
        /// Starts the attack behavior.
        /// </summary>
        private void StartAttack()
        {
            if (_attackComponent != null)
            {
                _attackComponent.TryAttack();
            }
        }

        /// <summary>
        /// Stops all AI behaviors.
        /// </summary>
        private void StopAllBehaviors()
        {
            if (_patrolComponent != null)
            {
                _patrolComponent.IsPatrolling = false;
            }

            if (_chaseComponent != null)
            {
                _chaseComponent.StopChase();
            }

            // Note: We don't stop the attack component as it manages its own state
        }

        /// <summary>
        /// Handles the target detected event from the chase component.
        /// </summary>
        private void HandleTargetDetected()
        {
            _wasChasing = true;
            _loseTargetTimer = 0f;
        }

        /// <summary>
        /// Handles the target lost event from the chase component.
        /// </summary>
        private void HandleTargetLost()
        {
            // State change will be handled in UpdateState
        }

        /// <summary>
        /// Handles the attack started event from the attack component.
        /// </summary>
        private void HandleAttackStarted()
        {
            // Attack state is already set
        }

        /// <summary>
        /// Handles the attack ended event from the attack component.
        /// </summary>
        private void HandleAttackEnded()
        {
            // Check if we should remain in attack state or transition
            if (_attackComponent != null && _attackComponent.IsTargetInRange())
            {
                // Stay in attack state, wait for next attack
            }
            else
            {
                // Transition to chase or patrol
                UpdateState();
            }
        }

        #endregion
    }
}

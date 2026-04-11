using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Chase,
            Attack,
            Flee,
            Idle
        }

        public enum PatrolMode
        {
            Loop,
            PingPong,
            Random
        }

        public enum AttackMovementType
        {
            None,
            Lunge,
            Dash
        }

        public enum AttackState
        {
            Idle,
            Preparing,
            Attacking,
            Repositioning,
            Cooldown
        }

        [Header("Feature Enable/Disable")]
        [SerializeField] private bool _enablePatrol = true;
        [SerializeField] private bool _enableChase = true;
        [SerializeField] private bool _enableAttack = true;
        [SerializeField] private bool _enableFlee = false;

        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private string _targetTag = "Player";

        [Header("Patrol Settings")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private PatrolMode _patrolMode = PatrolMode.Loop;
        [SerializeField] private bool _shuffleOnStart = false;
        [SerializeField] private bool _startPatrolOnAwake = true;
        [SerializeField] private bool _waitAtWaypoints = true;
        [SerializeField] private float _minWaitTime = 1f;
        [SerializeField] private float _maxWaitTime = 2f;
        [SerializeField] private bool _showPatrolDebugLines = true;
        [SerializeField] private Color _patrolDebugLineColor = Color.cyan;

        [Header("Chase Detection Settings")]
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField, Range(0f, 360f)] private float _fieldOfView = 90f;
        [SerializeField] private LayerMask _obstacleLayers;
        [SerializeField] private bool _requireLineOfSight = true;

        [Header("Chase Movement Settings")]
        [SerializeField] private float _chaseSpeedMultiplier = 1.5f;
        [SerializeField] private float _chaseAngularSpeedMultiplier = 2f;
        [SerializeField] private float _chaseStopDistance = 2f;
        [SerializeField] private bool _stopChaseWhenOutOfRange = true;
        [SerializeField] private float _loseTargetDelay = 2f;
        [SerializeField] private bool _showChaseDebugGizmos = true;
        [SerializeField] private Color _detectionRangeColor = new Color(1f, 0f, 0f, 0.2f);
        [SerializeField] private Color _fieldOfViewColor = new Color(1f, 1f, 0f, 0.3f);
        [SerializeField] private Color _lineOfSightColor = Color.green;
        [SerializeField] private float _detectionResetCooldown = 2f;
        private float _detectionResetTimer = 0f;

        [Header("Attack Settings")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private bool _lookAtTarget = true;
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("Attack Movement")]
        [SerializeField] private AttackMovementType _attackMovementType = AttackMovementType.None;
        [SerializeField] private float _attackMovementDuration = 0.3f;
        [SerializeField] private float _attackMovementDistance = 2f;
        [SerializeField] private float _attackMovementSpeedMultiplier = 3f;
        [SerializeField] private float _preparationTime = 0.2f;
        [SerializeField] private float _postAttackPause = 0.3f;
        [SerializeField] private float _repositionDistance = 1.5f;

        [Header("Attack Visuals")]
        [SerializeField] private GameObject _attackVisual;
        [SerializeField] private float _attackVisualDuration = 0.3f;
        [SerializeField] private bool _showAttackDebugGizmos = true;
        [SerializeField] private Color _attackRangeColor = new Color(1f, 0f, 0f, 0.3f);

        [Header("Flee Settings")]
        [SerializeField] private float _fleeDetectionRange = 10f;
        [SerializeField] private float _fleeDistance = 15f;
        [SerializeField] private float _fleeSafeDistance = 20f;
        [SerializeField] private float _fleeSpeedMultiplier = 1.8f;
        [SerializeField] private float _fleeAngularSpeedMultiplier = 2.5f;
        [SerializeField] private bool _stopFleeWhenSafe = true;
        [SerializeField] private float _fleeCooldownTime = 2f;
        [SerializeField] private bool _showFleeDebugGizmos = true;
        [SerializeField] private Color _fleeDetectionRangeColor = new Color(0f, 0f, 1f, 0.2f);
        [SerializeField] private Color _fleeSafeDistanceColor = new Color(0f, 1f, 0f, 0.2f);

        [Header("AI State Settings")]
        [SerializeField] private AIState _initialState = AIState.Patrol;
        [SerializeField] private bool _enableOnStart = true;
        [SerializeField] private bool _returnToPatrolOnLoseTarget = true;
        [SerializeField] private float _returnToPatrolDelay = 3f;

        [Header("Debug Settings")]
        [SerializeField] private bool _logStateChanges = true;
        [SerializeField] private bool _showStateInGizmos = true;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private NavMeshAgent _navAgent;
        private Rigidbody _rigidbody;

        private AIState _currentState = AIState.Idle;
        private bool _isEnabled;
        private bool _isStoppedOverride;

        private bool _isPatrolling;
        private int _currentWaypointIndex;
        private bool _isWaitingAtWaypoint;
        private float _waypointWaitTimer;
        private bool _isPatrolReversing;

        private bool _isChasing;
        private bool _hasTarget;
        private float _loseTargetTimer;
        private bool _wasChasing;

        private AttackState _attackState = AttackState.Idle;
        private float _attackStateTimer;
        private float _attackVisualTimer;
        private bool _canAttack = true;
        private bool _hasDealtDamageThisAttack;
        private Vector3 _attackStartPosition;
        private Vector3 _attackTargetPosition;
        private Vector3 _attackInitialTargetPosition;
        private float _originalSpeed;
        private float _originalAngularSpeed;

        private bool _isFleeing;
        private Vector3 _fleeDestination;
        private float _fleeCooldownTimer;

        public event System.Action<AIState, AIState> OnStateChanged;
        public event System.Action OnAIEnabled;
        public event System.Action OnAIDisabled;
        public event System.Action OnTargetDetected;
        public event System.Action OnTargetLost;
        public event System.Action OnAttackStarted;
        public event System.Action<Transform> OnAttackHit;
        public event System.Action OnAttackEnded;
        public event System.Action<AttackState, AttackState> OnAttackStateChanged;
        public event System.Action OnFleeStarted;
        public event System.Action OnFleeEnded;

        public AIState CurrentState => _currentState;
        public AttackState CurrentAttackState => _attackState;
        public bool IsEnabled => _isEnabled;
        public bool IsPatrolling => _isPatrolling;
        public bool IsChasing => _isChasing;
        public bool IsAttacking => _attackState == AttackState.Attacking;
        public bool IsInAttackSequence => _attackState != AttackState.Idle;
        public bool IsFleeing => _isFleeing;
        public bool CanAttack => _canAttack && _attackState == AttackState.Idle;
        public bool HasTarget => _hasTarget;
        public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

        public bool IsStoppedOverride
        {
            get => _isStoppedOverride;
            set
            {
                if (_isStoppedOverride == value) return;
                _isStoppedOverride = value;

                if (_isStoppedOverride)
                {
                    StopAllBehaviors();
                    if (_navAgent != null)
                    {
                        _navAgent.enabled = true;
                        _navAgent.isStopped = true;
                    }
                }
            }
        }

        private bool IsInDetectionResetCooldown()
        {
            return _detectionResetTimer > 0f;
        }

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _rigidbody = GetComponent<Rigidbody>();

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _originalSpeed = _navAgent.speed;
                _originalAngularSpeed = _navAgent.angularSpeed;
            }

            if (_target == null && !string.IsNullOrEmpty(_targetTag))
            {
                GameObject targetObj = GameObject.FindGameObjectWithTag(_targetTag);
                if (targetObj != null) _target = targetObj.transform;
            }

            if (_enablePatrol && _waypoints != null && _waypoints.Length > 0)
            {
                if (_shuffleOnStart && _patrolMode == PatrolMode.Random)
                    ShuffleWaypoints();

                if (_startPatrolOnAwake)
                    StartPatrol();
            }

            if (_attackVisual != null)
                _attackVisual.SetActive(false);
        }

        private void Start()
        {
            if (_enableOnStart)
                EnableAI();
        }

        private void Update()
        {
            if (animator != null && _navAgent != null)
                animator.SetFloat("Speed", _navAgent.velocity.magnitude);

            if (!_isEnabled) return;

            if (_isStoppedOverride)
            {
                if (_navAgent != null)
                {
                    _navAgent.enabled = true;
                    _navAgent.isStopped = true;
                }
                return;
            }

            if (_detectionResetTimer > 0f)
                _detectionResetTimer -= Time.deltaTime;

            UpdatePatrol();
            UpdateChase();
            UpdateFlee();
            UpdateAttackSequence();
            UpdateAttackVisual();

            if (!IsInDetectionResetCooldown())
            {
                if (_enableAttack && _target != null && CanAttack && IsTargetInRange())
                    TryAttack();
            }

            if (!IsInDetectionResetCooldown())
            {
                if (_lookAtTarget && _target != null && (_currentState == AIState.Chase || _attackState != AttackState.Idle))
                    LookAtTarget();
            }

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

            UpdateState();
        }

        public void EnableAI()
        {
            if (_isEnabled) return;
            _isEnabled = true;
            ChangeState(_initialState);
            OnAIEnabled?.Invoke();
        }

        public void DisableAI()
        {
            if (!_isEnabled) return;
            _isEnabled = false;
            StopAllBehaviors();
            _currentState = AIState.Idle;
            OnAIDisabled?.Invoke();
        }

        public void ChangeState(AIState newState)
        {
            if (_currentState == newState) return;

            AIState oldState = _currentState;
            _currentState = newState;

            if (_logStateChanges)
                Debug.Log($"EnemyAI: {gameObject.name} changing state from {oldState} to {newState}", this);

            StopAllBehaviors();

            switch (newState)
            {
                case AIState.Patrol:
                    if (_enablePatrol) StartPatrol();
                    break;
                case AIState.Chase:
                    if (_enableChase) StartChase();
                    break;
                case AIState.Attack:
                    if (_enableAttack) TryAttack();
                    break;
                case AIState.Flee:
                    if (_enableFlee) StartFlee();
                    break;
            }

            OnStateChanged?.Invoke(oldState, newState);
        }

        public void ForceState(AIState newState) => ChangeState(newState);

        public void SetTargetByTag(string tag)
        {
            _targetTag = tag;
            GameObject targetObj = GameObject.FindGameObjectWithTag(tag);
            _target = targetObj != null ? targetObj.transform : null;
        }

        public bool IsTargetInRange()
        {
            if (IsInDetectionResetCooldown())
                return false;

            return _target != null && DistanceToTarget <= _attackRange;
        }
        public void ResetCooldown()
        {
            _canAttack = true;
            if (_attackState == AttackState.Cooldown)
                ChangeAttackState(AttackState.Idle);
        }

        public void SetOnCooldown()
        {
            _canAttack = false;
            if (_attackState == AttackState.Idle)
                ChangeAttackState(AttackState.Cooldown);
        }

        public void CancelAttack()
        {
            if (_attackState == AttackState.Idle) return;

            ChangeAttackState(AttackState.Idle);
            _hasDealtDamageThisAttack = false;

            if (_navAgent != null)
                _navAgent.enabled = true;

            RestoreNavAgentSettings();

            if (_attackVisual != null)
                _attackVisual.SetActive(false);
        }

        public void StartPatrol()
        {
            if (!_enablePatrol) return;
            if (_waypoints == null || _waypoints.Length == 0) return;

            _isPatrolling = true;
            _navAgent.enabled = true;
            _navAgent.isStopped = false;
            _isWaitingAtWaypoint = false;
            SetNextPatrolDestination();
        }

        public void StopPatrol()
        {
            _isPatrolling = false;
            if (_navAgent == null) return;
            _navAgent.enabled = true;
            _navAgent.isStopped = true;
            _isWaitingAtWaypoint = false;
        }

        public void StartChase()
        {
            if (!_enableChase || _target == null || _navAgent == null) return;

            _isChasing = true;
            _navAgent.enabled = true;
            _navAgent.isStopped = false;
            _navAgent.speed = _originalSpeed * _chaseSpeedMultiplier;
            _navAgent.angularSpeed = _originalAngularSpeed * _chaseAngularSpeedMultiplier;
            UpdateChaseDestination();
        }

        public void StopChase()
        {
            _isChasing = false;
            if (_navAgent == null) return;
            _navAgent.enabled = true;
            _navAgent.isStopped = true;
            _navAgent.speed = _originalSpeed;
            _navAgent.angularSpeed = _originalAngularSpeed;
        }

        public void StartFlee()
        {
            if (!_enableFlee || _target == null || _navAgent == null) return;

            _isFleeing = true;
            _fleeCooldownTimer = 0f;
            _navAgent.enabled = true;
            _navAgent.isStopped = false;
            _navAgent.speed = _originalSpeed * _fleeSpeedMultiplier;
            _navAgent.angularSpeed = _originalAngularSpeed * _fleeAngularSpeedMultiplier;

            CalculateFleeDestination();
            _navAgent.SetDestination(_fleeDestination);

            OnFleeStarted?.Invoke();
        }

        public void StopFlee()
        {
            if (!_isFleeing) return;

            _isFleeing = false;
            _fleeCooldownTimer = 0f;

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _navAgent.isStopped = true;
                _navAgent.speed = _originalSpeed;
                _navAgent.angularSpeed = _originalAngularSpeed;
            }

            OnFleeEnded?.Invoke();
        }

        private void UpdatePatrol()
        {
            if (!_enablePatrol || !_isPatrolling || _navAgent == null || _waypoints == null || _waypoints.Length == 0)
                return;

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

            if (_navAgent.enabled && !_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                if (_waitAtWaypoints) StartWaypointWaiting();
                else SetNextPatrolDestination();
            }
        }

        private void UpdateChase()
        {
            if (!_enableChase)
                return;

            if (IsInDetectionResetCooldown())
            {
                _hasTarget = false;
                _isChasing = false;

                if (_navAgent != null)
                {
                    _navAgent.enabled = true;
                    _navAgent.isStopped = true;
                    _navAgent.ResetPath();
                }

                return;
            }

            if (_target == null)
            {
                if (_isChasing) StopChase();
                return;
            }

            bool targetDetected = DetectTarget();

            if (targetDetected)
            {
                if (!_hasTarget)
                {
                    _hasTarget = true;
                    _loseTargetTimer = 0f;
                    OnTargetDetected?.Invoke();
                }

                if (!_isChasing && _attackState == AttackState.Idle)
                    StartChase();

                if (_isChasing)
                    UpdateChaseDestination();
            }
            else
            {
                if (_hasTarget)
                {
                    _loseTargetTimer += Time.deltaTime;
                    if (_loseTargetTimer >= _loseTargetDelay)
                    {
                        _hasTarget = false;
                        OnTargetLost?.Invoke();

                        if (_stopChaseWhenOutOfRange)
                            StopChase();
                    }
                }
            }
        }

        private void UpdateFlee()
        {
            if (!_enableFlee || !_isFleeing || _navAgent == null) return;

            if (_target == null)
            {
                StopFlee();
                return;
            }

            if (_attackState != AttackState.Idle) return;

            CalculateFleeDestination();

            if (_navAgent.enabled && !_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
                _navAgent.SetDestination(_fleeDestination);
        }

        private void CalculateFleeDestination()
        {
            if (_target == null) return;

            Vector3 directionAway = (transform.position - _target.position).normalized;
            directionAway.y = 0f;

            if (directionAway == Vector3.zero)
                directionAway = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

            Vector3 desiredFleePosition = transform.position + directionAway * _fleeDistance;

            if (NavMesh.SamplePosition(desiredFleePosition, out NavMeshHit hit, _fleeDistance, NavMesh.AllAreas))
            {
                _fleeDestination = hit.position;
            }
            else
            {
                bool foundValid = false;

                for (int i = 0; i < 5; i++)
                {
                    Vector3 testPosition = desiredFleePosition + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                    if (NavMesh.SamplePosition(testPosition, out hit, _fleeDistance * 0.5f, NavMesh.AllAreas))
                    {
                        _fleeDestination = hit.position;
                        foundValid = true;
                        break;
                    }
                }

                if (!foundValid)
                    _fleeDestination = transform.position + directionAway * (_fleeDistance * 0.5f);
            }

            if (_navAgent != null && _navAgent.enabled && _isFleeing)
                _navAgent.SetDestination(_fleeDestination);
        }

        private void UpdateAttackSequence()
        {
            if (!_enableAttack) return;

            switch (_attackState)
            {
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

        private void UpdateState()
        {
            if (IsInDetectionResetCooldown())
            {
                if (_currentState != AIState.Patrol && _enablePatrol)
                    ChangeState(AIState.Patrol);
                return;
            }

            bool isInAttackSequence = _attackState != AttackState.Idle;
            if (isInAttackSequence) return;

            if (_enableFlee && _target != null && !_isFleeing)
            {
                if (DistanceToTarget <= _fleeDetectionRange)
                {
                    if (_currentState != AIState.Flee)
                        ChangeState(AIState.Flee);
                    return;
                }
            }

            if (_isFleeing && _stopFleeWhenSafe)
            {
                if (DistanceToTarget >= _fleeSafeDistance)
                {
                    _fleeCooldownTimer += Time.deltaTime;
                    if (_fleeCooldownTimer >= _fleeCooldownTime)
                    {
                        StopFlee();
                        ChangeState(_enablePatrol ? AIState.Patrol : AIState.Idle);
                        return;
                    }
                }
                else
                {
                    _fleeCooldownTimer = 0f;
                }
            }

            if (_currentState == AIState.Flee) return;

            if (_enableAttack && IsTargetInRange() && CanAttack)
            {
                if (_currentState != AIState.Attack)
                    ChangeState(AIState.Attack);
                return;
            }

            if (_enableChase && _hasTarget)
            {
                if (_currentState != AIState.Chase)
                    ChangeState(AIState.Chase);
                return;
            }

            if (_currentState == AIState.Chase && !_hasTarget)
            {
                _wasChasing = true;
                _loseTargetTimer = 0f;
                ChangeState(AIState.Idle);
                return;
            }

            if (_currentState == AIState.Idle && _enablePatrol && !_wasChasing)
                ChangeState(AIState.Patrol);
        }

        private void StopAllBehaviors()
        {
            StopPatrol();
            StopChase();
            StopFlee();

            if (_navAgent != null && !_navAgent.enabled)
                _navAgent.enabled = true;
        }

        private void SetNextPatrolDestination()
        {
            if (_waypoints == null || _waypoints.Length == 0 || _navAgent == null) return;

            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint == null)
            {
                AdvanceToNextWaypoint();
                SetNextPatrolDestination();
                return;
            }

            _navAgent.enabled = true;
            _navAgent.isStopped = false;
            _navAgent.SetDestination(targetWaypoint.position);
        }

        private void AdvanceToNextWaypoint()
        {
            _currentWaypointIndex = GetNextWaypointIndex(_currentWaypointIndex);
        }

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

        private int GetPingPongNextIndex(int currentIndex)
        {
            if (_isPatrolReversing)
            {
                if (currentIndex <= 0)
                {
                    _isPatrolReversing = false;
                    return Mathf.Min(1, _waypoints.Length - 1);
                }
                return currentIndex - 1;
            }

            if (currentIndex >= _waypoints.Length - 1)
            {
                _isPatrolReversing = true;
                return Mathf.Max(_waypoints.Length - 2, 0);
            }

            return currentIndex + 1;
        }

        // Add this inside EnemyAI

        public void ResetDetectionState()
        {
            _hasTarget = false;
            _isChasing = false;
            _wasChasing = false;
            _loseTargetTimer = 0f;

            _isFleeing = false;
            _fleeCooldownTimer = 0f;

            _canAttack = true;
            _attackStateTimer = 0f;
            _attackVisualTimer = 0f;
            _attackState = AttackState.Idle;
            _hasDealtDamageThisAttack = false;

            _detectionResetTimer = _detectionResetCooldown;

            if (_attackVisual != null)
                _attackVisual.SetActive(false);

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _navAgent.isStopped = true;
                _navAgent.ResetPath();
                _navAgent.speed = _originalSpeed;
                _navAgent.angularSpeed = _originalAngularSpeed;
            }

            _currentState = AIState.Idle;
        }
        private void StartWaypointWaiting()
        {
            _isWaitingAtWaypoint = true;
            _navAgent.enabled = true;
            _navAgent.isStopped = true;
            _waypointWaitTimer = Random.Range(_minWaitTime, _maxWaitTime);
            AdvanceToNextWaypoint();
        }

        private void ShuffleWaypoints()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            for (int i = _waypoints.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_waypoints[i], _waypoints[j]) = (_waypoints[j], _waypoints[i]);
            }
        }

        private bool DetectTarget()
        {
            if (IsInDetectionResetCooldown())
                return false;

            if (_target == null)
                return false;

            float distance = DistanceToTarget;
            if (distance > _detectionRange)
                return false;

            if (_fieldOfView < 360f)
            {
                Vector3 directionToTarget = (_target.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                if (angleToTarget > _fieldOfView * 0.5f)
                    return false;
            }

            if (_requireLineOfSight && !HasLineOfSight())
                return false;

            return true;
        }

        private bool HasLineOfSight()
        {
            if (_target == null) return false;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 targetPosition = _target.position + Vector3.up * 0.5f;
            Vector3 direction = (targetPosition - origin).normalized;
            float distance = Vector3.Distance(origin, targetPosition);

            return !Physics.Raycast(origin, direction, distance, _obstacleLayers);
        }

        private void UpdateChaseDestination()
        {
            if (_target == null || _navAgent == null || !_navAgent.enabled) return;
            if (_attackState != AttackState.Idle) return;

            if (DistanceToTarget <= _chaseStopDistance)
            {
                _navAgent.isStopped = true;
            }
            else
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(_target.position);
            }
        }

        private void UpdatePreparingState()
        {
            _attackStateTimer += Time.deltaTime;

            if (_attackStateTimer >= _preparationTime)
                ChangeAttackState(AttackState.Attacking);
        }

        private void UpdateAttackingState()
        {
            _attackStateTimer += Time.deltaTime;

            if (_attackMovementType != AttackMovementType.None)
            {
                float duration = Mathf.Max(0.01f, _attackMovementDuration);
                float progress = Mathf.Clamp01(_attackStateTimer / duration);
                Vector3 newPosition = Vector3.Lerp(_attackStartPosition, _attackTargetPosition, progress);

                if (_rigidbody != null && !_rigidbody.isKinematic)
                    _rigidbody.MovePosition(newPosition);
                else
                    transform.position = newPosition;
            }

            if (!_hasDealtDamageThisAttack)
            {
                _hasDealtDamageThisAttack = true;
                DealDamage();
                OnAttackHit?.Invoke(_target);
            }

            if (_attackStateTimer >= Mathf.Max(0.01f, _attackMovementDuration))
            {
                OnAttackEnded?.Invoke();
                _canAttack = false;
                ChangeAttackState(AttackState.Repositioning);
            }
        }

        private void UpdateRepositioningState()
        {
            _attackStateTimer += Time.deltaTime;

            if (_attackStateTimer < _postAttackPause) return;

            if (_target != null && _navAgent != null)
            {
                float distance = DistanceToTarget;

                if (distance < _repositionDistance * 0.5f || distance > _repositionDistance * 1.5f)
                {
                    Vector3 direction = (transform.position - _target.position).normalized;
                    if (direction == Vector3.zero) direction = -transform.forward;

                    Vector3 desiredPosition = _target.position + direction * _repositionDistance;

                    _navAgent.enabled = true;
                    _navAgent.isStopped = false;
                    _navAgent.SetDestination(desiredPosition);

                    if (!_navAgent.pathPending && _navAgent.remainingDistance <= 0.5f)
                    {
                        RestoreNavAgentSettings();
                        ChangeAttackState(AttackState.Cooldown);
                    }
                }
                else
                {
                    RestoreNavAgentSettings();
                    ChangeAttackState(AttackState.Cooldown);
                }
            }
            else
            {
                RestoreNavAgentSettings();
                ChangeAttackState(AttackState.Cooldown);
            }
        }

        private void UpdateCooldownState()
        {
            _attackStateTimer += Time.deltaTime;

            if (_attackStateTimer >= _attackCooldown)
            {
                _canAttack = true;
                ChangeAttackState(AttackState.Idle);

                if (_hasTarget && _enableChase)
                    ChangeState(AIState.Chase);
                else if (_enablePatrol)
                    ChangeState(AIState.Patrol);
                else
                    ChangeState(AIState.Idle);
            }
        }

        private bool TryAttack()
        {
            if (!CanAttack || _target == null || !IsTargetInRange())
                return false;

            StartAttackSequence();
            return true;
        }

        private void StartAttackSequence()
        {
            _hasDealtDamageThisAttack = false;
            _attackStartPosition = transform.position;

            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false;
            }

            if (_attackMovementType != AttackMovementType.None && _target != null)
            {
                _attackInitialTargetPosition = _target.position;
                Vector3 direction = (_attackInitialTargetPosition - transform.position).normalized;
                direction.y = 0f;

                if (direction == Vector3.zero)
                    direction = transform.forward;

                _attackTargetPosition = _attackStartPosition + direction * _attackMovementDistance;
            }
            else
            {
                _attackTargetPosition = _attackStartPosition;
            }

            if (_attackVisual != null)
            {
                _attackVisual.SetActive(true);
                _attackVisualTimer = 0f;
            }

            OnAttackStarted?.Invoke();
            ChangeAttackState(AttackState.Preparing);
        }

        private void ChangeAttackState(AttackState newState)
        {
            if (_attackState == newState) return;

            AttackState oldState = _attackState;
            _attackState = newState;
            _attackStateTimer = 0f;
            OnAttackStateChanged?.Invoke(oldState, newState);
        }

        private void RestoreNavAgentSettings()
        {
            if (_navAgent == null) return;

            _navAgent.enabled = true;
            _navAgent.speed = _originalSpeed;
            _navAgent.angularSpeed = _originalAngularSpeed;
            _navAgent.isStopped = true;
        }

        private void UpdateAttackVisual()
        {
            if (_attackVisual == null || !_attackVisual.activeSelf) return;

            _attackVisualTimer += Time.deltaTime;
            if (_attackVisualTimer >= _attackVisualDuration)
                _attackVisual.SetActive(false);
        }

        private void DealDamage()
        {
            if (_target == null) return;

            float distance = Vector3.Distance(transform.position, _target.position);
            if (distance > _attackRange + 0.5f) return;

            ChildOfEclipse.Health.HealthComponent healthComponent =
                _target.GetComponent<ChildOfEclipse.Health.HealthComponent>();

            if (healthComponent == null)
                healthComponent = _target.GetComponentInParent<ChildOfEclipse.Health.HealthComponent>();

            if (healthComponent != null)
                healthComponent.TakeDamage(_damage);
        }

        private void LookAtTarget()
        {
            if (_target == null) return;

            Vector3 targetPosition =
                (_attackState == AttackState.Preparing || _attackState == AttackState.Attacking)
                ? _attackTargetPosition
                : _target.position;

            targetPosition.y = transform.position.y;
            Vector3 direction = (targetPosition - transform.position).normalized;

            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            if (_showStateInGizmos)
            {
#if UNITY_EDITOR
                string stateLabel = _attackState != AttackState.Idle
                    ? $"{_currentState} ({_attackState})"
                    : _currentState.ToString();
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"AI State: {stateLabel}");
#endif
            }

            if (_enablePatrol && _showPatrolDebugLines && _waypoints != null && _waypoints.Length > 0)
            {
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] == null) continue;

                    Gizmos.color = i == _currentWaypointIndex ? Color.green : Color.yellow;
                    Gizmos.DrawSphere(_waypoints[i].position, 0.5f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(_waypoints[i].position + Vector3.up * 0.5f, i.ToString());
#endif
                }

                Gizmos.color = _patrolDebugLineColor;
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] == null) continue;
                    int nextIndex = GetNextWaypointIndex(i);
                    if (_waypoints[nextIndex] != null)
                        Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                }
            }

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
                }

                if (_target != null && _hasTarget)
                {
                    Gizmos.color = _lineOfSightColor;
                    Gizmos.DrawLine(transform.position, _target.position);
                }

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, _chaseStopDistance);
            }

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

            if (_enableFlee && _showFleeDebugGizmos)
            {
                Gizmos.color = _fleeDetectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, _fleeDetectionRange);

                Gizmos.color = _fleeSafeDistanceColor;
                Gizmos.DrawWireSphere(transform.position, _fleeSafeDistance);

                if (_isFleeing && _target != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(transform.position, _fleeDestination);
                    Gizmos.DrawSphere(_fleeDestination, 0.5f);
                }
            }
        }
    }
}

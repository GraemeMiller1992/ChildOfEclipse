using UnityEngine;

namespace World
{
    /// <summary>
    /// Handles attack behavior for an enemy when a target is within range.
    /// Can deal damage to targets with HealthComponent.
    /// </summary>
    public class NavMeshAgentAttack : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Defines how the attack is triggered.
        /// </summary>
        public enum AttackTriggerMode
        {
            /// <summary>Attack is triggered by external code (e.g., EnemyAI).</summary>
            Manual,
            /// <summary>Attack automatically when target is within range.</summary>
            Automatic
        }

        #endregion

        #region Serialized Fields

        [Header("Target Settings")]
        [SerializeField]
        [Tooltip("The target to attack.")]
        private Transform _target;

        [SerializeField]
        [Tooltip("Tag to automatically find the target. If set, will search for GameObject with this tag.")]
        private string _targetTag = "Player";

        [Header("Attack Settings")]
        [SerializeField]
        [Tooltip("How the attack is triggered.")]
        private AttackTriggerMode _triggerMode = AttackTriggerMode.Manual;

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

        [Header("Attack Visuals")]
        [SerializeField]
        [Tooltip("GameObject to enable/disable during attack animation.")]
        private GameObject _attackVisual;

        [SerializeField]
        [Tooltip("Duration of the attack visual.")]
        private float _attackVisualDuration = 0.3f;

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Whether to draw debug visualization.")]
        private bool _showDebugGizmos = true;

        [SerializeField]
        [Tooltip("Color for attack range gizmo.")]
        private Color _attackRangeColor = new Color(1f, 0f, 0f, 0.3f);

        #endregion

        #region Private Fields

        private bool _isAttacking = false;
        private float _attackTimer = 0f;
        private float _attackVisualTimer = 0f;
        private bool _canAttack = true;

        #endregion

        #region Events

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

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the target to attack.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Gets whether the enemy is currently attacking.
        /// </summary>
        public bool IsAttacking => _isAttacking;

        /// <summary>
        /// Gets whether the enemy can attack (not on cooldown).
        /// </summary>
        public bool CanAttack => _canAttack && !_isAttacking;

        /// <summary>
        /// Gets the distance to the target.
        /// </summary>
        public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

        /// <summary>
        /// Gets the attack cooldown progress (0 to 1).
        /// </summary>
        public float CooldownProgress => _attackTimer / _attackCooldown;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find target by tag if not assigned
            if (_target == null && !string.IsNullOrEmpty(_targetTag))
            {
                GameObject targetObj = GameObject.FindGameObjectWithTag(_targetTag);
                if (targetObj != null)
                {
                    _target = targetObj.transform;
                }
            }

            // Initialize attack visual state
            if (_attackVisual != null)
            {
                _attackVisual.SetActive(false);
            }
        }

        private void Update()
        {
            // Update attack cooldown
            if (!_canAttack)
            {
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= _attackCooldown)
                {
                    _canAttack = true;
                    _attackTimer = 0f;
                }
            }

            // Update attack visual timer
            if (_attackVisual != null && _attackVisual.activeSelf)
            {
                _attackVisualTimer += Time.deltaTime;
                if (_attackVisualTimer >= _attackVisualDuration)
                {
                    _attackVisual.SetActive(false);
                }
            }

            // Handle automatic attack mode
            if (_triggerMode == AttackTriggerMode.Automatic && _target != null)
            {
                if (CanAttack && IsTargetInRange())
                {
                    TryAttack();
                }
            }

            // Look at target
            if (_lookAtTarget && _target != null && _isAttacking)
            {
                LookAtTarget();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos)
            {
                return;
            }

            // Draw attack range
            Gizmos.color = _attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // Draw line to target
            if (_target != null)
            {
                Gizmos.color = IsTargetInRange() ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, _target.position);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to attack the target. Returns true if attack was successful.
        /// </summary>
        /// <returns>True if attack was initiated.</returns>
        public bool TryAttack()
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

            PerformAttack();
            return true;
        }

        /// <summary>
        /// Performs an attack on the target.
        /// </summary>
        public void PerformAttack()
        {
            _isAttacking = true;
            _canAttack = false;
            _attackTimer = 0f;

            // Show attack visual
            if (_attackVisual != null)
            {
                _attackVisual.SetActive(true);
                _attackVisualTimer = 0f;
            }

            // Fire attack started event
            OnAttackStarted?.Invoke();

            // Deal damage to target
            DealDamage();

            // Fire attack hit event
            OnAttackHit?.Invoke(_target);

            // End attack
            _isAttacking = false;
            OnAttackEnded?.Invoke();
        }

        /// <summary>
        /// Sets the target to attack.
        /// </summary>
        /// <param name="target">The target transform.</param>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// Sets the target to attack by finding a GameObject with the specified tag.
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
            _attackTimer = 0f;
        }

        /// <summary>
        /// Sets the attack cooldown immediately.
        /// </summary>
        public void SetOnCooldown()
        {
            _canAttack = false;
            _attackTimer = 0f;
        }

        #endregion

        #region Private Methods

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
        /// </summary>
        private void LookAtTarget()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 targetPosition = _target.position;
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

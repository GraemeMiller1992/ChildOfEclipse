using UnityEngine;
using UnityEngine.Events;
using ChildOfEclipse.Health;

namespace ChildOfEclipse.Health
{
    /// <summary>
    /// Defines where a respawnable entity should respawn.
    /// </summary>
    public enum RespawnLocation
    {
        /// <summary>Respawn at the entity's initial position when the game started</summary>
        OriginalPosition,
        /// <summary>Respawn at the last activated checkpoint</summary>
        LastCheckpoint,
        /// <summary>Use a custom spawn point specified in the inspector</summary>
        CustomSpawnPoint
    }

    /// <summary>
    /// Manages respawning for game objects with health. When the entity dies,
    /// it will automatically respawn after a configured delay at a specified location.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class RespawnableComponent : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [Tooltip("Where should this entity respawn?")]
        [SerializeField] private RespawnLocation respawnLocation = RespawnLocation.OriginalPosition;

        [Tooltip("Custom spawn point to use when RespawnLocation is set to CustomSpawnPoint")]
        [SerializeField] private Transform customSpawnPoint;

        [Tooltip("Delay in seconds before respawning after death")]
        [SerializeField] private float respawnDelay = 2f;

        [Tooltip("Should the entity be disabled during the respawn delay?")]
        [SerializeField] private bool disableDuringDelay = true;

        [Tooltip("Should health be fully restored on respawn?")]
        [SerializeField] private bool restoreFullHealth = true;

        [Tooltip("Should velocity be reset on respawn?")]
        [SerializeField] private bool resetVelocity = true;

        [Tooltip("Maximum number of times this entity can respawn (-1 for unlimited)")]
        [SerializeField] private int maxRespawnCount = -1;

        [Header("Events")]
        [Space]
        [Tooltip("Invoked just before respawning (passes respawn position)")]
        public UnityEvent<Vector3> OnBeforeRespawn;

        [Tooltip("Invoked just after respawning (passes respawn position)")]
        public UnityEvent<Vector3> OnAfterRespawn;

        [Tooltip("Invoked when the entity runs out of respawns")]
        public UnityEvent OnRespawnsExhausted;

        private HealthComponent _health;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private int _respawnCount = 0;
        private bool _isRespawning = false;
        private Coroutine _respawnCoroutine;

        /// <summary>
        /// Gets the current number of times this entity has respawned
        /// </summary>
        public int RespawnCount => _respawnCount;

        /// <summary>
        /// Gets whether this entity can still respawn
        /// </summary>
        public bool CanRespawn => maxRespawnCount < 0 || _respawnCount < maxRespawnCount;

        /// <summary>
        /// Gets the current checkpoint position
        /// </summary>
        public Vector3 CheckpointPosition { get; private set; }

        /// <summary>
        /// Gets whether a checkpoint has been set
        /// </summary>
        public bool HasCheckpoint { get; private set; }

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            CheckpointPosition = _initialPosition;
            Debug.Log($"{gameObject.name}: RespawnableComponent initialized. Initial position: {_initialPosition}, RespawnLocation: {respawnLocation}");
        }

        private void OnEnable()
        {
            _health.OnDeath.AddListener(HandleDeath);
            Debug.Log($"{gameObject.name}: RespawnableComponent enabled. Subscribed to death events.");
        }

        private void OnDisable()
        {
            _health.OnDeath.RemoveListener(HandleDeath);
            Debug.Log($"{gameObject.name}: RespawnableComponent disabled. Unsubscribed from death events.");
        }

        /// <summary>
        /// Set the checkpoint position
        /// </summary>
        public void SetCheckpointPosition(Vector3 position)
        {
            CheckpointPosition = position;
            HasCheckpoint = true;
            Debug.Log($"{gameObject.name}: Checkpoint set at: {position} (respawnLocation={respawnLocation})");
        }

        /// <summary>
        /// Clear the checkpoint
        /// </summary>
        public void ClearCheckpoint()
        {
            HasCheckpoint = false;
            Debug.Log("Checkpoint cleared");
        }

        /// <summary>
        /// Get the respawn position based on the configured respawn location
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            Vector3 result;

            switch (respawnLocation)
            {
                case RespawnLocation.OriginalPosition:
                    Debug.Log($"{gameObject.name}: Using OriginalPosition for respawn: {_initialPosition}");
                    result = _initialPosition;
                    break;

                case RespawnLocation.LastCheckpoint:
                    if (HasCheckpoint)
                    {
                        Debug.Log($"{gameObject.name}: Using LastCheckpoint for respawn: {CheckpointPosition}");
                        result = CheckpointPosition;
                    }
                    else
                    {
                        Debug.LogWarning($"{gameObject.name}: No checkpoint set, falling back to original position", this);
                        result = _initialPosition;
                    }
                    break;

                case RespawnLocation.CustomSpawnPoint:
                    if (customSpawnPoint != null)
                    {
                        Debug.Log($"{gameObject.name}: Using CustomSpawnPoint for respawn: {customSpawnPoint.position}");
                        result = customSpawnPoint.position;
                    }
                    else
                    {
                        Debug.LogWarning($"{gameObject.name}: No custom spawn point set, falling back to original position", this);
                        result = _initialPosition;
                    }
                    break;

                default:
                    result = _initialPosition;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Get the respawn rotation based on the configured respawn location
        /// </summary>
        public Quaternion GetRespawnRotation()
        {
            switch (respawnLocation)
            {
                case RespawnLocation.OriginalPosition:
                    return _initialRotation;

                case RespawnLocation.LastCheckpoint:
                    // Use initial rotation for checkpoint respawn
                    return _initialRotation;

                case RespawnLocation.CustomSpawnPoint:
                    if (customSpawnPoint != null)
                    {
                        return customSpawnPoint.rotation;
                    }
                    return _initialRotation;

                default:
                    return _initialRotation;
            }
        }

        /// <summary>
        /// Manually trigger a respawn
        /// </summary>
        public void Respawn()
        {
            if (!CanRespawn)
            {
                OnRespawnsExhausted?.Invoke();
                return;
            }

            Vector3 respawnPosition = GetRespawnPosition();
            Quaternion respawnRotation = GetRespawnRotation();

            Debug.Log($"{gameObject.name}: Respawning at position {respawnPosition}");

            OnBeforeRespawn?.Invoke(respawnPosition);

            // Re-enable if disabled
            if (!gameObject.activeSelf)
            {
                Debug.Log($"{gameObject.name}: Re-enabling GameObject");
                gameObject.SetActive(true);
            }

            // Set position and rotation
            transform.position = respawnPosition;
            transform.rotation = respawnRotation;

            // Reset velocity if configured
            if (resetVelocity)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.linearVelocity = Vector2.zero;
                    rb2d.angularVelocity = 0f;
                }
            }

            // Restore health if configured
            if (restoreFullHealth)
            {
                _health.Revive();
            }

            _respawnCount++;
            _isRespawning = false;

            Debug.Log($"{gameObject.name}: Respawn complete. Total respawns: {_respawnCount}");
            OnAfterRespawn?.Invoke(respawnPosition);
        }

        /// <summary>
        /// Manually trigger a respawn at a specific position
        /// </summary>
        public void RespawnAt(Vector3 position, Quaternion rotation)
        {
            if (_isRespawning)
            {
                return;
            }

            if (!CanRespawn)
            {
                OnRespawnsExhausted?.Invoke();
                return;
            }

            OnBeforeRespawn?.Invoke(position);

            // Re-enable if disabled
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Set position and rotation
            transform.position = position;
            transform.rotation = rotation;

            // Reset velocity if configured
            if (resetVelocity)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.linearVelocity = Vector2.zero;
                    rb2d.angularVelocity = 0f;
                }
            }

            // Restore health if configured
            if (restoreFullHealth)
            {
                _health.Revive();
            }

            _respawnCount++;
            _isRespawning = false;

            OnAfterRespawn?.Invoke(position);
        }

        /// <summary>
        /// Reset the respawn count (useful for level restarts)
        /// </summary>
        public void ResetRespawnCount()
        {
            _respawnCount = 0;
        }

        private void HandleDeath()
        {
            if (!CanRespawn)
            {
                OnRespawnsExhausted?.Invoke();
                return;
            }

            _isRespawning = true;
            Debug.Log($"{gameObject.name}: Death detected. Respawning in {respawnDelay} seconds at {GetRespawnPosition()}");

            if (disableDuringDelay)
            {
                gameObject.SetActive(false);
            }

            // Schedule respawn using coroutine (works even when GameObject is disabled)
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
            }
            _respawnCoroutine = StartCoroutine(RespawnCoroutine());
        }

        private System.Collections.IEnumerator RespawnCoroutine()
        {
            Debug.Log($"{gameObject.name}: Respawn timer started. Waiting {respawnDelay} seconds...");
            yield return new WaitForSeconds(respawnDelay);
            Debug.Log($"{gameObject.name}: Respawn timer complete. Executing respawn...");
            _isRespawning = false; // Reset flag before calling Respawn
            Respawn();
            _respawnCoroutine = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate inspector values to prevent invalid states
        /// </summary>
        private void OnValidate()
        {
            respawnDelay = Mathf.Max(0f, respawnDelay);
            
            if (respawnLocation == RespawnLocation.CustomSpawnPoint && customSpawnPoint == null)
            {
                Debug.LogWarning("RespawnLocation is set to CustomSpawnPoint but no spawn point is assigned", this);
            }
        }

        /// <summary>
        /// Draw gizmos to visualize respawn location
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 respawnPos;
            
            switch (respawnLocation)
            {
                case RespawnLocation.OriginalPosition:
                    // Can't determine original position in editor mode without playing
                    return;
                    
                case RespawnLocation.LastCheckpoint:
                    if (HasCheckpoint)
                    {
                        respawnPos = CheckpointPosition;
                    }
                    else
                    {
                        return;
                    }
                    break;
                    
                case RespawnLocation.CustomSpawnPoint:
                    if (customSpawnPoint != null)
                    {
                        respawnPos = customSpawnPoint.position;
                    }
                    else
                    {
                        return;
                    }
                    break;
                    
                default:
                    return;
            }

            // Draw respawn position indicator
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPos, 0.5f);
            Gizmos.DrawLine(respawnPos, respawnPos + Vector3.up * 2f);
            
            // Draw label
            UnityEditor.Handles.Label(respawnPos + Vector3.up * 2.2f, "Respawn Point");
        }
#endif
    }
}

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
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
    /// Defines which component types should be disabled during respawn delay.
    /// Multiple options can be selected.
    /// </summary>
    [System.Flags]
    public enum RespawnDisableComponents
    {
        /// <summary>No components will be disabled</summary>
        None = 0,
        /// <summary>Disable Renderer components (makes object invisible)</summary>
        Renderer = 1 << 0,
        /// <summary>Disable Collider components (3D colliders)</summary>
        Collider = 1 << 1,
        /// <summary>Disable Collider2D components (2D colliders)</summary>
        Collider2D = 1 << 2,
        /// <summary>Disable all standard components (Renderer, Collider, Collider2D)</summary>
        All = Renderer | Collider | Collider2D
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

        [Tooltip("Should the checkpoint be set to the starting position on Awake?")]
        [SerializeField] private bool setCheckpointOnStart = false;

        [Header("Component Disabling")]
        [Tooltip("Which components should be disabled during the respawn delay?")]
        [SerializeField] private RespawnDisableComponents componentsToDisable = RespawnDisableComponents.All;

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
        private float _respawnTimer = 0f;
        
        // Track which components were disabled so we can re-enable them
        private List<Renderer> _disabledRenderers = new List<Renderer>();
        private List<Collider> _disabledColliders = new List<Collider>();
        private List<Collider2D> _disabledColliders2D = new List<Collider2D>();
        
        // Track Rigidbody states for physics freezing
        private List<Rigidbody> _frozenRigidbodies = new List<Rigidbody>();
        private List<bool> _rigidbodyWasKinematic = new List<bool>();
        private List<RigidbodyInterpolation> _rigidbodyInterpolation = new List<RigidbodyInterpolation>();
        private List<Rigidbody2D> _frozenRigidbodies2D = new List<Rigidbody2D>();
        private List<bool> _rigidbody2DWasKinematic = new List<bool>();

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

        /// <summary>
        /// Gets whether this entity is currently in the respawn process (delay or just respawned).
        /// Other components can check this to pause their physics updates.
        /// </summary>
        public bool IsRespawning => _isRespawning || _respawnTimer > 0f;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            CheckpointPosition = _initialPosition;
            
            if (setCheckpointOnStart)
            {
                HasCheckpoint = true;
                Debug.Log($"{gameObject.name}: Checkpoint set on start at: {CheckpointPosition}");
            }
            
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

            // Unfreeze physics first
            UnfreezePhysics();

            // Re-enable components if they were disabled
            if (disableDuringDelay)
            {
                EnableObjectComponents();
            }

            // Set position and rotation using Rigidbody for proper physics sync
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = respawnPosition;
                rb.rotation = respawnRotation;
            }
            else
            {
                transform.position = respawnPosition;
                transform.rotation = respawnRotation;
            }

            // Reset velocity if configured
            if (resetVelocity)
            {
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

            // Unfreeze physics first
            UnfreezePhysics();

            // Re-enable components if they were disabled
            if (disableDuringDelay)
            {
                EnableObjectComponents();
            }

            // Set position and rotation using Rigidbody for proper physics sync
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = position;
                rb.rotation = rotation;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }

            // Reset velocity if configured
            if (resetVelocity)
            {
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
            _respawnTimer = 0f;
            Debug.Log($"{gameObject.name}: Death detected. Respawning in {respawnDelay} seconds at {GetRespawnPosition()}");

            // Always freeze physics to prevent continued movement during delay
            FreezePhysics();

            // Disable components if configured
            if (disableDuringDelay)
            {
                DisableObjectComponents();
            }
        }

        private void Update()
        {
            // Check if we're waiting to respawn
            if (_isRespawning)
            {
                _respawnTimer += Time.deltaTime;
                
                if (_respawnTimer >= respawnDelay)
                {
                    Debug.Log($"{gameObject.name}: Respawn timer complete. Executing respawn...");
                    _isRespawning = false;
                    _respawnTimer = 0f; // Reset timer after respawn
                    Respawn();
                }
            }
        }

        /// <summary>
        /// Disables visual and interactive components while keeping the GameObject active
        /// so that coroutines can continue running. Only disables components specified in componentsToDisable.
        /// </summary>
        /// <summary>
        /// Freezes all Rigidbodies to prevent continued movement during respawn delay.
        /// This is called regardless of disableDuringDelay setting.
        /// </summary>
        private void FreezePhysics()
        {
            // Clear previous tracking lists
            _frozenRigidbodies.Clear();
            _rigidbodyWasKinematic.Clear();
            _rigidbodyInterpolation.Clear();
            _frozenRigidbodies2D.Clear();
            _rigidbody2DWasKinematic.Clear();

            // Freeze all Rigidbodies to prevent continued movement during delay
            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                _frozenRigidbodies.Add(rb);
                _rigidbodyWasKinematic.Add(rb.isKinematic);
                _rigidbodyInterpolation.Add(rb.interpolation);
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Freeze all Rigidbody2Ds to prevent continued movement during delay
            Rigidbody2D[] rigidbodies2D = GetComponentsInChildren<Rigidbody2D>();
            foreach (Rigidbody2D rb2d in rigidbodies2D)
            {
                _frozenRigidbodies2D.Add(rb2d);
                _rigidbody2DWasKinematic.Add(rb2d.isKinematic);
                rb2d.isKinematic = true;
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }

            Debug.Log($"{gameObject.name}: Physics frozen during respawn delay");
        }

        /// <summary>
        /// Unfreezes all Rigidbodies after respawn is complete.
        /// </summary>
        private void UnfreezePhysics()
        {
            // Unfreeze Rigidbodies and restore their original isKinematic and interpolation states
            for (int i = 0; i < _frozenRigidbodies.Count; i++)
            {
                Rigidbody rb = _frozenRigidbodies[i];
                if (rb != null && i < _rigidbodyWasKinematic.Count && i < _rigidbodyInterpolation.Count)
                {
                    rb.isKinematic = _rigidbodyWasKinematic[i];
                    rb.interpolation = _rigidbodyInterpolation[i];
                }
            }
            _frozenRigidbodies.Clear();
            _rigidbodyWasKinematic.Clear();
            _rigidbodyInterpolation.Clear();

            // Unfreeze Rigidbody2Ds and restore their original isKinematic state
            for (int i = 0; i < _frozenRigidbodies2D.Count; i++)
            {
                Rigidbody2D rb2d = _frozenRigidbodies2D[i];
                if (rb2d != null && i < _rigidbody2DWasKinematic.Count)
                {
                    rb2d.isKinematic = _rigidbody2DWasKinematic[i];
                }
            }
            _frozenRigidbodies2D.Clear();
            _rigidbody2DWasKinematic.Clear();

            Debug.Log($"{gameObject.name}: Physics unfrozen after respawn");
        }

        private void DisableObjectComponents()
        {
            // Clear previous tracking lists
            _disabledRenderers.Clear();
            _disabledColliders.Clear();
            _disabledColliders2D.Clear();

            // Disable renderers if specified
            if ((componentsToDisable & RespawnDisableComponents.Renderer) != 0)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.enabled)
                    {
                        renderer.enabled = false;
                        _disabledRenderers.Add(renderer);
                    }
                }
            }

            // Disable 3D colliders if specified
            if ((componentsToDisable & RespawnDisableComponents.Collider) != 0)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();
                foreach (Collider collider in colliders)
                {
                    if (collider.enabled)
                    {
                        collider.enabled = false;
                        _disabledColliders.Add(collider);
                    }
                }
            }

            // Disable 2D colliders if specified
            if ((componentsToDisable & RespawnDisableComponents.Collider2D) != 0)
            {
                Collider2D[] colliders2D = GetComponentsInChildren<Collider2D>();
                foreach (Collider2D collider2D in colliders2D)
                {
                    if (collider2D.enabled)
                    {
                        collider2D.enabled = false;
                        _disabledColliders2D.Add(collider2D);
                    }
                }
            }

            Debug.Log($"{gameObject.name}: Components disabled during respawn delay ({componentsToDisable})");
        }

        /// <summary>
        /// Re-enables visual and interactive components after respawn.
        /// Only re-enables components that were actually disabled.
        /// </summary>
        private void EnableObjectComponents()
        {
            // Re-enable renderers that were disabled
            foreach (Renderer renderer in _disabledRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
            _disabledRenderers.Clear();

            // Re-enable 3D colliders that were disabled
            foreach (Collider collider in _disabledColliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            _disabledColliders.Clear();

            // Re-enable 2D colliders that were disabled
            foreach (Collider2D collider2D in _disabledColliders2D)
            {
                if (collider2D != null)
                {
                    collider2D.enabled = true;
                }
            }
            _disabledColliders2D.Clear();

            Debug.Log($"{gameObject.name}: Components re-enabled after respawn");
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

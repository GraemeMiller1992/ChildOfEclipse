using UnityEngine;
using UnityEngine.Events;
using ChildOfEclipse.Health;

namespace ChildOfEclipse.Health
{
    /// <summary>
    /// Represents a checkpoint in the game world. When a player enters the checkpoint's trigger,
    /// it becomes the active spawn point for respawnable entities.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [Tooltip("Should this checkpoint only be activated once?")]
        [SerializeField] private bool oneTimeUse = false;

        [Tooltip("Should the checkpoint automatically activate when entered?")]
        [SerializeField] private bool autoActivate = true;

        [Tooltip("Tag of objects that can activate this checkpoint")]
        [SerializeField] private string activatorTag = "Player";

        [Tooltip("Offset from checkpoint position for respawn")]
        [SerializeField] private Vector3 respawnOffset = Vector3.zero;

        [Header("Visual Feedback")]
        [Tooltip("Renderer to change color when activated")]
        [SerializeField] private Renderer checkpointRenderer;

        [Tooltip("Color when checkpoint is inactive")]
        [SerializeField] private Color inactiveColor = Color.red;

        [Tooltip("Color when checkpoint is active")]
        [SerializeField] private Color activeColor = Color.green;

        [Tooltip("Light to change when activated")]
        [SerializeField] private Light checkpointLight;

        [Tooltip("Light color when inactive")]
        [SerializeField] private Color inactiveLightColor = Color.red;

        [Tooltip("Light color when active")]
        [SerializeField] private Color activeLightColor = Color.green;

        [Tooltip("Particle system to play when activated")]
        [SerializeField] private ParticleSystem activationParticles;

        [Header("Events")]
        [Space]
        [Tooltip("Invoked when this checkpoint is activated")]
        public UnityEvent OnCheckpointActivated;

        [Tooltip("Invoked when this checkpoint is deactivated (if reusable)")]
        public UnityEvent OnCheckpointDeactivated;

        /// <summary>
        /// Gets whether this checkpoint is currently active
        /// </summary>
        public bool IsActive { get; private set; } = false;

        /// <summary>
        /// Gets whether this checkpoint has been used (for one-time checkpoints)
        /// </summary>
        public bool HasBeenUsed { get; private set; } = false;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            // Ensure the collider is a trigger
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            // Set initial visual state
            UpdateVisualState(false);
        }

        private void Start()
        {
            // Check if this is already the active checkpoint by finding the player
            GameObject player = GameObject.FindGameObjectWithTag(activatorTag);
            if (player != null)
            {
                RespawnableComponent respawnable = player.GetComponent<RespawnableComponent>();
                if (respawnable != null && respawnable.HasCheckpoint)
                {
                    Vector3 currentCheckpoint = respawnable.CheckpointPosition;
                    if (Vector3.Distance(currentCheckpoint, transform.position + respawnOffset) < 0.1f)
                    {
                        ActivateCheckpoint(null);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoActivate)
            {
                return;
            }

            if (other.CompareTag(activatorTag))
            {
                ActivateCheckpoint(other.gameObject);
            }
        }

        /// <summary>
        /// Manually activate this checkpoint
        /// </summary>
        /// <param name="activator">The object that activated this checkpoint (for setting respawn position)</param>
        public void ActivateCheckpoint(GameObject activator = null)
        {
            if (oneTimeUse && HasBeenUsed)
            {
                return;
            }

            if (IsActive)
            {
                return;
            }

            // Deactivate all other checkpoints
            Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            foreach (Checkpoint checkpoint in allCheckpoints)
            {
                if (checkpoint != this && checkpoint.IsActive)
                {
                    checkpoint.DeactivateCheckpoint();
                }
            }

            IsActive = true;
            HasBeenUsed = true;

            // Set checkpoint position on the activator's RespawnableComponent
            if (activator != null)
            {
                RespawnableComponent respawnable = activator.GetComponent<RespawnableComponent>();
                if (respawnable != null)
                {
                    respawnable.SetCheckpointPosition(transform.position + respawnOffset);
                }
            }

            UpdateVisualState(true);

            // Play activation particles
            if (activationParticles != null)
            {
                activationParticles.Play();
            }

            OnCheckpointActivated?.Invoke();
        }

        /// <summary>
        /// Deactivate this checkpoint (useful for reusable checkpoints)
        /// </summary>
        public void DeactivateCheckpoint()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;

            UpdateVisualState(false);

            OnCheckpointDeactivated?.Invoke();
        }

        /// <summary>
        /// Reset the checkpoint state (useful for level restarts)
        /// </summary>
        public void ResetCheckpoint()
        {
            IsActive = false;
            HasBeenUsed = false;
            UpdateVisualState(false);
        }

        /// <summary>
        /// Update the visual state of the checkpoint
        /// </summary>
        private void UpdateVisualState(bool active)
        {
            // Update renderer color
            if (checkpointRenderer != null)
            {
                if (checkpointRenderer.material.HasProperty("_Color"))
                {
                    checkpointRenderer.material.color = active ? activeColor : inactiveColor;
                }
                else if (checkpointRenderer.material.HasProperty("_BaseColor"))
                {
                    checkpointRenderer.material.SetColor("_BaseColor", active ? activeColor : inactiveColor);
                }
            }

            // Update light color
            if (checkpointLight != null)
            {
                checkpointLight.color = active ? activeLightColor : inactiveLightColor;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate inspector values to prevent invalid states
        /// </summary>
        private void OnValidate()
        {
            // Ensure collider is set to trigger in editor
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }
            
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        /// <summary>
        /// Draw gizmos to visualize the checkpoint area
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = IsActive ? activeColor : inactiveColor;
            
            if (_collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (_collider is SphereCollider sphereCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
            else if (_collider is CapsuleCollider capsuleCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Vector3 center = capsuleCollider.center;
                float height = capsuleCollider.height;
                float radius = capsuleCollider.radius;
                
                // Draw capsule approximation
                Vector3 point1 = center + Vector3.up * (height / 2 - radius);
                Vector3 point2 = center - Vector3.up * (height / 2 - radius);
                
                Gizmos.DrawWireSphere(point1, radius);
                Gizmos.DrawWireSphere(point2, radius);
                
                // Draw connecting lines
                Gizmos.DrawLine(point1 + Vector3.right * radius, point2 + Vector3.right * radius);
                Gizmos.DrawLine(point1 - Vector3.right * radius, point2 - Vector3.right * radius);
                Gizmos.DrawLine(point1 + Vector3.forward * radius, point2 + Vector3.forward * radius);
                Gizmos.DrawLine(point1 - Vector3.forward * radius, point2 - Vector3.forward * radius);
            }

            // Draw spawn point indicator
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
            
            // Draw label
            string label = oneTimeUse ? "Checkpoint (One-time)" : "Checkpoint (Reusable)";
            if (IsActive)
            {
                label += " [ACTIVE]";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.7f, label);
        }
#endif
    }
}

using UnityEngine;
using Actions;
using ChildOfEclipse;

namespace Actions
{
    /// <summary>
    /// Triggers an ActionRunner when a collider enters or exits a trigger zone.
    /// Supports filtering by tag, layer, and triggering on enter/exit events.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerAction : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("Trigger when objects enter the collider")]
        public bool triggerOnEnter = true;

        [Tooltip("Trigger when objects exit the collider")]
        public bool triggerOnExit = false;

        [Tooltip("Whether to trigger only once and then disable")]
        public bool triggerOnce = false;

        [Tooltip("Whether to destroy this component after triggering")]
        public bool destroyOnTrigger = false;

        [Header("Filter Settings")]
        [Tooltip("Tag filter - only trigger when objects with this tag enter/exit (empty = any tag)")]
        public string filterTag = string.Empty;

        [Tooltip("Layer filter - only trigger when objects on this layer enter/exit (empty = any layer)")]
        public LayerMask filterLayer = 0;

        [Tooltip("Whether to trigger only when the player enters/exits")]
        public bool playerOnly = false;

        [Header("Action Runner")]
        [SerializeReference]
        [Tooltip("The ActionRunner to execute when the trigger is activated")]
        public ActionRunner actionRunner = new ActionRunner();

        [Header("Debug")]
        [Tooltip("Whether to log debug messages")]
        public bool debugMode = false;

        private bool _hasTriggered = false;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            if (_collider == null)
            {
                Debug.LogError("TriggerAction: No Collider component found!");
                return;
            }

            if (!_collider.isTrigger)
            {
                Debug.LogWarning("TriggerAction: Collider is not set to trigger. Setting to trigger mode.");
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter)
            {
                return;
            }

            if (_hasTriggered && triggerOnce)
            {
                return;
            }

            if (!ShouldTrigger(other))
            {
                return;
            }

            if (debugMode)
            {
                Debug.Log($"TriggerAction: {other.name} entered trigger zone", gameObject);
            }

            TriggerActions(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!triggerOnExit)
            {
                return;
            }

            if (_hasTriggered && triggerOnce)
            {
                return;
            }

            if (!ShouldTrigger(other))
            {
                return;
            }

            if (debugMode)
            {
                Debug.Log($"TriggerAction: {other.name} exited trigger zone", gameObject);
            }

            TriggerActions(other.gameObject);
        }

        /// <summary>
        /// Check if the collider should trigger based on filter settings
        /// </summary>
        private bool ShouldTrigger(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            // Check tag filter
            if (!string.IsNullOrEmpty(filterTag) && !other.CompareTag(filterTag))
            {
                if (debugMode)
                {
                    Debug.Log($"TriggerAction: {other.name} filtered out by tag (expected: {filterTag}, actual: {other.tag})");
                }
                return false;
            }

            // Check layer filter
            if (filterLayer != 0 && ((1 << other.gameObject.layer) & filterLayer) == 0)
            {
                if (debugMode)
                {
                    Debug.Log($"TriggerAction: {other.name} filtered out by layer");
                }
                return false;
            }

            // Check player only filter
            if (playerOnly)
            {
                // Check if the object has a player controller or is tagged as Player
                bool isPlayer = other.CompareTag("Player") || 
                                other.GetComponent<PlayerInputSingleton>() != null ||
                                other.GetComponent<RigidbodyPlayerController>() != null;
                
                if (!isPlayer)
                {
                    if (debugMode)
                    {
                        Debug.Log($"TriggerAction: {other.name} filtered out (player only mode)");
                    }
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Trigger the action runner
        /// </summary>
        private void TriggerActions(GameObject triggerObject)
        {
            if (actionRunner == null)
            {
                Debug.LogWarning("TriggerAction: ActionRunner is null");
                return;
            }

            if (actionRunner.IsEmpty())
            {
                Debug.LogWarning("TriggerAction: No actions to run");
                return;
            }

            if (debugMode)
            {
                Debug.Log($"TriggerAction: Executing {actionRunner.ActionCount} action(s)", gameObject);
            }

            // Pass the trigger object as context to the actions
            actionRunner.RunAll(triggerObject);

            if (triggerOnce)
            {
                _hasTriggered = true;
            }

            if (destroyOnTrigger)
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// Manually trigger the actions (for external calls)
        /// </summary>
        public void ManualTrigger(GameObject context = null)
        {
            if (_hasTriggered && triggerOnce)
            {
                Debug.LogWarning("TriggerAction: Already triggered and triggerOnce is enabled");
                return;
            }

            if (debugMode)
            {
                Debug.Log("TriggerAction: Manual trigger called", gameObject);
            }

            TriggerActions(context ?? gameObject);
        }

        /// <summary>
        /// Reset the trigger state (for re-triggering)
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            if (debugMode)
            {
                Debug.Log("TriggerAction: Trigger state reset", gameObject);
            }
        }

        /// <summary>
        /// Enable or disable the collider
        /// </summary>
        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }

        private void OnDrawGizmos()
        {
            if (_collider != null)
            {
                Gizmos.color = debugMode ? Color.yellow : Color.cyan;
                Gizmos.matrix = transform.localToWorldMatrix;
                
                if (_collider is BoxCollider box)
                {
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (_collider is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
                else if (_collider is CapsuleCollider capsule)
                {
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_collider != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                
                if (_collider is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (_collider is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
                else if (_collider is CapsuleCollider capsule)
                {
                    Gizmos.DrawSphere(capsule.center, capsule.radius);
                }
            }
        }
    }
}

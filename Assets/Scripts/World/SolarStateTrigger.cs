using UnityEngine;

namespace World
{
    /// <summary>
    /// Trigger zone that changes the SolarState of objects entering the collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SolarStateTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("The solar state to apply when objects enter the trigger")]
        public SolarStateValue targetState = SolarStateValue.Sun;

        [Tooltip("Trigger when objects enter the collider")]
        public bool triggerOnEnter = true;

        [Tooltip("Trigger when objects exit the collider")]
        public bool triggerOnExit = false;

        [Tooltip("Whether to trigger only once and then disable")]
        public bool triggerOnce = false;

        [Header("Filter Settings")]
        [Tooltip("Tag filter - only trigger when objects with this tag enter/exit (empty = any tag)")]
        public string filterTag = string.Empty;

        [Tooltip("Layer filter - only trigger when objects on this layer enter/exit (empty = any layer)")]
        public LayerMask filterLayer = 0;

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
                Debug.LogError("SolarStateTrigger: No Collider component found!");
                return;
            }

            if (!_collider.isTrigger)
            {
                Debug.LogWarning("SolarStateTrigger: Collider is not set to trigger. Setting to trigger mode.");
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
                Debug.Log($"SolarStateTrigger: {other.name} entered trigger zone", gameObject);
            }

            ChangeSolarState(other.gameObject);
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
                Debug.Log($"SolarStateTrigger: {other.name} exited trigger zone", gameObject);
            }

            ChangeSolarState(other.gameObject);
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
                    Debug.Log($"SolarStateTrigger: {other.name} filtered out by tag (expected: {filterTag}, actual: {other.tag})");
                }
                return false;
            }

            // Check layer filter
            if (filterLayer != 0 && ((1 << other.gameObject.layer) & filterLayer) == 0)
            {
                if (debugMode)
                {
                    Debug.Log($"SolarStateTrigger: {other.name} filtered out by layer");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Change the solar state of the entered object
        /// </summary>
        private void ChangeSolarState(GameObject targetObject)
        {
            SolarState solarState = targetObject.GetComponent<SolarState>();
            
            if (solarState == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"SolarStateTrigger: {targetObject.name} does not have a SolarState component", gameObject);
                }
                return;
            }

            if (debugMode)
            {
                Debug.Log($"SolarStateTrigger: Changing {targetObject.name} from {solarState.CurrentState} to {targetState}", gameObject);
            }

            // Change the state based on the target state
            switch (targetState)
            {
                case SolarStateValue.Sun:
                    solarState.SetSunState();
                    break;
                case SolarStateValue.Moon:
                    solarState.SetMoonState();
                    break;
                case SolarStateValue.Eclipse:
                    solarState.SetEclipseState();
                    break;
            }

            if (triggerOnce)
            {
                _hasTriggered = true;
            }
        }

        /// <summary>
        /// Manually trigger the state change (for external calls)
        /// </summary>
        public void ManualTrigger(GameObject targetObject)
        {
            if (_hasTriggered && triggerOnce)
            {
                Debug.LogWarning("SolarStateTrigger: Already triggered and triggerOnce is enabled");
                return;
            }

            if (debugMode)
            {
                Debug.Log($"SolarStateTrigger: Manual trigger called for {targetObject?.name}", gameObject);
            }

            if (targetObject != null)
            {
                ChangeSolarState(targetObject);
            }
        }

        /// <summary>
        /// Reset the trigger state (for re-triggering)
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            if (debugMode)
            {
                Debug.Log("SolarStateTrigger: Trigger state reset", gameObject);
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
                // Color based on target state
                Gizmos.color = targetState switch
                {
                    SolarStateValue.Sun => Color.yellow,
                    SolarStateValue.Moon => Color.blue,
                    SolarStateValue.Eclipse => Color.magenta,
                    _ => Color.cyan
                };
                
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
                // Semi-transparent fill based on target state
                Gizmos.color = targetState switch
                {
                    SolarStateValue.Sun => new Color(1f, 1f, 0f, 0.3f),
                    SolarStateValue.Moon => new Color(0f, 0.5f, 1f, 0.3f),
                    SolarStateValue.Eclipse => new Color(1f, 0f, 1f, 0.3f),
                    _ => new Color(0f, 1f, 1f, 0.3f)
                };
                
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

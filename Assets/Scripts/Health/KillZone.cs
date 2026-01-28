using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using ChildOfEclipse.Health;

namespace ChildOfEclipse.Health
{
    /// <summary>
    /// A kill zone that instantly kills any game object with a HealthComponent
    /// that enters its trigger collider. Useful for lava, bottomless pits, etc.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        [Header("Kill Zone Settings")]
        [Tooltip("Should the kill zone only kill objects with specific tags? Leave empty to kill all.")]
        [SerializeField] private string[] targetTags;

        [Tooltip("Should the kill zone only kill objects on specific layers? Leave empty to kill all.")]
        [SerializeField] private LayerMask targetLayers = -1;

        [Tooltip("Should the kill zone only trigger once per object?")]
        [SerializeField] private bool oneTimeKill = false;

        [Tooltip("Delay in seconds before killing (0 for instant)")]
        [SerializeField] private float killDelay = 0f;

        [Header("Events")]
        [Space]
        [Tooltip("Invoked when an object enters the kill zone (passes the object)")]
        public UnityEvent<GameObject> OnObjectEntered;

        [Tooltip("Invoked when an object is killed by this zone (passes the object)")]
        public UnityEvent<GameObject> OnObjectKilled;

        private Collider _collider;
        private HashSet<GameObject> _killedObjects = new HashSet<GameObject>();

        /// <summary>
        /// Gets the number of objects currently in the kill zone
        /// </summary>
        public int ObjectCount => _killedObjects.Count;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            // Ensure the collider is a trigger
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject target = other.gameObject;

            // Check if already killed (for one-time kill)
            if (oneTimeKill && _killedObjects.Contains(target))
            {
                return;
            }

            // Check tag filter
            if (targetTags != null && targetTags.Length > 0)
            {
                bool tagMatch = false;
                foreach (string tag in targetTags)
                {
                    if (target.CompareTag(tag))
                    {
                        tagMatch = true;
                        break;
                    }
                }
                if (!tagMatch)
                {
                    return;
                }
            }

            // Check layer filter
            if (targetLayers != (targetLayers | (1 << target.layer)))
            {
                return;
            }

            // Try to get HealthComponent
            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                return;
            }

            // Check if already dead
            if (health.IsDead)
            {
                return;
            }

            // Fire entered event
            OnObjectEntered?.Invoke(target);

            // Kill the object
            if (killDelay > 0)
            {
                StartCoroutine(KillWithDelay(health, target));
            }
            else
            {
                KillObject(health, target);
            }
        }

        private IEnumerator KillWithDelay(HealthComponent health, GameObject target)
        {
            yield return new WaitForSeconds(killDelay);
            
            // Double-check the object still exists and is alive
            if (health != null && target != null && !health.IsDead)
            {
                KillObject(health, target);
            }
        }

        private void KillObject(HealthComponent health, GameObject target)
        {
            // Kill the object
            health.Kill();

            // Track killed objects for one-time kill
            if (oneTimeKill)
            {
                _killedObjects.Add(target);
            }

            // Fire killed event
            OnObjectKilled?.Invoke(target);
        }

        /// <summary>
        /// Manually kill a specific object
        /// </summary>
        public void KillTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health != null && !health.IsDead)
            {
                KillObject(health, target);
            }
        }

        /// <summary>
        /// Reset the kill zone (clears tracked objects for one-time kill)
        /// </summary>
        public void ResetKillZone()
        {
            _killedObjects.Clear();
        }

        /// <summary>
        /// Remove a specific object from the killed list (allows it to be killed again)
        /// </summary>
        public void RemoveFromKilledList(GameObject target)
        {
            if (target != null)
            {
                _killedObjects.Remove(target);
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

            // Ensure kill delay is non-negative
            killDelay = Mathf.Max(0f, killDelay);
        }

        /// <summary>
        /// Draw gizmos to visualize the kill zone
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            
            if (_collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (_collider is SphereCollider sphereCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
                Gizmos.color = Color.red;
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

            // Draw label
            string label = "Kill Zone";
            if (oneTimeKill)
            {
                label += " (One-time)";
            }
            if (killDelay > 0)
            {
                label += $" [{killDelay}s delay]";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }
#endif
    }
}

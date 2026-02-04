using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using ChildOfEclipse.Health;

namespace ChildOfEclipse.Health
{
    /// <summary>
    /// A damage zone that uses sphere casting to detect and damage objects with HealthComponents.
    /// Like a laser beam that damages anything in its path. The sphere cast extends from the
    /// transform position in the forward direction of the transform.
    /// </summary>
    public class SphereCastDamageZone : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Amount of damage to apply per hit")]
        [SerializeField] private float damageAmount = 10f;

        [Tooltip("How often to apply damage (in seconds). 0 = once per frame")]
        [SerializeField] private float damageInterval = 0.1f;

        [Tooltip("Radius of the sphere cast (width of the laser)")]
        [SerializeField] private float sphereRadius = 0.5f;

        [Tooltip("Maximum distance of the sphere cast")]
        [SerializeField] private float maxDistance = 10f;

        [Tooltip("Direction of the sphere cast in local space. Default is forward.")]
        [SerializeField] private Vector3 castDirection = Vector3.forward;

        [Header("Target Filtering")]
        [Tooltip("Should the damage zone only damage objects with specific tags? Leave empty to damage all.")]
        [SerializeField] private string[] targetTags;

        [Tooltip("Should the damage zone only damage objects on specific layers?")]
        [SerializeField] private LayerMask targetLayers = -1;

        [Tooltip("Should the damage zone only damage each target once per interval?")]
        [SerializeField] private bool damageOncePerInterval = true;

        [Header("Visual Settings")]
        [Tooltip("Show the sphere cast in the scene view")]
        [SerializeField] private bool showGizmos = true;

        [Tooltip("Color for the gizmo visualization")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.5f);

        [Header("Events")]
        [Space]
        [Tooltip("Invoked when an object is damaged (passes the object and damage amount)")]
        public UnityEvent<GameObject, float> OnObjectDamaged;

        [Tooltip("Invoked when the damage zone hits something (passes the object)")]
        public UnityEvent<GameObject> OnHitDetected;

        [Tooltip("Invoked each update with all currently hit objects")]
        public UnityEvent<List<GameObject>> OnObjectsInZone;

        private float _lastDamageTime;
        private List<GameObject> _hitObjects = new List<GameObject>();
        private Dictionary<GameObject, float> _lastDamageTimes = new Dictionary<GameObject, float>();

        /// <summary>
        /// Gets the current list of objects being hit by the sphere cast
        /// </summary>
        public List<GameObject> HitObjects => new List<GameObject>(_hitObjects);

        /// <summary>
        /// Gets whether the damage zone is currently hitting any objects
        /// </summary>
        public bool IsHittingObjects => _hitObjects.Count > 0;

        private void FixedUpdate()
        {
            PerformSphereCast();
        }

        private void PerformSphereCast()
        {
            _hitObjects.Clear();

            // Calculate world space direction
            Vector3 worldDirection = transform.TransformDirection(castDirection.normalized);

            // Perform the sphere cast
            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position,
                sphereRadius,
                worldDirection,
                maxDistance,
                targetLayers
            );

            foreach (RaycastHit hit in hits)
            {
                GameObject target = hit.collider.gameObject;

                // Skip if this is the zone's own collider
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject == gameObject)
                {
                    continue;
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
                        continue;
                    }
                }

                // Try to get HealthComponent
                HealthComponent health = target.GetComponent<HealthComponent>();
                if (health == null)
                {
                    continue;
                }

                // Check if already dead
                if (health.IsDead)
                {
                    continue;
                }

                // Add to hit objects list
                _hitObjects.Add(target);

                // Fire hit detected event
                OnHitDetected?.Invoke(target);

                // Apply damage
                if (CanDamageTarget(target))
                {
                    ApplyDamage(health, target);
                }
            }

            // Fire objects in zone event
            if (_hitObjects.Count > 0)
            {
                OnObjectsInZone?.Invoke(new List<GameObject>(_hitObjects));
            }
        }

        private bool CanDamageTarget(GameObject target)
        {
            // If damage interval is 0, always damage
            if (damageInterval <= 0f)
            {
                return true;
            }

            // If not tracking per-target, check global timer
            if (!damageOncePerInterval)
            {
                return Time.time >= _lastDamageTime + damageInterval;
            }

            // Check per-target timer
            if (_lastDamageTimes.TryGetValue(target, out float lastDamageTime))
            {
                return Time.time >= lastDamageTime + damageInterval;
            }

            return true;
        }

        private void ApplyDamage(HealthComponent health, GameObject target)
        {
            // Apply damage
            health.TakeDamage(damageAmount);

            // Update damage timers
            _lastDamageTime = Time.time;
            if (damageOncePerInterval)
            {
                _lastDamageTimes[target] = Time.time;
            }

            // Fire damage event
            OnObjectDamaged?.Invoke(target, damageAmount);

            // Clean up dead targets from tracking
            if (health.IsDead)
            {
                _lastDamageTimes.Remove(target);
            }
        }

        /// <summary>
        /// Manually trigger a sphere cast and apply damage
        /// </summary>
        public void TriggerDamage()
        {
            PerformSphereCast();
        }

        /// <summary>
        /// Reset the damage tracking timers
        /// </summary>
        public void ResetDamageTimers()
        {
            _lastDamageTime = 0f;
            _lastDamageTimes.Clear();
        }

        /// <summary>
        /// Set the damage amount
        /// </summary>
        public void SetDamageAmount(float amount)
        {
            damageAmount = Mathf.Max(0f, amount);
        }

        /// <summary>
        /// Set the sphere radius
        /// </summary>
        public void SetSphereRadius(float radius)
        {
            sphereRadius = Mathf.Max(0f, radius);
        }

        /// <summary>
        /// Set the maximum distance
        /// </summary>
        public void SetMaxDistance(float distance)
        {
            maxDistance = Mathf.Max(0f, distance);
        }

        /// <summary>
        /// Set the cast direction in local space
        /// </summary>
        public void SetCastDirection(Vector3 direction)
        {
            castDirection = direction.normalized;
        }

        /// <summary>
        /// Remove a target from damage tracking (allows it to be damaged immediately)
        /// </summary>
        public void RemoveTargetFromTracking(GameObject target)
        {
            if (target != null)
            {
                _lastDamageTimes.Remove(target);
            }
        }

        /// <summary>
        /// Clear all tracked targets
        /// </summary>
        public void ClearAllTracking()
        {
            _lastDamageTimes.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate inspector values to prevent invalid states
        /// </summary>
        private void OnValidate()
        {
            damageAmount = Mathf.Max(0f, damageAmount);
            damageInterval = Mathf.Max(0f, damageInterval);
            sphereRadius = Mathf.Max(0f, sphereRadius);
            maxDistance = Mathf.Max(0f, maxDistance);
            castDirection = castDirection.normalized;
        }

        /// <summary>
        /// Draw gizmos to visualize the sphere cast
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            Vector3 worldDirection = transform.TransformDirection(castDirection.normalized);
            Vector3 endPoint = transform.position + worldDirection * maxDistance;

            // Draw the main beam line
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, endPoint);

            // Draw sphere at start
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.5f);
            Gizmos.DrawWireSphere(transform.position, sphereRadius);

            // Draw sphere at end
            Gizmos.DrawWireSphere(endPoint, sphereRadius);

            // Draw cylinder representation
            UnityEditor.Handles.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.3f);
            UnityEditor.Handles.DrawWireDisc(transform.position, worldDirection, sphereRadius);
            UnityEditor.Handles.DrawWireDisc(endPoint, worldDirection, sphereRadius);

            // Draw label
            string label = $"SphereCast Damage\nDamage: {damageAmount}\nInterval: {damageInterval}s\nRadius: {sphereRadius}\nDistance: {maxDistance}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }
#endif
    }
}

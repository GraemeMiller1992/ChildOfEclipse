using UnityEngine;
using ChildOfEclipse.Coin;

namespace ChildOfEclipse.Coin
{
    /// <summary>
    /// Represents a collectible coin in the game world.
    /// Attach this component to coin GameObjects. Requires a Collider set as a trigger.
    /// When collected, it notifies the CoinManager and destroys itself.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Coin : MonoBehaviour
    {
        [Header("Coin Settings")]
        [Tooltip("How many coins this pickup is worth")]
        [SerializeField] private int value = 1;

        [Tooltip("Should the coin bob up and down?")]
        [SerializeField] private bool doBob = true;

        [Tooltip("Speed of the bobbing animation")]
        [SerializeField] private float bobSpeed = 2f;

        [Tooltip("Height of the bobbing animation")]
        [SerializeField] private float bobHeight = 0.15f;

        [Tooltip("Rotation speed in degrees per second")]
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Collection Settings")]
        [Tooltip("Should the coin attract towards the collector before being picked up?")]
        [SerializeField] private bool attractToCollector = true;

        [Tooltip("Speed at which the coin moves towards the collector")]
        [SerializeField] private float attractSpeed = 15f;

        [Tooltip("Distance at which the coin snaps to the collector")]
        [SerializeField] private float snapDistance = 0.3f;

        /// <summary>
        /// How many coins this pickup is worth
        /// </summary>
        public int Value => value;

        private Vector3 _startPosition;
        private bool _isBeingCollected;
        private Transform _collectorTransform;

        private void Start()
        {
            _startPosition = transform.position;

            // Validate that the collider is a trigger
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning(
                    $"[Coin] Collider on '{gameObject.name}' is not a trigger. " +
                    "CoinCollector uses OverlapSphere, so trigger is not strictly required, " +
                    "but the collider should be on the Coin layer.",
                    this);
            }
        }

        private void Update()
        {
            if (_isBeingCollected && _collectorTransform != null)
            {
                AttractTowardsCollector();
            }
            else
            {
                BobAndRotate();
            }
        }

        /// <summary>
        /// Called by CoinCollector to begin the collection process.
        /// Initiates attraction towards the collector.
        /// </summary>
        /// <param name="collector">The transform of the object collecting this coin</param>
        public void Collect(Transform collector)
        {
            if (_isBeingCollected) return;

            _isBeingCollected = true;
            _collectorTransform = collector;

            // If no attraction, collect immediately
            if (!attractToCollector)
            {
                FinalizeCollection();
            }
        }

        /// <summary>
        /// Handles the attraction movement towards the collector and finalizes collection when close enough.
        /// </summary>
        private void AttractTowardsCollector()
        {
            if (_collectorTransform == null)
            {
                _isBeingCollected = false;
                return;
            }

            Vector3 direction = _collectorTransform.position - transform.position;
            float distance = direction.magnitude;

            if (distance <= snapDistance)
            {
                FinalizeCollection();
                return;
            }

            // Move towards collector with acceleration (gets faster as it gets closer)
            float speed = attractSpeed * (1f + (1f - Mathf.Clamp01(distance / 5f)));
            transform.position += direction.normalized * (speed * Time.deltaTime);
        }

        /// <summary>
        /// Adds the coin value to the CoinManager and destroys the coin GameObject.
        /// </summary>
        private void FinalizeCollection()
        {
            CoinManager.Instance?.AddCoins(value);
            Destroy(gameObject);
        }

        /// <summary>
        /// Handles the idle bobbing and rotation animation.
        /// </summary>
        private void BobAndRotate()
        {
            if (doBob)
            {
                Vector3 pos = _startPosition;
                pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = pos;
            }

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw coin value label
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"Coin (Value: {value})",
                UnityEditor.EditorStyles.miniLabel);
        }
#endif
    }
}

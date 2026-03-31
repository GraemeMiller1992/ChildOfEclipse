using System.Collections.Generic;
using UnityEngine;

namespace ChildOfEclipse.Coin
{
    /// <summary>
    /// Detects and collects nearby coins using Physics.OverlapSphere.
    /// Attach this to the player or any GameObject that should collect coins.
    /// Uses a configurable layer mask to filter coin detection.
    /// </summary>
    public class CoinCollector : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Detection Settings")]
        [Tooltip("Radius of the overlap sphere used to detect coins")]
        [SerializeField] private float detectionRadius = 3f;

        [Tooltip("Layer mask for coin detection. Set this to the layer coins are on.")]
        [SerializeField] private LayerMask coinLayer = 1 << 6; // Default to layer 6, adjust in inspector

        [Tooltip("Offset from the transform position for the detection center")]
        [SerializeField] private Vector3 detectionOffset = Vector3.zero;

        [Header("Collection Settings")]
        [Tooltip("How often to scan for coins (in seconds). 0 = every frame.")]
        [SerializeField] private float scanInterval = 0.1f;

        [Tooltip("Maximum number of coins detected per scan (for performance)")]
        [SerializeField] private int maxCoinsPerScan = 32;

        [Tooltip("Should the detection radius be drawn in the editor?")]
        [SerializeField] private bool showGizmos = true;

        [Header("Audio (Optional)")]
        [Tooltip("Sound played when a coin is collected")]
        [SerializeField] private AudioClip collectSound;

        [Tooltip("Volume of the collection sound")]
        [SerializeField] [Range(0f, 1f)] private float collectVolume = 0.7f;

        #endregion

        #region Private Fields

        private float _scanTimer;
        private readonly Collider[] _hitColliders = new Collider[64];
        private readonly HashSet<Coin> _coinsInProximity = new HashSet<Coin>();

        #endregion

        #region Properties

        /// <summary>
        /// The radius of the coin detection sphere.
        /// </summary>
        public float DetectionRadius => detectionRadius;

        /// <summary>
        /// The world-space center of the detection sphere.
        /// </summary>
        public Vector3 DetectionCenter => transform.position + detectionOffset;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            _scanTimer -= Time.deltaTime;

            if (_scanTimer <= 0f)
            {
                _scanTimer = scanInterval;
                ScanForCoins();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Scans for coins within the detection radius using Physics.OverlapSphere.
        /// Coins on the specified layer mask are detected and collected.
        /// </summary>
        private void ScanForCoins()
        {
            Vector3 center = DetectionCenter;
            int numColliders = Physics.OverlapSphereNonAlloc(
                center,
                detectionRadius,
                _hitColliders,
                coinLayer,
                QueryTriggerInteraction.Collide
            );

            // Track which coins are still in proximity
            _coinsInProximity.Clear();

            int coinsProcessed = 0;

            for (int i = 0; i < numColliders; i++)
            {
                if (coinsProcessed >= maxCoinsPerScan) break;

                Collider col = _hitColliders[i];
                if (col == null) continue;

                Coin coin = col.GetComponent<Coin>();
                if (coin == null)
                {
                    // Try getting it from the parent in case the collider is on a child
                    coin = col.GetComponentInParent<Coin>();
                }

                if (coin != null)
                {
                    _coinsInProximity.Add(coin);
                    CollectCoin(coin);
                    coinsProcessed++;
                }
            }
        }

        /// <summary>
        /// Initiates collection of a coin. Plays audio feedback.
        /// </summary>
        /// <param name="coin">The coin to collect</param>
        private void CollectCoin(Coin coin)
        {
            // Play collection sound
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, coin.transform.position, collectVolume);
            }

            // Tell the coin to begin its collection sequence (attraction + finalize)
            coin.Collect(transform);
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // Draw detection sphere
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f); // Gold, semi-transparent
            Gizmos.DrawWireSphere(DetectionCenter, detectionRadius);

            // Draw solid inner sphere for visual clarity
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.05f);
            Gizmos.DrawSphere(DetectionCenter, detectionRadius);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            scanInterval = Mathf.Max(0f, scanInterval);
            maxCoinsPerScan = Mathf.Max(1, maxCoinsPerScan);
        }
#endif

        #endregion
    }
}

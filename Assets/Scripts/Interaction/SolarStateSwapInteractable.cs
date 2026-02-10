using UnityEngine;
using World;

namespace ChildOfEclipse
{
    /// <summary>
    /// An interactable object that swaps the player's SolarState with this object's SolarState when clicked.
    /// The player must have a SolarState component for this to work.
    /// </summary>
    public class SolarStateSwapInteractable : MonoBehaviour, IInteractable
    {
        #region Serialized Fields

        [Header("Visual Feedback")]
        [Tooltip("Renderer to highlight when hovering. If null, will search for one on this GameObject.")]
        [SerializeField] private Renderer highlightRenderer;

        [Tooltip("Color to use when hovering over this interactable.")]
        [SerializeField] private Color hoverColor = Color.yellow;

        [Header("Interaction Description")]
        [Tooltip("Custom description for this interactable. If empty, generates one automatically.")]
        [SerializeField] private string customInteractionDescription = string.Empty;

        [Header("Optional Effects")]
        [Tooltip("Particle system to play when interacted with.")]
        [SerializeField] private ParticleSystem interactParticles;

        [Tooltip("Sound to play when interacted with.")]
        [SerializeField] private AudioClip interactSound;

        [Tooltip("Volume for the interact sound.")]
        [Range(0f, 1f)]
        [SerializeField] private float interactSoundVolume = 1f;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player GameObject.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Swap Limits")]
        [Tooltip("Maximum number of swaps allowed. Set to -1 for unlimited swaps.")]
        [SerializeField] private int maxSwaps = -1;

        [Header("Debug")]
        [Tooltip("Show debug messages in console.")]
        [SerializeField] private bool showDebugMessages = false;

        #endregion

        #region Private Fields

        private bool _canSwapState;
        private AudioSource _audioSource;
        private SolarState _playerSolarState;
        private SolarStateMaterial _solarStateMaterial;
        private int _currentSwaps;

        #endregion

        #region Properties

        /// <summary>
        /// Returns whether this object can swap state (player has different state).
        /// </summary>
        public bool CanInteract => _canSwapState;

        /// <summary>
        /// Returns the description of what will happen when interacted with.
        /// </summary>
        public string InteractionDescription
        {
            get
            {
                if (!string.IsNullOrEmpty(customInteractionDescription))
                {
                    return customInteractionDescription;
                }

                // Check if max swaps reached
                if (maxSwaps >= 0 && _currentSwaps >= maxSwaps)
                {
                    return "No swaps remaining";
                }

                if (!_canSwapState)
                {
                    if (_playerSolarState != null && GetComponent<SolarState>() != null)
                    {
                        return $"Already has {_playerSolarState.CurrentState} state";
                    }
                    return "Cannot swap state";
                }

                if (_playerSolarState != null && GetComponent<SolarState>() != null)
                {
                    var myState = GetComponent<SolarState>().CurrentState;
                    var playerState = _playerSolarState.CurrentState;
                    if (maxSwaps >= 0)
                    {
                        int remainingSwaps = maxSwaps - _currentSwaps;
                        return $"Swap {playerState} for {myState} ({remainingSwaps} remaining)";
                    }
                    return $"Swap {playerState} for {myState}";
                }
                return "Swap state";
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get or create audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && interactSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            // Find renderer if not assigned
            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponent<Renderer>();
            }

            // Get SolarStateMaterial component
            _solarStateMaterial = GetComponent<SolarStateMaterial>();

            // Find player's SolarState
            FindPlayerSolarState();

            // Subscribe to player's state changes to update canSwapState
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged += OnPlayerStateChanged;
            }

            // Subscribe to this object's state changes to update canSwapState
            var mySolarState = GetComponent<SolarState>();
            if (mySolarState != null)
            {
                mySolarState.OnSolarStateChanged += OnMyStateChanged;
            }

            // Update swapable state
            UpdateSwapableState();
        }

        private void Update()
        {
            // Find player if not found yet
            if (_playerSolarState == null)
            {
                FindPlayerSolarState();
                if (_playerSolarState != null)
                {
                    _playerSolarState.OnSolarStateChanged += OnPlayerStateChanged;
                }
            }

            // Update swapable state
            UpdateSwapableState();
        }

        private void OnDestroy()
        {
            // Unsubscribe from player's state changes
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged -= OnPlayerStateChanged;
            }

            // Unsubscribe from this object's state changes
            var mySolarState = GetComponent<SolarState>();
            if (mySolarState != null)
            {
                mySolarState.OnSolarStateChanged -= OnMyStateChanged;
            }
        }

        #endregion

        #region IInteractable Implementation

        /// <summary>
        /// Called when the object is clicked by the interact pointer.
        /// </summary>
        public void OnInteract(GameObject interactor, RaycastHit hitInfo)
        {
            if (!_canSwapState)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"{gameObject.name}: Cannot swap state - no player SolarState or same state", this);
                }
                return;
            }

            // Get the player's SolarState
            if (_playerSolarState == null)
            {
                Debug.LogError($"{gameObject.name}: Player SolarState not found!", this);
                return;
            }

            // Get this object's SolarState
            SolarState mySolarState = GetComponent<SolarState>();
            if (mySolarState == null)
            {
                Debug.LogError($"{gameObject.name}: SolarState component not found on this object!", this);
                return;
            }

            // Store the states before swapping
            SolarStateValue playerState = _playerSolarState.CurrentState;
            SolarStateValue myState = mySolarState.CurrentState;

            // Swap the states
            _playerSolarState.CurrentState = myState;
            mySolarState.CurrentState = playerState;

            // Increment swap count
            _currentSwaps++;

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Swapped states - Player now has {myState}, Object now has {playerState} ({_currentSwaps}/{(maxSwaps >= 0 ? maxSwaps.ToString() : "∞")} swaps)", this);
            }

            // Update swapable state after incrementing
            UpdateSwapableState();

            // Play effects
            PlayInteractEffects();
        }

        /// <summary>
        /// Called when the object is hovered over by the interact pointer.
        /// </summary>
        public void OnHoverEnter(GameObject interactor)
        {
            // Only apply hover color if we can swap
            if (_canSwapState && highlightRenderer != null)
            {
                highlightRenderer.material.color = hoverColor;
            }

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Hover enter", this);
            }
        }

        /// <summary>
        /// Called when the object is no longer being hovered over.
        /// </summary>
        public void OnHoverExit(GameObject interactor)
        {
            // Trigger SolarStateMaterial to re-apply the correct material for the current state
            if (_solarStateMaterial != null)
            {
                var mySolarState = GetComponent<SolarState>();
                if (mySolarState != null)
                {
                    // Use reflection to call the private ApplyMaterialForState method
                    var method = typeof(SolarStateMaterial).GetMethod("ApplyMaterialForState",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(_solarStateMaterial, new object[] { mySolarState.CurrentState });
                    }
                }
            }

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Hover exit", this);
            }
        }

        #endregion

        #region Private Methods

        private void FindPlayerSolarState()
        {
            // Try to find player by tag
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                _playerSolarState = player.GetComponent<SolarState>();
                if (_playerSolarState != null && showDebugMessages)
                {
                    Debug.Log($"{gameObject.name}: Found player SolarState on {player.name}", this);
                }
            }
        }

        private void UpdateSwapableState()
        {
            SolarState mySolarState = GetComponent<SolarState>();

            if (_playerSolarState == null || mySolarState == null)
            {
                _canSwapState = false;
                return;
            }

            // Check if we've reached max swaps
            if (maxSwaps >= 0 && _currentSwaps >= maxSwaps)
            {
                _canSwapState = false;
                return;
            }

            // Can swap if states are different
            _canSwapState = _playerSolarState.CurrentState != mySolarState.CurrentState;
        }

        private void OnPlayerStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Player state changed from {oldState} to {newState}", this);
            }
            UpdateSwapableState();
        }

        private void OnMyStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: My state changed from {oldState} to {newState}", this);
            }
            UpdateSwapableState();
        }

        private void PlayInteractEffects()
        {
            // Play particles
            if (interactParticles != null)
            {
                interactParticles.Play();
            }

            // Play sound
            if (_audioSource != null && interactSound != null)
            {
                _audioSource.volume = interactSoundVolume;
                _audioSource.PlayOneShot(interactSound);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually sets the player SolarState reference.
        /// </summary>
        public void SetPlayerSolarState(SolarState playerState)
        {
            // Unsubscribe from old player
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged -= OnPlayerStateChanged;
            }

            _playerSolarState = playerState;

            // Subscribe to new player
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged += OnPlayerStateChanged;
            }

            UpdateSwapableState();
        }

        /// <summary>
        /// Triggers the interaction manually (without raycast).
        /// </summary>
        public void TriggerInteraction()
        {
            if (!_canSwapState)
            {
                return;
            }

            // Create a fake hit info
            RaycastHit hitInfo = new RaycastHit();
            hitInfo.point = transform.position;
            hitInfo.normal = Vector3.up;

            OnInteract(gameObject, hitInfo);
        }

        /// <summary>
        /// Gets the player's current solar state.
        /// </summary>
        public SolarStateValue? GetPlayerState()
        {
            return _playerSolarState?.CurrentState;
        }

        /// <summary>
        /// Gets this object's current solar state.
        /// </summary>
        public SolarStateValue? GetMyState()
        {
            return GetComponent<SolarState>()?.CurrentState;
        }

        /// <summary>
        /// Gets the number of swaps remaining. Returns -1 if unlimited.
        /// </summary>
        public int GetRemainingSwaps()
        {
            if (maxSwaps < 0)
            {
                return -1; // Unlimited
            }
            return Mathf.Max(0, maxSwaps - _currentSwaps);
        }

        /// <summary>
        /// Gets the current number of swaps performed.
        /// </summary>
        public int GetCurrentSwaps()
        {
            return _currentSwaps;
        }

        /// <summary>
        /// Resets the swap count to zero.
        /// </summary>
        public void ResetSwapCount()
        {
            _currentSwaps = 0;
            UpdateSwapableState();

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Swap count reset", this);
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw a sphere around the interactable to show its interaction radius
            Gizmos.color = _canSwapState ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw label
            if (highlightRenderer != null)
            {
                var myState = GetMyState();
                var playerState = GetPlayerState();

                string stateText = myState.HasValue ? myState.ToString() : "No State";
                string playerText = playerState.HasValue ? playerState.ToString() : "No Player";
                string swapText = _canSwapState ? "Can Swap" : "Cannot Swap";
                string swapsText = maxSwaps >= 0 ? $"({_currentSwaps}/{maxSwaps})" : "(∞)";

                UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
                    $"{stateText} Interactable\nPlayer: {playerText}\n{swapText} {swapsText}");
            }
        }
#endif
#endregion
    }
}

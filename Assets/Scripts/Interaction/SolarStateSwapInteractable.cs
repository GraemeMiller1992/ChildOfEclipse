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

        [Header("Interaction Lock")]
        [Tooltip("If true, this object cannot be interacted with.")]
        [SerializeField] private bool interactionLocked = false;

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
        /// Returns whether this object can currently be interacted with.
        /// </summary>
        public bool CanInteract => !interactionLocked && _canSwapState;

        /// <summary>
        /// Returns whether interaction is currently locked.
        /// </summary>
        public bool IsInteractionLocked => interactionLocked;

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

                if (interactionLocked)
                {
                    return "Interaction disabled";
                }

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
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && interactSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponent<Renderer>();
            }

            _solarStateMaterial = GetComponent<SolarStateMaterial>();

            FindPlayerSolarState();

            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged += OnPlayerStateChanged;
            }

            var mySolarState = GetComponent<SolarState>();
            if (mySolarState != null)
            {
                mySolarState.OnSolarStateChanged += OnMyStateChanged;
            }

            UpdateSwapableState();
        }

        private void Update()
        {
            if (_playerSolarState == null)
            {
                FindPlayerSolarState();
                if (_playerSolarState != null)
                {
                    _playerSolarState.OnSolarStateChanged += OnPlayerStateChanged;
                }
            }

            UpdateSwapableState();
        }

        private void OnDestroy()
        {
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged -= OnPlayerStateChanged;
            }

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
            if (interactionLocked)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"{gameObject.name}: Interaction blocked because it is locked", this);
                }
                return;
            }

            if (!_canSwapState)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"{gameObject.name}: Cannot swap state - no player SolarState or same state", this);
                }
                return;
            }

            if (_playerSolarState == null)
            {
                Debug.LogError($"{gameObject.name}: Player SolarState not found!", this);
                return;
            }

            SolarState mySolarState = GetComponent<SolarState>();
            if (mySolarState == null)
            {
                Debug.LogError($"{gameObject.name}: SolarState component not found on this object!", this);
                return;
            }

            SolarStateValue playerState = _playerSolarState.CurrentState;
            SolarStateValue myState = mySolarState.CurrentState;

            _playerSolarState.CurrentState = myState;
            mySolarState.CurrentState = playerState;

            _currentSwaps++;

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Swapped states - Player now has {myState}, Object now has {playerState} ({_currentSwaps}/{(maxSwaps >= 0 ? maxSwaps.ToString() : "∞")} swaps)", this);
            }

            UpdateSwapableState();
            PlayInteractEffects();
        }

        /// <summary>
        /// Called when the object is hovered over by the interact pointer.
        /// </summary>
        public void OnHoverEnter(GameObject interactor)
        {
            if (interactionLocked)
            {
                return;
            }

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
            if (_solarStateMaterial != null)
            {
                var mySolarState = GetComponent<SolarState>();
                if (mySolarState != null)
                {
                    var method = typeof(SolarStateMaterial).GetMethod(
                        "ApplyMaterialForState",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                    );

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

            if (maxSwaps >= 0 && _currentSwaps >= maxSwaps)
            {
                _canSwapState = false;
                return;
            }

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
            if (interactParticles != null)
            {
                interactParticles.Play();
            }

            if (_audioSource != null && interactSound != null)
            {
                _audioSource.volume = interactSoundVolume;
                _audioSource.PlayOneShot(interactSound);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Disable interaction on this object.
        /// </summary>
        public void DisableInteraction()
        {
            interactionLocked = true;

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Interaction disabled", this);
            }
        }

        /// <summary>
        /// Enable interaction on this object.
        /// </summary>
        public void EnableInteraction()
        {
            interactionLocked = false;

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Interaction enabled", this);
            }
        }

        /// <summary>
        /// Set whether interaction is locked.
        /// </summary>
        public void SetInteractionLocked(bool locked)
        {
            interactionLocked = locked;

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Interaction locked set to {locked}", this);
            }
        }

        /// <summary>
        /// Manually sets the player SolarState reference.
        /// </summary>
        public void SetPlayerSolarState(SolarState playerState)
        {
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged -= OnPlayerStateChanged;
            }

            _playerSolarState = playerState;

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
            if (interactionLocked || !_canSwapState)
            {
                return;
            }

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
                return -1;
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
            Gizmos.color = CanInteract ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            if (highlightRenderer != null)
            {
                var myState = GetMyState();
                var playerState = GetPlayerState();

                string stateText = myState.HasValue ? myState.ToString() : "No State";
                string playerText = playerState.HasValue ? playerState.ToString() : "No Player";
                string swapText = interactionLocked ? "Locked" : (CanInteract ? "Can Swap" : "Cannot Swap");
                string swapsText = maxSwaps >= 0 ? $"({_currentSwaps}/{maxSwaps})" : "(∞)";

                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 1f,
                    $"{stateText} Interactable\nPlayer: {playerText}\n{swapText} {swapsText}"
                );
            }
        }
#endif

        #endregion
    }
}

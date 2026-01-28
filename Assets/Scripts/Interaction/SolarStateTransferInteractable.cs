using UnityEngine;
using World;

namespace ChildOfEclipse
{
    /// <summary>
    /// An interactable object that transfers the player's SolarState to this object's SolarState when clicked.
    /// The player must have a SolarState component for this to work.
    /// </summary>
    public class SolarStateTransferInteractable : MonoBehaviour, IInteractable
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

        [Header("Debug")]
        [Tooltip("Show debug messages in console.")]
        [SerializeField] private bool showDebugMessages = false;

        #endregion

        #region Private Fields

        private bool _canTransferState;
        private AudioSource _audioSource;
        private SolarState _playerSolarState;

        #endregion

        #region Properties

        /// <summary>
        /// Returns whether this object can transfer state (player has different state).
        /// </summary>
        public bool CanInteract => _canTransferState;

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

                if (!_canTransferState)
                {
                    if (_playerSolarState != null && GetComponent<SolarState>() != null)
                    {
                        return $"Already has {_playerSolarState.CurrentState} state";
                    }
                    return "Cannot transfer state";
                }

                if (_playerSolarState != null)
                {
                    return $"Transfer {_playerSolarState.CurrentState} state";
                }
                return "Transfer state";
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

            // Find player's SolarState
            FindPlayerSolarState();

            // Subscribe to player's state changes to update canTransferState
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged += OnPlayerStateChanged;
            }

            // Update transferable state
            UpdateTransferableState();
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

            // Update transferable state
            UpdateTransferableState();
        }

        private void OnDestroy()
        {
            // Unsubscribe from player's state changes
            if (_playerSolarState != null)
            {
                _playerSolarState.OnSolarStateChanged -= OnPlayerStateChanged;
            }
        }

        #endregion

        #region IInteractable Implementation

        /// <summary>
        /// Called when the object is clicked by the interact pointer.
        /// </summary>
        public void OnInteract(GameObject interactor, RaycastHit hitInfo)
        {
            if (!_canTransferState)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"{gameObject.name}: Cannot transfer state - no player SolarState or same state", this);
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

            // Transfer the player's state to this object
            SolarStateValue playerState = _playerSolarState.CurrentState;
            mySolarState.CurrentState = playerState;

            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Transferred {playerState} state from player", this);
            }

            // Play effects
            PlayInteractEffects();
        }

        /// <summary>
        /// Called when the object is hovered over by the interact pointer.
        /// </summary>
        public void OnHoverEnter(GameObject interactor)
        {
            // Apply hover color
            if (highlightRenderer != null)
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
            // Let SolarStateMaterial handle the color - don't restore anything
            // The material will be updated by SolarStateMaterial based on the current state

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

        private void UpdateTransferableState()
        {
            SolarState mySolarState = GetComponent<SolarState>();

            if (_playerSolarState == null || mySolarState == null)
            {
                _canTransferState = false;
                return;
            }

            // Can transfer if states are different
            _canTransferState = _playerSolarState.CurrentState != mySolarState.CurrentState;
        }

        private void OnPlayerStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Player state changed from {oldState} to {newState}", this);
            }
            UpdateTransferableState();
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

            UpdateTransferableState();
        }

        /// <summary>
        /// Triggers the interaction manually (without raycast).
        /// </summary>
        public void TriggerInteraction()
        {
            if (!_canTransferState)
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

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw a sphere around the interactable to show its interaction radius
            Gizmos.color = _canTransferState ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw label
            if (highlightRenderer != null)
            {
                var myState = GetMyState();
                var playerState = GetPlayerState();

                string stateText = myState.HasValue ? myState.ToString() : "No State";
                string playerText = playerState.HasValue ? playerState.ToString() : "No Player";
                string transferText = _canTransferState ? "Can Transfer" : "Same State";

                UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
                    $"{stateText} Interactable\nPlayer: {playerText}\n{transferText}");
            }
        }

        #endregion
    }
}

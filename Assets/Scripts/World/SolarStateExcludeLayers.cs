using UnityEngine;

namespace World
{
    /// <summary>
    /// Component that changes the excludeLayers of colliders based on the solar state.
    /// Requires a SolarState component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(SolarState))]
    public class SolarStateExcludeLayers : MonoBehaviour
    {
        [Header("Exclude Layer Masks")]
        [SerializeField]
        [Tooltip("Layer mask to exclude when the solar state is Sun.")]
        private LayerMask _sunExcludeLayers;

        [SerializeField]
        [Tooltip("Layer mask to exclude when the solar state is Moon.")]
        private LayerMask _moonExcludeLayers;

        [SerializeField]
        [Tooltip("Layer mask to exclude when the solar state is Eclipse.")]
        private LayerMask _eclipseExcludeLayers;

        [Header("Collider Settings")]
        [SerializeField]
        [Tooltip("The colliders whose excludeLayers will be changed. If empty, uses all Colliders on this GameObject.")]
        private Collider[] _targetColliders;

        private SolarState _solarState;

        private void Awake()
        {
            _solarState = GetComponent<SolarState>();

            // Get colliders if not explicitly assigned
            if (_targetColliders == null || _targetColliders.Length == 0)
            {
                _targetColliders = GetComponents<Collider>();
            }

            if (_targetColliders == null || _targetColliders.Length == 0)
            {
                Debug.LogError($"SolarStateExcludeLayers: No Colliders found on {gameObject.name}. Component will not function.", this);
                return;
            }

            // Subscribe to state changes
            _solarState.OnSolarStateChanged += HandleSolarStateChanged;
        }

        private void Start()
        {
            // Apply initial excludeLayers based on current state
            if (_targetColliders != null && _targetColliders.Length > 0)
            {
                ApplyExcludeLayersForState(_solarState.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (_solarState != null)
            {
                _solarState.OnSolarStateChanged -= HandleSolarStateChanged;
            }
        }

        /// <summary>
        /// Handles the solar state changed event and updates the excludeLayers accordingly.
        /// </summary>
        private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            ApplyExcludeLayersForState(newState);
        }

        /// <summary>
        /// Applies the appropriate excludeLayers for the given solar state to all target colliders.
        /// </summary>
        private void ApplyExcludeLayersForState(SolarStateValue state)
        {
            if (_targetColliders == null || _targetColliders.Length == 0)
            {
                return;
            }

            LayerMask excludeLayersToApply = state switch
            {
                SolarStateValue.Sun => _sunExcludeLayers,
                SolarStateValue.Moon => _moonExcludeLayers,
                SolarStateValue.Eclipse => _eclipseExcludeLayers,
                _ => 0
            };

            // Apply excludeLayers to all target colliders
            foreach (var collider in _targetColliders)
            {
                if (collider != null)
                {
                    collider.excludeLayers = excludeLayersToApply;
                }
            }
        }

        /// <summary>
        /// Gets the excludeLayers currently associated with the specified solar state.
        /// </summary>
        public LayerMask GetExcludeLayersForState(SolarStateValue state)
        {
            return state switch
            {
                SolarStateValue.Sun => _sunExcludeLayers,
                SolarStateValue.Moon => _moonExcludeLayers,
                SolarStateValue.Eclipse => _eclipseExcludeLayers,
                _ => 0
            };
        }

        /// <summary>
        /// Sets the excludeLayers for a specific solar state.
        /// </summary>
        public void SetExcludeLayersForState(SolarStateValue state, LayerMask excludeLayers)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    _sunExcludeLayers = excludeLayers;
                    break;
                case SolarStateValue.Moon:
                    _moonExcludeLayers = excludeLayers;
                    break;
                case SolarStateValue.Eclipse:
                    _eclipseExcludeLayers = excludeLayers;
                    break;
            }

            // If this is the current state, apply the new excludeLayers immediately
            if (_solarState != null && _solarState.CurrentState == state)
            {
                ApplyExcludeLayersForState(state);
            }
        }
    }
}

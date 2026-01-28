using UnityEngine;

namespace World
{
    /// <summary>
    /// Component that changes the material of a renderer based on the solar state.
    /// Requires a SolarState component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(SolarState))]
    public class SolarStateMaterial : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField]
        [Tooltip("Material to use when the solar state is Sun.")]
        private Material _sunMaterial;

        [SerializeField]
        [Tooltip("Material to use when the solar state is Moon.")]
        private Material _moonMaterial;

        [SerializeField]
        [Tooltip("Material to use when the solar state is Eclipse.")]
        private Material _eclipseMaterial;

        [Header("Renderer Settings")]
        [SerializeField]
        [Tooltip("The renderers whose materials will be changed. If empty, uses all Renderers on this GameObject.")]
        private Renderer[] _targetRenderers;

        private SolarState _solarState;

        private void Awake()
        {
            _solarState = GetComponent<SolarState>();
            
            // Get renderers if not explicitly assigned
            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                _targetRenderers = GetComponents<Renderer>();
            }

            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                Debug.LogError($"SolarStateMaterial: No Renderers found on {gameObject.name}. Component will not function.", this);
                return;
            }

            // Subscribe to state changes
            _solarState.OnSolarStateChanged += HandleSolarStateChanged;
        }

        private void Start()
        {
            // Apply initial material based on current state
            if (_targetRenderers != null && _targetRenderers.Length > 0)
            {
                ApplyMaterialForState(_solarState.CurrentState);
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
        /// Handles the solar state changed event and updates the material accordingly.
        /// </summary>
        private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            ApplyMaterialForState(newState);
        }

        /// <summary>
        /// Applies the appropriate material for the given solar state to all target renderers.
        /// </summary>
        private void ApplyMaterialForState(SolarStateValue state)
        {
            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                return;
            }

            Material materialToApply = state switch
            {
                SolarStateValue.Sun => _sunMaterial,
                SolarStateValue.Moon => _moonMaterial,
                SolarStateValue.Eclipse => _eclipseMaterial,
                _ => null
            };

            if (materialToApply == null)
            {
                Debug.LogWarning($"SolarStateMaterial: No material assigned for state {state} on {gameObject.name}.", this);
                return;
            }

            // Apply material to all target renderers
            foreach (var renderer in _targetRenderers)
            {
                if (renderer != null)
                {
                    renderer.material = materialToApply;
                }
            }
        }

        /// <summary>
        /// Gets the material currently associated with the specified solar state.
        /// </summary>
        public Material GetMaterialForState(SolarStateValue state)
        {
            return state switch
            {
                SolarStateValue.Sun => _sunMaterial,
                SolarStateValue.Moon => _moonMaterial,
                SolarStateValue.Eclipse => _eclipseMaterial,
                _ => null
            };
        }

        /// <summary>
        /// Sets the material for a specific solar state.
        /// </summary>
        public void SetMaterialForState(SolarStateValue state, Material material)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    _sunMaterial = material;
                    break;
                case SolarStateValue.Moon:
                    _moonMaterial = material;
                    break;
                case SolarStateValue.Eclipse:
                    _eclipseMaterial = material;
                    break;
            }

            // If this is the current state, apply the new material immediately
            if (_solarState != null && _solarState.CurrentState == state)
            {
                ApplyMaterialForState(state);
            }
        }
    }
}

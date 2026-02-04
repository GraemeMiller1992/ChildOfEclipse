using UnityEngine;

namespace World
{
    /// <summary>
    /// Component that changes the layer of a GameObject based on the solar state.
    /// Requires a SolarState component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(SolarState))]
    public class SolarStateLayer : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField]
        [Tooltip("Layer to use when the solar state is Sun.")]
        private int _sunLayer;

        [SerializeField]
        [Tooltip("Layer to use when the solar state is Moon.")]
        private int _moonLayer;

        [SerializeField]
        [Tooltip("Layer to use when the solar state is Eclipse.")]
        private int _eclipseLayer;

        [Header("Target Settings")]
        [SerializeField]
        [Tooltip("The GameObjects whose layers will be changed. If empty, uses this GameObject only.")]
        private GameObject[] _targetGameObjects;

        [SerializeField]
        [Tooltip("If true, also applies the layer change to all children of target GameObjects.")]
        private bool _includeChildren = false;

        private SolarState _solarState;

        private void Awake()
        {
            _solarState = GetComponent<SolarState>();

            // Get target GameObjects if not explicitly assigned
            if (_targetGameObjects == null || _targetGameObjects.Length == 0)
            {
                _targetGameObjects = new GameObject[] { gameObject };
            }

            if (_targetGameObjects == null || _targetGameObjects.Length == 0)
            {
                Debug.LogError($"SolarStateLayer: No target GameObjects found on {gameObject.name}. Component will not function.", this);
                return;
            }

            // Subscribe to state changes
            _solarState.OnSolarStateChanged += HandleSolarStateChanged;
        }

        private void Start()
        {
            // Apply initial layer based on current state
            if (_targetGameObjects != null && _targetGameObjects.Length > 0)
            {
                ApplyLayerForState(_solarState.CurrentState);
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
        /// Handles the solar state changed event and updates the layer accordingly.
        /// </summary>
        private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            ApplyLayerForState(newState);
        }

        /// <summary>
        /// Applies the appropriate layer for the given solar state to all target GameObjects.
        /// </summary>
        private void ApplyLayerForState(SolarStateValue state)
        {
            if (_targetGameObjects == null || _targetGameObjects.Length == 0)
            {
                return;
            }

            int layerToApply = state switch
            {
                SolarStateValue.Sun => _sunLayer,
                SolarStateValue.Moon => _moonLayer,
                SolarStateValue.Eclipse => _eclipseLayer,
                _ => gameObject.layer
            };

            // Apply layer to all target GameObjects
            foreach (var target in _targetGameObjects)
            {
                if (target != null)
                {
                    target.layer = layerToApply;

                    // Apply to children if enabled
                    if (_includeChildren)
                    {
                        Transform[] children = target.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child != null)
                            {
                                child.gameObject.layer = layerToApply;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the layer currently associated with the specified solar state.
        /// </summary>
        public int GetLayerForState(SolarStateValue state)
        {
            return state switch
            {
                SolarStateValue.Sun => _sunLayer,
                SolarStateValue.Moon => _moonLayer,
                SolarStateValue.Eclipse => _eclipseLayer,
                _ => gameObject.layer
            };
        }

        /// <summary>
        /// Sets the layer for a specific solar state.
        /// </summary>
        public void SetLayerForState(SolarStateValue state, int layer)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    _sunLayer = layer;
                    break;
                case SolarStateValue.Moon:
                    _moonLayer = layer;
                    break;
                case SolarStateValue.Eclipse:
                    _eclipseLayer = layer;
                    break;
            }

            // If this is the current state, apply the new layer immediately
            if (_solarState != null && _solarState.CurrentState == state)
            {
                ApplyLayerForState(state);
            }
        }
    }
}

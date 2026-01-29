using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// Component that controls a NavMeshAgent's isStopped property based on the solar state.
    /// Requires a SolarState component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(SolarState))]
    public class SolarStateNavAgentStopper : MonoBehaviour
    {
        [Header("NavMeshAgent Settings")]
        [SerializeField]
        [Tooltip("The NavMeshAgent whose isStopped property will be controlled. If null, uses the first NavMeshAgent on this GameObject.")]
        private NavMeshAgent _navAgent;

        [Header("Sun State Settings")]
        [SerializeField]
        [Tooltip("Whether the NavMeshAgent should be stopped when the solar state is Sun.")]
        private bool _stopOnSun = false;

        [Header("Moon State Settings")]
        [SerializeField]
        [Tooltip("Whether the NavMeshAgent should be stopped when the solar state is Moon.")]
        private bool _stopOnMoon = false;

        [Header("Eclipse State Settings")]
        [SerializeField]
        [Tooltip("Whether the NavMeshAgent should be stopped when the solar state is Eclipse.")]
        private bool _stopOnEclipse = true;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("If true, applies the initial state in Start(). If false, waits for the first state change.")]
        private bool _applyInitialState = true;

        private SolarState _solarState;

        private void Awake()
        {
            _solarState = GetComponent<SolarState>();
            
            // Get NavMeshAgent if not explicitly assigned
            if (_navAgent == null)
            {
                _navAgent = GetComponent<NavMeshAgent>();
            }

            if (_navAgent == null)
            {
                Debug.LogError($"SolarStateNavAgentStopper: No NavMeshAgent found on {gameObject.name}. Component will not function.", this);
                return;
            }

            // Subscribe to state changes
            _solarState.OnSolarStateChanged += HandleSolarStateChanged;
        }

        private void Start()
        {
            // Apply initial state if enabled
            if (_applyInitialState && _navAgent != null)
            {
                ApplyNavAgentStateForState(_solarState.CurrentState);
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
        /// Handles the solar state changed event and updates the NavMeshAgent's isStopped property accordingly.
        /// </summary>
        private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            ApplyNavAgentStateForState(newState);
        }

        /// <summary>
        /// Applies the appropriate isStopped state for the given solar state.
        /// </summary>
        private void ApplyNavAgentStateForState(SolarStateValue state)
        {
            if (_navAgent == null)
            {
                return;
            }

            bool shouldStop = state switch
            {
                SolarStateValue.Sun => _stopOnSun,
                SolarStateValue.Moon => _stopOnMoon,
                SolarStateValue.Eclipse => _stopOnEclipse,
                _ => false
            };

            _navAgent.isStopped = shouldStop;
        }

        /// <summary>
        /// Gets whether the NavMeshAgent should be stopped for the specified solar state.
        /// </summary>
        public bool GetStopSettingForState(SolarStateValue state)
        {
            return state switch
            {
                SolarStateValue.Sun => _stopOnSun,
                SolarStateValue.Moon => _stopOnMoon,
                SolarStateValue.Eclipse => _stopOnEclipse,
                _ => false
            };
        }

        /// <summary>
        /// Sets whether the NavMeshAgent should be stopped for a specific solar state.
        /// </summary>
        public void SetStopSettingForState(SolarStateValue state, bool shouldStop)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    _stopOnSun = shouldStop;
                    break;
                case SolarStateValue.Moon:
                    _stopOnMoon = shouldStop;
                    break;
                case SolarStateValue.Eclipse:
                    _stopOnEclipse = shouldStop;
                    break;
            }

            // If this is the current state, apply the new setting immediately
            if (_solarState != null && _solarState.CurrentState == state && _navAgent != null)
            {
                _navAgent.isStopped = shouldStop;
            }
        }
    }
}

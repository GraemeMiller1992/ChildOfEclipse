using UnityEngine;

namespace World
{
    /// <summary>
    /// Component that enables and disables GameObjects based on the solar state.
    /// Requires a SolarState component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(SolarState))]
    public class SolarStateGameObjectSwitcher : MonoBehaviour
    {
        [Header("Sun State Objects")]
        [SerializeField]
        [Tooltip("GameObject(s) to enable when the solar state is Sun. All other state objects will be disabled.")]
        private GameObject[] _sunStateObjects;

        [Header("Moon State Objects")]
        [SerializeField]
        [Tooltip("GameObject(s) to enable when the solar state is Moon. All other state objects will be disabled.")]
        private GameObject[] _moonStateObjects;

        [Header("Eclipse State Objects")]
        [SerializeField]
        [Tooltip("GameObject(s) to enable when the solar state is Eclipse. All other state objects will be disabled.")]
        private GameObject[] _eclipseStateObjects;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("If true, disables all objects when no matching state is found. If false, leaves objects unchanged.")]
        private bool _disableOthers = true;

        [SerializeField]
        [Tooltip("If true, applies the initial state in Start(). If false, waits for the first state change.")]
        private bool _applyInitialState = true;

        private SolarState _solarState;

        private void Awake()
        {
            _solarState = GetComponent<SolarState>();

            // Subscribe to state changes
            _solarState.OnSolarStateChanged += HandleSolarStateChanged;
        }

        private void Start()
        {
            // Apply initial state if enabled
            if (_applyInitialState)
            {
                ApplyGameObjectStateForState(_solarState.CurrentState);
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
        /// Handles the solar state changed event and updates GameObject active states accordingly.
        /// </summary>
        private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
        {
            ApplyGameObjectStateForState(newState);
        }

        /// <summary>
        /// Applies the appropriate GameObject active states for the given solar state.
        /// </summary>
        private void ApplyGameObjectStateForState(SolarStateValue state)
        {
            // Disable all state objects first
            DisableAllStateObjects();

            // Enable objects for the current state
            GameObject[] objectsToEnable = state switch
            {
                SolarStateValue.Sun => _sunStateObjects,
                SolarStateValue.Moon => _moonStateObjects,
                SolarStateValue.Eclipse => _eclipseStateObjects,
                _ => null
            };

            if (objectsToEnable != null)
            {
                foreach (var obj in objectsToEnable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Disables all GameObjects in all state arrays.
        /// </summary>
        private void DisableAllStateObjects()
        {
            if (_sunStateObjects != null)
            {
                foreach (var obj in _sunStateObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }

            if (_moonStateObjects != null)
            {
                foreach (var obj in _moonStateObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }

            if (_eclipseStateObjects != null)
            {
                foreach (var obj in _eclipseStateObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the GameObjects currently associated with the specified solar state.
        /// </summary>
        public GameObject[] GetGameObjectsForState(SolarStateValue state)
        {
            return state switch
            {
                SolarStateValue.Sun => _sunStateObjects,
                SolarStateValue.Moon => _moonStateObjects,
                SolarStateValue.Eclipse => _eclipseStateObjects,
                _ => null
            };
        }

        /// <summary>
        /// Sets the GameObjects for a specific solar state.
        /// </summary>
        public void SetGameObjectsForState(SolarStateValue state, GameObject[] gameObjects)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    _sunStateObjects = gameObjects;
                    break;
                case SolarStateValue.Moon:
                    _moonStateObjects = gameObjects;
                    break;
                case SolarStateValue.Eclipse:
                    _eclipseStateObjects = gameObjects;
                    break;
            }

            // If this is the current state, apply the new GameObject state immediately
            if (_solarState != null && _solarState.CurrentState == state)
            {
                ApplyGameObjectStateForState(state);
            }
        }

        /// <summary>
        /// Adds a GameObject to a specific solar state.
        /// </summary>
        public void AddGameObjectToState(SolarStateValue state, GameObject gameObject)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    AddToArray(ref _sunStateObjects, gameObject);
                    break;
                case SolarStateValue.Moon:
                    AddToArray(ref _moonStateObjects, gameObject);
                    break;
                case SolarStateValue.Eclipse:
                    AddToArray(ref _eclipseStateObjects, gameObject);
                    break;
            }

            // If this is the current state, apply the new GameObject state immediately
            if (_solarState != null && _solarState.CurrentState == state)
            {
                ApplyGameObjectStateForState(state);
            }
        }

        /// <summary>
        /// Removes a GameObject from a specific solar state.
        /// </summary>
        public void RemoveGameObjectFromState(SolarStateValue state, GameObject gameObject)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    RemoveFromArray(ref _sunStateObjects, gameObject);
                    break;
                case SolarStateValue.Moon:
                    RemoveFromArray(ref _moonStateObjects, gameObject);
                    break;
                case SolarStateValue.Eclipse:
                    RemoveFromArray(ref _eclipseStateObjects, gameObject);
                    break;
            }
        }

        /// <summary>
        /// Helper method to add a GameObject to an array.
        /// </summary>
        private void AddToArray(ref GameObject[] array, GameObject gameObject)
        {
            if (gameObject == null) return;

            if (array == null)
            {
                array = new GameObject[] { gameObject };
                return;
            }

            // Check if already in array
            foreach (var obj in array)
            {
                if (obj == gameObject) return;
            }

            // Add to array
            GameObject[] newArray = new GameObject[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[array.Length] = gameObject;
            array = newArray;
        }

        /// <summary>
        /// Helper method to remove a GameObject from an array.
        /// </summary>
        private void RemoveFromArray(ref GameObject[] array, GameObject gameObject)
        {
            if (array == null || gameObject == null) return;

            int index = -1;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == gameObject)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1) return;

            // Remove from array
            GameObject[] newArray = new GameObject[array.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (i != index)
                {
                    newArray[newIndex++] = array[i];
                }
            }
            array = newArray;
        }
    }
}

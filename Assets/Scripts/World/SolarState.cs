using UnityEngine;
using System;

namespace World
{
    /// <summary>
    /// Represents the different solar states for individual objects.
    /// </summary>
    public enum SolarStateValue
    {
        Sun,
        Moon,
        Eclipse
    }

    /// <summary>
    /// Component for managing the solar state of individual game objects.
    /// Each object can have its own independent solar state.
    /// </summary>
    public class SolarState : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The initial solar state for this object.")]
        private SolarStateValue _currentState = SolarStateValue.Sun;

        /// <summary>
        /// Event fired when the solar state changes.
        /// Parameters: oldState, newState
        /// </summary>
        public event Action<SolarStateValue, SolarStateValue> OnSolarStateChanged;

        /// <summary>
        /// Gets or sets the current solar state.
        /// Setting this value will trigger the OnSolarStateChanged event.
        /// </summary>
        public SolarStateValue CurrentState
        {
            get => _currentState;
            set => ChangeState(value);
        }

        private void Awake()
        {
            Debug.Log($"SolarState: {gameObject.name} initialized with state {_currentState}");
        }

        /// <summary>
        /// Changes the solar state to Sun.
        /// </summary>
        public void SetSunState()
        {
            ChangeState(SolarStateValue.Sun);
        }

        /// <summary>
        /// Changes the solar state to Moon.
        /// </summary>
        public void SetMoonState()
        {
            ChangeState(SolarStateValue.Moon);
        }

        /// <summary>
        /// Changes the solar state to Eclipse.
        /// </summary>
        public void SetEclipseState()
        {
            ChangeState(SolarStateValue.Eclipse);
        }

        /// <summary>
        /// Internal method to handle state changes and fire events.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        private void ChangeState(SolarStateValue newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            SolarStateValue oldState = _currentState;
            _currentState = newState;

            Debug.Log($"SolarState: {gameObject.name} changed from {oldState} to {newState}");

            // Fire the event to notify all subscribers
            OnSolarStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Checks if the current solar state is Sun.
        /// </summary>
        public bool IsSunState() => _currentState == SolarStateValue.Sun;

        /// <summary>
        /// Checks if the current solar state is Moon.
        /// </summary>
        public bool IsMoonState() => _currentState == SolarStateValue.Moon;

        /// <summary>
        /// Checks if the current solar state is Eclipse.
        /// </summary>
        public bool IsEclipseState() => _currentState == SolarStateValue.Eclipse;
    }
}

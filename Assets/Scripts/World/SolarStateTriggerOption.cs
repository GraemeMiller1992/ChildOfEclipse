using UnityEngine;

namespace World
{
    /// <summary>
    /// A trigger option that only activates when the triggering object's solar state matches a required value.
    /// This component should be placed on the same GameObject as the GenericTrigger.
    /// </summary>
    public class SolarStateTriggerOption : MonoBehaviour, ITriggerOption
    {
        [Header("Solar State Settings")]
        [Tooltip("The required solar state for the trigger to activate.")]
        [SerializeField] private SolarStateValue requiredState = SolarStateValue.Sun;

        /// <summary>
        /// Evaluates whether the trigger should activate based on the triggering object's solar state.
        /// </summary>
        /// <param name="triggeringObject">The object that entered the trigger.</param>
        /// <param name="trigger">The GenericTrigger component being evaluated.</param>
        /// <returns>True if the triggering object's solar state matches the required state, false otherwise.</returns>
        public bool ShouldActivate(GameObject triggeringObject, GenericTrigger trigger)
        {
            // Get the SolarState component from the triggering object
            SolarState stateToCheck = triggeringObject.GetComponent<SolarState>();

            // If no component found, cannot activate
            if (stateToCheck == null)
            {
                return false;
            }

            // Check if the current state matches the required state
            return stateToCheck.CurrentState == requiredState;
        }

        /// <summary>
        /// Called when the trigger activates (all options passed).
        /// </summary>
        /// <param name="triggeringObject">The object that triggered the activation.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        public void OnActivated(GameObject triggeringObject, GenericTrigger trigger)
        {
            Debug.Log($"SolarStateTriggerOption: Trigger {trigger.gameObject.name} activated by {triggeringObject.name} with solar state {requiredState}", this);
        }
    }
}

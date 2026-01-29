using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// A trigger option that only activates when the triggering object's NavMeshAgent is stopped.
    /// This component should be placed on the same GameObject as the GenericTrigger.
    /// </summary>
    public class NavAgentStoppedTriggerOption : MonoBehaviour, ITriggerOption
    {
        [Header("Nav Agent Settings")]
        [Tooltip("Whether the agent must be stopped for the trigger to activate.")]
        [SerializeField] private bool requireStopped = true;

        /// <summary>
        /// Evaluates whether the trigger should activate based on the triggering object's NavMeshAgent stopped state.
        /// </summary>
        /// <param name="triggeringObject">The object that entered the trigger.</param>
        /// <param name="trigger">The GenericTrigger component being evaluated.</param>
        /// <returns>True if the triggering object's NavMeshAgent stopped state matches the requirement, false otherwise.</returns>
        public bool ShouldActivate(GameObject triggeringObject, GenericTrigger trigger)
        {
            // Get the NavMeshAgent component from the triggering object
            NavMeshAgent navAgent = triggeringObject.GetComponent<NavMeshAgent>();

            // If no component found, cannot activate
            if (navAgent == null)
            {
                return false;
            }

            // Check if the agent's stopped state matches the requirement
            return navAgent.isStopped == requireStopped;
        }

        /// <summary>
        /// Called when the trigger activates (all options passed).
        /// </summary>
        /// <param name="triggeringObject">The object that triggered the activation.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        public void OnActivated(GameObject triggeringObject, GenericTrigger trigger)
        {
            Debug.Log($"NavAgentStoppedTriggerOption: Trigger {trigger.gameObject.name} activated by {triggeringObject.name} with agent stopped state {requireStopped}", this);
        }
    }
}

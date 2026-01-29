using UnityEngine;

namespace World
{
    /// <summary>
    /// Interface for trigger reactions that respond when a trigger activates.
    /// Unlike ITriggerOption, reactions do not determine if activation should occur,
    /// they only respond when activation happens.
    /// </summary>
    public interface ITriggerReaction
    {
        /// <summary>
        /// Called when the trigger activates (all options passed).
        /// </summary>
        /// <param name="triggeringObject">The object that triggered the activation.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        void OnActivated(GameObject triggeringObject, GenericTrigger trigger);

        /// <summary>
        /// Called when the triggering object leaves the trigger zone and the trigger
        /// is configured to reset state on exit.
        /// </summary>
        /// <param name="triggeringObject">The object that left the trigger zone.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        void OnReset(GameObject triggeringObject, GenericTrigger trigger);
    }
}

using UnityEngine;

namespace World
{
    /// <summary>
    /// Interface for trigger options that can be evaluated to determine
    /// if a trigger should activate when an object enters it.
    /// </summary>
    public interface ITriggerOption
    {
        /// <summary>
        /// Evaluates whether the trigger should activate based on this option.
        /// </summary>
        /// <param name="triggeringObject">The object that entered the trigger.</param>
        /// <param name="trigger">The GenericTrigger component being evaluated.</param>
        /// <returns>True if the trigger should activate, false otherwise.</returns>
        bool ShouldActivate(GameObject triggeringObject, GenericTrigger trigger);

        /// <summary>
        /// Called when the trigger activates (all options passed).
        /// </summary>
        /// <param name="triggeringObject">The object that triggered the activation.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        void OnActivated(GameObject triggeringObject, GenericTrigger trigger);
    }
}

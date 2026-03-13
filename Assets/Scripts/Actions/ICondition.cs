using UnityEngine;

namespace Actions
{
    /// <summary>
    /// Interface for all conditions that can be evaluated by the ConditionalTriggerActions component.
    /// Implement this interface to create custom conditions.
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// Evaluates whether the condition is currently met.
        /// </summary>
        /// <returns>True if the condition is met, false otherwise</returns>
        bool IsMet();

        /// <summary>
        /// Gets a description of the condition for debugging purposes.
        /// </summary>
        /// <returns>A string describing the condition</returns>
        string GetDescription();
    }
}

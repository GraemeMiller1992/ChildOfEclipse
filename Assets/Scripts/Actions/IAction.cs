using UnityEngine;

namespace Actions
{
    /// <summary>
    /// Interface for all actions that can be executed by the ActionRunner.
    /// Implement this interface to create custom actions.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Execute the action.
        /// </summary>
        /// <param name="context">Optional context object for passing data between actions</param>
        void Execute(object context = null);
    }
}

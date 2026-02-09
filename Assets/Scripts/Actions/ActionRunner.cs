using System;
using System.Collections.Generic;
using UnityEngine;

namespace Actions
{
    /// <summary>
    /// A generic action runner that can execute a list of actions.
    /// This is a plain class (not MonoBehaviour) and can be used in other scripts.
    /// Uses SerializeReference to allow different action types in the same list.
    /// </summary>
    [Serializable]
    public class ActionRunner
    {
        [SerializeReference, SubClassSelector] public List<IAction> _actions = new List<IAction>();

        /// <summary>
        /// Gets the number of actions in the runner.
        /// </summary>
        public int ActionCount => _actions.Count;

        /// <summary>
        /// Gets or sets whether to stop execution on the first exception.
        /// Default is true.
        /// </summary>
        public bool StopOnError { get; set; } = true;

        /// <summary>
        /// Gets the list of actions for external manipulation.
        /// </summary>
        public List<IAction> Actions => _actions;

        /// <summary>
        /// Executes all actions in sequence.
        /// </summary>
        /// <param name="context">Optional context object passed to each action</param>
        public void RunAll(object context = null)
        {
            foreach (var action in _actions)
            {
                try
                {
                    action.Execute(context);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ActionRunner: Error executing action: {ex.Message}");
                    if (StopOnError)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Executes actions starting from a specific index.
        /// </summary>
        /// <param name="startIndex">The index to start execution from</param>
        /// <param name="context">Optional context object passed to each action</param>
        public void RunFrom(int startIndex, object context = null)
        {
            if (startIndex < 0 || startIndex >= _actions.Count)
            {
                Debug.LogWarning($"ActionRunner: Invalid start index {startIndex}");
                return;
            }

            for (int i = startIndex; i < _actions.Count; i++)
            {
                try
                {
                    _actions[i].Execute(context);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ActionRunner: Error executing action at index {i}: {ex.Message}");
                    if (StopOnError)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Executes a specific action by index.
        /// </summary>
        /// <param name="index">The index of the action to execute</param>
        /// <param name="context">Optional context object passed to the action</param>
        public void RunAction(int index, object context = null)
        {
            if (index < 0 || index >= _actions.Count)
            {
                Debug.LogWarning($"ActionRunner: Invalid action index {index}");
                return;
            }

            try
            {
                _actions[index].Execute(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ActionRunner: Error executing action at index {index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an action to the end of the action list.
        /// </summary>
        /// <param name="action">The action to add</param>
        public void AddAction(IAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("ActionRunner: Cannot add null action");
                return;
            }
            _actions.Add(action);
        }

        /// <summary>
        /// Inserts an action at a specific index.
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="action">The action to insert</param>
        public void InsertAction(int index, IAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("ActionRunner: Cannot insert null action");
                return;
            }
            _actions.Insert(index, action);
        }

        /// <summary>
        /// Removes an action at a specific index.
        /// </summary>
        /// <param name="index">The index of the action to remove</param>
        /// <returns>True if the action was removed, false otherwise</returns>
        public bool RemoveAction(int index)
        {
            if (index < 0 || index >= _actions.Count)
            {
                return false;
            }
            _actions.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes all actions from the runner.
        /// </summary>
        public void ClearActions()
        {
            _actions.Clear();
        }

        /// <summary>
        /// Gets an action at a specific index.
        /// </summary>
        /// <param name="index">The index of the action</param>
        /// <returns>The action at the specified index, or null if invalid</returns>
        public IAction GetAction(int index)
        {
            if (index < 0 || index >= _actions.Count)
            {
                return null;
            }
            return _actions[index];
        }

        /// <summary>
        /// Checks if the runner contains any actions.
        /// </summary>
        /// <returns>True if there are no actions, false otherwise</returns>
        public bool IsEmpty()
        {
            return _actions.Count == 0;
        }
    }
}

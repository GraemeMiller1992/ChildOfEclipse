using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Actions;
using World;

namespace Actions
{
    /// <summary>
    /// Represents a single condition for the solar state trigger.
    /// Each condition specifies a SolarState object and the state it must be in.
    /// </summary>
    [Serializable]
    public class SolarStateCondition
    {
        [SerializeField]
        [Tooltip("The SolarState component to monitor.")]
        private World.SolarState _solarState;

        [SerializeField]
        [Tooltip("The required state for this condition to be met.")]
        private World.SolarStateValue _requiredState = World.SolarStateValue.Sun;

        /// <summary>
        /// Gets or sets the SolarState component to monitor.
        /// </summary>
        public World.SolarState SolarState
        {
            get => _solarState;
            set => _solarState = value;
        }

        /// <summary>
        /// Gets or sets the required state for this condition.
        /// </summary>
        public World.SolarStateValue RequiredState
        {
            get => _requiredState;
            set => _requiredState = value;
        }

        /// <summary>
        /// Checks if this condition is currently met.
        /// </summary>
        /// <returns>True if the SolarState is in the required state, false otherwise</returns>
        public bool IsMet()
        {
            return _solarState != null && _solarState.CurrentState == _requiredState;
        }

        /// <summary>
        /// Gets the name of the target object for debugging purposes.
        /// </summary>
        public string GetTargetName()
        {
            return _solarState != null ? _solarState.gameObject.name : "None";
        }
    }

    /// <summary>
    /// A trigger that runs an ActionRunner when multiple solar state objects meet their required states.
    /// All conditions must be met simultaneously for the trigger to activate.
    /// </summary>
    public class SolarStateActionTrigger : MonoBehaviour
    {
        [Header("Trigger Conditions")]
        [SerializeField]
        [Tooltip("List of solar state conditions. All must be met for the trigger to activate.")]
        private List<SolarStateCondition> _conditions = new List<SolarStateCondition>();

        [Header("Action Settings")]
        [SerializeField]
        [Tooltip("The action runner to execute when all conditions are met.")]
        private ActionRunner _actionRunner;

        [SerializeField]
        [Tooltip("Whether to trigger only once or every time all conditions are met.")]
        private bool _triggerOnce = true;

        [SerializeField]
        [Tooltip("Whether to trigger immediately on Awake if all conditions are already met.")]
        private bool _triggerOnAwake = false;

        [SerializeField]
        [Tooltip("Optional context object to pass to the action runner.")]
        private UnityEngine.Object _contextObject;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Show debug logs for condition state changes.")]
        private bool _showDebugLogs = true;

        private bool _hasTriggered = false;
        private bool _allConditionsMet = false;

        /// <summary>
        /// Gets the list of conditions.
        /// </summary>
        public List<SolarStateCondition> Conditions => _conditions;

        /// <summary>
        /// Gets or sets the action runner to execute.
        /// </summary>
        public ActionRunner ActionRunner
        {
            get => _actionRunner;
            set => _actionRunner = value;
        }

        /// <summary>
        /// Gets or sets whether to trigger only once.
        /// </summary>
        public bool TriggerOnce
        {
            get => _triggerOnce;
            set => _triggerOnce = value;
        }

        /// <summary>
        /// Gets or sets the context object passed to actions.
        /// </summary>
        public UnityEngine.Object ContextObject
        {
            get => _contextObject;
            set => _contextObject = value;
        }

        /// <summary>
        /// Gets whether all conditions are currently met.
        /// </summary>
        public bool AllConditionsMet => _allConditionsMet;

        private void Awake()
        {
            // Subscribe to all solar state components
            SubscribeToConditions();

            // Check initial state
            CheckConditions();

            // Trigger on awake if enabled and all conditions are met
            if (_triggerOnAwake && _allConditionsMet)
            {
                TriggerActions();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from all solar state components
            UnsubscribeFromConditions();
        }

        /// <summary>
        /// Subscribes to the OnSolarStateChanged event for all conditions.
        /// </summary>
        private void SubscribeToConditions()
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                var condition = _conditions[i];
                if (condition.SolarState != null)
                {
                    condition.SolarState.OnSolarStateChanged += HandleSolarStateChanged;
                }
                else if (_showDebugLogs)
                {
                    Debug.LogWarning($"SolarStateActionTrigger: Condition {i} has no SolarState assigned on {gameObject.name}");
                }
            }
        }

        /// <summary>
        /// Unsubscribes from the OnSolarStateChanged event for all conditions.
        /// </summary>
        private void UnsubscribeFromConditions()
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                var condition = _conditions[i];
                if (condition.SolarState != null)
                {
                    condition.SolarState.OnSolarStateChanged -= HandleSolarStateChanged;
                }
            }
        }

        /// <summary>
        /// Handles the solar state changed event from any monitored SolarState component.
        /// </summary>
        /// <param name="oldState">The previous state</param>
        /// <param name="newState">The new state</param>
        private void HandleSolarStateChanged(World.SolarStateValue oldState, World.SolarStateValue newState)
        {
            CheckConditions();
        }

        /// <summary>
        /// Checks if all conditions are met and triggers actions if appropriate.
        /// </summary>
        private void CheckConditions()
        {
            bool previouslyMet = _allConditionsMet;

            // Check all conditions
            _allConditionsMet = AreAllConditionsMet();

            if (_showDebugLogs && previouslyMet != _allConditionsMet)
            {
                Debug.Log($"SolarStateActionTrigger: All conditions changed from {previouslyMet} to {_allConditionsMet} on {gameObject.name}");
            }

            // Trigger if all conditions are met
            if (_allConditionsMet && !previouslyMet)
            {
                TriggerActions();
            }
        }

        /// <summary>
        /// Checks if all conditions are currently met.
        /// </summary>
        /// <returns>True if all conditions are met, false otherwise</returns>
        public bool AreAllConditionsMet()
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (!_conditions[i].IsMet())
                {
                    return false;
                }
            }
            return _conditions.Count > 0;
        }

        /// <summary>
        /// Gets the number of conditions currently met.
        /// </summary>
        /// <returns>The count of met conditions</returns>
        public int GetMetConditionCount()
        {
            int count = 0;
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (_conditions[i].IsMet())
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a list of condition statuses for debugging.
        /// </summary>
        /// <returns>A list of tuples containing condition info and met status</returns>
        public List<(string objectName, World.SolarStateValue requiredState, World.SolarStateValue currentState, bool isMet)> GetConditionStatuses()
        {
            var statuses = new List<(string, World.SolarStateValue, World.SolarStateValue, bool)>();
            for (int i = 0; i < _conditions.Count; i++)
            {
                var condition = _conditions[i];
                if (condition.SolarState != null)
                {
                    statuses.Add((
                        condition.GetTargetName(),
                        condition.RequiredState,
                        condition.SolarState.CurrentState,
                        condition.IsMet()
                    ));
                }
            }
            return statuses;
        }

        /// <summary>
        /// Triggers the action runner if conditions are met.
        /// </summary>
        private void TriggerActions()
        {
            // Check if we should trigger
            if (_triggerOnce && _hasTriggered)
            {
                return;
            }

            // Check if we have an action runner
            if (_actionRunner == null)
            {
                Debug.LogWarning($"SolarStateActionTrigger: No action runner assigned on {gameObject.name}");
                return;
            }

            // Check if the action runner has any actions
            if (_actionRunner.IsEmpty())
            {
                Debug.LogWarning($"SolarStateActionTrigger: Action runner has no actions on {gameObject.name}");
                return;
            }

            // Run the actions
            _hasTriggered = true;
            _actionRunner.RunAll(_contextObject);

            if (_showDebugLogs)
            {
                Debug.Log($"SolarStateActionTrigger: Triggered actions on {gameObject.name} - {GetMetConditionCount()}/{_conditions.Count} conditions met");
            }
        }

        /// <summary>
        /// Manually triggers the action runner regardless of conditions.
        /// Useful for debugging or external triggering.
        /// </summary>
        public void ManualTrigger()
        {
            TriggerActions();
        }

        /// <summary>
        /// Resets the trigger state, allowing it to trigger again if TriggerOnce is enabled.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            CheckConditions(); // Re-check conditions in case they're still met
        }

        /// <summary>
        /// Adds a new condition to the trigger.
        /// </summary>
        /// <param name="solarState">The SolarState component to monitor</param>
        /// <param name="requiredState">The required state</param>
        public void AddCondition(World.SolarState solarState, World.SolarStateValue requiredState)
        {
            var condition = new SolarStateCondition
            {
                SolarState = solarState,
                RequiredState = requiredState
            };
            _conditions.Add(condition);
            solarState.OnSolarStateChanged += HandleSolarStateChanged;
            CheckConditions();
        }

        /// <summary>
        /// Removes a condition at the specified index.
        /// </summary>
        /// <param name="index">The index of the condition to remove</param>
        public void RemoveCondition(int index)
        {
            if (index >= 0 && index < _conditions.Count)
            {
                if (_conditions[index].SolarState != null)
                {
                    _conditions[index].SolarState.OnSolarStateChanged -= HandleSolarStateChanged;
                }
                _conditions.RemoveAt(index);
                CheckConditions();
            }
        }

        /// <summary>
        /// Removes all conditions from the trigger.
        /// </summary>
        public void ClearConditions()
        {
            UnsubscribeFromConditions();
            _conditions.Clear();
            CheckConditions();
        }

        /// <summary>
        /// Checks if this trigger has already been triggered.
        /// </summary>
        /// <returns>True if triggered (and TriggerOnce is enabled), false otherwise</returns>
        public bool HasTriggered()
        {
            return _hasTriggered;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Visual feedback in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw lines to all monitored solar state objects
            Gizmos.color = _allConditionsMet ? Color.green : Color.yellow;
            
            for (int i = 0; i < _conditions.Count; i++)
            {
                var condition = _conditions[i];
                if (condition.SolarState != null)
                {
                    // Draw a line from this trigger to the monitored object
                    Gizmos.DrawLine(transform.position, condition.SolarState.transform.position);
                    
                    // Draw a sphere at the monitored object
                    Color sphereColor = condition.IsMet() ? Color.green : Color.red;
                    Gizmos.color = sphereColor;
                    Gizmos.DrawWireSphere(condition.SolarState.transform.position, 0.3f);
                }
            }

            // Draw a sphere at this trigger's position
            Gizmos.color = _allConditionsMet ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
#endif
    }
}

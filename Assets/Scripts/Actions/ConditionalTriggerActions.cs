using System;
using System.Collections.Generic;
using UnityEngine;

namespace Actions
{
    /// <summary>
    /// A MonoBehaviour component that evaluates a list of IConditions and runs an ActionRunner
    /// when all conditions are met. Uses SubClassSelector for easy condition selection in the Inspector.
    /// </summary>
    public class ConditionalTriggerActions : MonoBehaviour
    {
        [Header("Conditions")]
        [SerializeReference, SubClassSelector]
        [Tooltip("List of conditions that must all be met for the actions to run")]
        private List<ICondition> _conditions = new List<ICondition>();

        [Header("Action Runner")]
        [SerializeReference]
        [Tooltip("The ActionRunner to execute when all conditions are met")]
        private ActionRunner _actionRunner = new ActionRunner();

        [Header("Trigger Settings")]
        [SerializeField]
        [Tooltip("How often to check conditions (in seconds). 0 = check every frame")]
        private float _checkInterval = 0f;

        [SerializeField]
        [Tooltip("Whether to trigger only once and then disable")]
        private bool _triggerOnce = false;

        [SerializeField]
        [Tooltip("Whether to trigger immediately on Start if all conditions are already met")]
        private bool _triggerOnStart = false;

        [SerializeField]
        [Tooltip("Optional context object to pass to the action runner")]
        private UnityEngine.Object _contextObject;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Show debug logs for condition state changes")]
        private bool _showDebugLogs = false;

        [SerializeField]
        [Tooltip("Show condition status in the Inspector")]
        private bool _showConditionStatus = true;

        private bool _hasTriggered = false;
        private bool _allConditionsMet = false;
        private float _lastCheckTime = 0f;
        private bool _initialized = false;

        /// <summary>
        /// Gets the list of conditions.
        /// </summary>
        public List<ICondition> Conditions => _conditions;

        /// <summary>
        /// Gets or sets the action runner to execute.
        /// </summary>
        public ActionRunner ActionRunner
        {
            get => _actionRunner;
            set => _actionRunner = value;
        }

        /// <summary>
        /// Gets or sets how often to check conditions (in seconds).
        /// </summary>
        public float CheckInterval
        {
            get => _checkInterval;
            set => _checkInterval = value;
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
        /// Gets or sets whether to trigger on start.
        /// </summary>
        public bool TriggerOnStart
        {
            get => _triggerOnStart;
            set => _triggerOnStart = value;
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

        /// <summary>
        /// Gets whether this trigger has already been triggered (when TriggerOnce is enabled).
        /// </summary>
        public bool HasTriggered => _hasTriggered;

        private void Start()
        {
            InitializeConditions();
            CheckConditions();

            if (_triggerOnStart && _allConditionsMet)
            {
                TriggerActions();
            }

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            // Check if enough time has passed since the last check
            if (_checkInterval > 0f)
            {
                if (Time.time - _lastCheckTime >= _checkInterval)
                {
                    CheckConditions();
                    _lastCheckTime = Time.time;
                }
            }
            else
            {
                // Check every frame
                CheckConditions();
            }
        }

        /// <summary>
        /// Initializes any conditions that need setup (e.g., TimeElapsedCondition).
        /// </summary>
        private void InitializeConditions()
        {
            foreach (var condition in _conditions)
            {
                if (condition != null && condition is TimeElapsedCondition timeCondition)
                {
                    timeCondition.Initialize();
                }
            }
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
                Debug.Log($"ConditionalTriggerActions: All conditions changed from {previouslyMet} to {_allConditionsMet} on {gameObject.name}");
            }

            // Trigger if all conditions are met and this is a new state
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
            if (_conditions.Count == 0)
            {
                if (_showDebugLogs)
                {
                    Debug.LogWarning($"ConditionalTriggerActions: No conditions set on {gameObject.name}");
                }
                return false;
            }

            for (int i = 0; i < _conditions.Count; i++)
            {
                if (_conditions[i] == null || !_conditions[i].IsMet())
                {
                    return false;
                }
            }
            return true;
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
                if (_conditions[i] != null && _conditions[i].IsMet())
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
        public List<(string description, bool isMet)> GetConditionStatuses()
        {
            var statuses = new List<(string, bool)>();
            for (int i = 0; i < _conditions.Count; i++)
            {
                var condition = _conditions[i];
                if (condition != null)
                {
                    statuses.Add((
                        condition.GetDescription(),
                        condition.IsMet()
                    ));
                }
                else
                {
                    statuses.Add((
                        "None (null condition)",
                        false
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
                Debug.LogWarning($"ConditionalTriggerActions: No action runner assigned on {gameObject.name}");
                return;
            }

            // Check if the action runner has any actions
            if (_actionRunner.IsEmpty())
            {
                Debug.LogWarning($"ConditionalTriggerActions: Action runner has no actions on {gameObject.name}");
                return;
            }

            // Run the actions
            _hasTriggered = true;
            _actionRunner.RunAll(_contextObject);

            if (_showDebugLogs)
            {
                Debug.Log($"ConditionalTriggerActions: Triggered actions on {gameObject.name} - {GetMetConditionCount()}/{_conditions.Count} conditions met");
            }
        }

        /// <summary>
        /// Manually triggers the action runner regardless of conditions.
        /// Useful for debugging or external triggering.
        /// </summary>
        public void ManualTrigger()
        {
            if (_showDebugLogs)
            {
                Debug.Log($"ConditionalTriggerActions: Manual trigger called on {gameObject.name}");
            }
            TriggerActions();
        }

        /// <summary>
        /// Manually checks and triggers if conditions are met.
        /// </summary>
        public void ManualCheckAndTrigger()
        {
            CheckConditions();
        }

        /// <summary>
        /// Resets the trigger state, allowing it to trigger again if TriggerOnce is enabled.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            CheckConditions(); // Re-check conditions in case they're still met
            if (_showDebugLogs)
            {
                Debug.Log($"ConditionalTriggerActions: Trigger state reset on {gameObject.name}");
            }
        }

        /// <summary>
        /// Adds a new condition to the trigger.
        /// </summary>
        /// <param name="condition">The condition to add</param>
        public void AddCondition(ICondition condition)
        {
            if (condition == null)
            {
                Debug.LogWarning("ConditionalTriggerActions: Cannot add null condition");
                return;
            }
            _conditions.Add(condition);
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
                _conditions.RemoveAt(index);
                CheckConditions();
            }
        }

        /// <summary>
        /// Removes all conditions from the trigger.
        /// </summary>
        public void ClearConditions()
        {
            _conditions.Clear();
            CheckConditions();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Visual feedback in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = _allConditionsMet ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            if (_showConditionStatus)
            {
                // Draw condition status text
                int metCount = GetMetConditionCount();
                string status = $"{metCount}/{_conditions.Count} conditions met";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, status);
            }
        }
#endif
    }
}

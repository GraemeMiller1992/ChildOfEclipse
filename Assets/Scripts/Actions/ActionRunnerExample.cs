using UnityEngine;
using Actions;

namespace Actions
{
    /// <summary>
    /// Example MonoBehaviour that demonstrates how to use the ActionRunner.
    /// This component can be attached to any GameObject and will run the configured
    /// actions when triggered (e.g., on collision, on trigger, on button press, etc.).
    /// </summary>
    public class ActionRunnerExample : MonoBehaviour
    {
        [Header("Action Runner Configuration")]
        [Tooltip("The ActionRunner that holds the list of actions to execute")]
        public ActionRunner actionRunner = new ActionRunner();

        [Header("Trigger Settings")]
        [Tooltip("When to run the actions")]
        public TriggerType triggerType = TriggerType.OnStart;

        [Tooltip("For collision triggers, specify the tag to filter collisions (optional)")]
        public string collisionTag = string.Empty;

        [Tooltip("For key press trigger, specify the key to press")]
        public KeyCode triggerKey = KeyCode.Space;

        [Tooltip("Delay before running actions (in seconds)")]
        public float delay = 0f;

        private bool _hasTriggered = false;

        public enum TriggerType
        {
            OnStart,
            OnEnable,
            OnDisable,
            OnCollisionEnter,
            OnTriggerEnter,
            OnKeyPress,
            OnMouseDown,
            Manual
        }

        private void Start()
        {
            if (triggerType == TriggerType.OnStart)
            {
                RunActionsDelayed();
            }
        }

        private void OnEnable()
        {
            if (triggerType == TriggerType.OnEnable)
            {
                RunActionsDelayed();
            }
        }

        private void OnDisable()
        {
            if (triggerType == TriggerType.OnDisable)
            {
                RunActions();
            }
        }

        private void Update()
        {
            if (triggerType == TriggerType.OnKeyPress && Input.GetKeyDown(triggerKey))
            {
                RunActions();
            }
        }

        private void OnMouseDown()
        {
            if (triggerType == TriggerType.OnMouseDown)
            {
                RunActions();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (triggerType == TriggerType.OnCollisionEnter)
            {
                if (string.IsNullOrEmpty(collisionTag) || collision.gameObject.CompareTag(collisionTag))
                {
                    RunActions();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType == TriggerType.OnTriggerEnter)
            {
                if (string.IsNullOrEmpty(collisionTag) || other.gameObject.CompareTag(collisionTag))
                {
                    RunActions();
                }
            }
        }

        /// <summary>
        /// Manually trigger the action runner.
        /// </summary>
        public void RunActions()
        {
            if (actionRunner == null)
            {
                Debug.LogWarning("ActionRunnerExample: ActionRunner is null");
                return;
            }

            if (actionRunner.IsEmpty())
            {
                Debug.LogWarning("ActionRunnerExample: No actions to run");
                return;
            }

            Debug.Log($"ActionRunnerExample: Running {actionRunner.ActionCount} action(s)");
            actionRunner.RunAll(gameObject);
        }

        /// <summary>
        /// Run actions after a delay.
        /// </summary>
        public void RunActionsDelayed()
        {
            if (delay > 0)
            {
                Invoke(nameof(RunActions), delay);
            }
            else
            {
                RunActions();
            }
        }

        /// <summary>
        /// Run actions starting from a specific index.
        /// </summary>
        /// <param name="startIndex">The index to start from</param>
        public void RunActionsFrom(int startIndex)
        {
            if (actionRunner == null)
            {
                Debug.LogWarning("ActionRunnerExample: ActionRunner is null");
                return;
            }

            actionRunner.RunFrom(startIndex, gameObject);
        }

        /// <summary>
        /// Run a specific action by index.
        /// </summary>
        /// <param name="index">The index of the action to run</param>
        public void RunAction(int index)
        {
            if (actionRunner == null)
            {
                Debug.LogWarning("ActionRunnerExample: ActionRunner is null");
                return;
            }

            actionRunner.RunAction(index, gameObject);
        }

        /// <summary>
        /// Reset the trigger state (for one-time triggers).
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        /// <summary>
        /// Clear all actions from the runner.
        /// </summary>
        public void ClearActions()
        {
            actionRunner?.ClearActions();
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to test actions in the Unity Editor.
        /// </summary>
        [ContextMenu("Test Run Actions")]
        private void TestRunActions()
        {
            RunActions();
        }
        #endif
    }

    /// <summary>
    /// Example of how to use ActionRunner in a custom script without MonoBehaviour.
    /// This demonstrates the flexibility of the ActionRunner class.
    /// </summary>
    public class CustomActionSystem
    {
        private ActionRunner _runner;

        public CustomActionSystem()
        {
            _runner = new ActionRunner();
        }

        /// <summary>
        /// Run all actions.
        /// </summary>
        public void Run(object context = null)
        {
            _runner.RunAll(context);
        }

        /// <summary>
        /// Get the underlying ActionRunner for direct manipulation.
        /// </summary>
        public ActionRunner GetRunner()
        {
            return _runner;
        }
    }
}

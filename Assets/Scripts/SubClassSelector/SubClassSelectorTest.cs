using UnityEngine;
using Actions;

namespace Actions
{
    /// <summary>
    /// Test MonoBehaviour to demonstrate the SubClassSelector attribute.
    /// This component allows selecting IAction implementations from a dropdown menu.
    /// </summary>
    public class SubClassSelectorTest : MonoBehaviour
    {
        [Header("Single Action Test")]
        [Tooltip("Select an action implementation from the dropdown")]
        [SerializeReference]
        [SubClassSelector]
        public IAction singleAction;

        [Header("Multiple Actions Test")]
        [Tooltip("List of actions - each will have its own dropdown")]
        [SerializeReference]
        public IAction[] actionList;

        [Header("Action Runner Test")]
        [Tooltip("ActionRunner with SubClassSelector")]
        [SerializeReference]
        [SubClassSelector]
        public ActionRunner actionRunner;

        [Header("Action Settings")]
        [Tooltip("Should the actions run on Start?")]
        public bool runOnStart = false;

        [Tooltip("Should the actions run when the object is clicked?")]
        public bool runOnClick = false;

        private void Start()
        {
            if (runOnStart)
            {
                RunActions();
            }
        }

        private void OnMouseDown()
        {
            if (runOnClick)
            {
                RunActions();
            }
        }

        /// <summary>
        /// Run all configured actions.
        /// </summary>
        public void RunActions()
        {
            // Run the single action
            if (singleAction != null)
            {
                Debug.Log($"Running single action: {singleAction.GetType().Name}");
                singleAction.Execute(gameObject);
            }

            // Run the action list
            if (actionList != null && actionList.Length > 0)
            {
                Debug.Log($"Running {actionList.Length} actions from list");
                foreach (var action in actionList)
                {
                    if (action != null)
                    {
                        action.Execute(gameObject);
                    }
                }
            }

            // Run the ActionRunner
            if (actionRunner != null)
            {
                Debug.Log($"Running ActionRunner with {actionRunner.ActionCount} actions");
                actionRunner.RunAll(gameObject);
            }
        }

        /// <summary>
        /// Clear all actions.
        /// </summary>
        public void ClearActions()
        {
            singleAction = null;
            actionList = null;
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

        /// <summary>
        /// Editor-only method to clear all actions.
        /// </summary>
        [ContextMenu("Clear All Actions")]
        private void ClearAllActions()
        {
            ClearActions();
        }
#endif
    }
}

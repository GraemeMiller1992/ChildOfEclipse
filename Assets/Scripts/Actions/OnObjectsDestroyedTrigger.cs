using UnityEngine;
using System.Collections.Generic;
using Actions;

namespace Actions
{
    /// <summary>
    /// Triggers an ActionRunner when a list of GameObjects is destroyed.
    /// Can trigger when all objects are destroyed, or when any object is destroyed.
    /// </summary>
    public class OnObjectsDestroyedTrigger : MonoBehaviour
    {
        [Header("Target Objects")]
        [Tooltip("The list of GameObjects to monitor for destruction")]
        public List<GameObject> targetObjects;

        [Tooltip("Whether to trigger when ALL objects are destroyed (true) or ANY object is destroyed (false)")]
        public bool triggerWhenAllDestroyed = true;

        [Header("Action Runner")]
        [SerializeReference]
        [Tooltip("The ActionRunner to execute when the trigger condition is met")]
        public ActionRunner actionRunner = new ActionRunner();

        [Header("Settings")]
        [Tooltip("Whether to automatically find and track objects at start")]
        public bool autoFindObjects = false;

        [Tooltip("Tag filter for auto-finding objects (empty = no filter)")]
        public string autoFindTag = string.Empty;

        [Tooltip("Whether to log debug messages")]
        public bool debugMode = false;

        [Tooltip("Whether to destroy this component after triggering")]
        public bool destroyOnTrigger = false;

        // Track which objects have been destroyed
        private HashSet<GameObject> _destroyedObjects;
        private bool _hasTriggered = false;

        private void Awake()
        {
            _destroyedObjects = new HashSet<GameObject>();
        }

        private void Start()
        {
            if (autoFindObjects)
            {
                AutoFindTargetObjects();
            }

            // Start monitoring the target objects
            StartMonitoring();
        }

        /// <summary>
        /// Automatically find GameObjects based on tag or all objects in scene
        /// </summary>
        private void AutoFindTargetObjects()
        {
            if (string.IsNullOrEmpty(autoFindTag))
            {
                // Find all GameObjects in the scene
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                targetObjects = new List<GameObject>(allObjects);
                if (debugMode)
                {
                    Debug.Log($"OnObjectsDestroyedTrigger: Auto-found {targetObjects.Count} objects in scene");
                }
            }
            else
            {
                // Find GameObjects with specific tag
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(autoFindTag);
                targetObjects = new List<GameObject>(taggedObjects);
                if (debugMode)
                {
                    Debug.Log($"OnObjectsDestroyedTrigger: Auto-found {targetObjects.Count} objects with tag '{autoFindTag}'");
                }
            }
        }

        /// <summary>
        /// Start monitoring the target objects for destruction
        /// </summary>
        private void StartMonitoring()
        {
            if (targetObjects == null || targetObjects.Count == 0)
            {
                Debug.LogWarning("OnObjectsDestroyedTrigger: No target objects to monitor");
                return;
            }

            // Add ObjectDestroyedListener components to each target object
            foreach (var obj in targetObjects)
            {
                if (obj != null)
                {
                    ObjectDestroyedListener listener = obj.GetComponent<ObjectDestroyedListener>();
                    if (listener == null)
                    {
                        listener = obj.AddComponent<ObjectDestroyedListener>();
                    }
                    listener.OnDestroyed += HandleObjectDestroyed;
                }
            }

            if (debugMode)
            {
                Debug.Log($"OnObjectsDestroyedTrigger: Started monitoring {targetObjects.Count} objects");
            }
        }

        /// <summary>
        /// Handle when a monitored object is destroyed
        /// </summary>
        private void HandleObjectDestroyed(GameObject destroyedObject)
        {
            if (_hasTriggered)
            {
                return;
            }

            if (destroyedObject != null)
            {
                _destroyedObjects.Add(destroyedObject);
            }

            if (debugMode)
            {
                Debug.Log($"OnObjectsDestroyedTrigger: Object destroyed. Total destroyed: {_destroyedObjects.Count}/{targetObjects.Count}");
            }

            // Check if trigger condition is met
            if (CheckTriggerCondition())
            {
                TriggerActions();
            }
        }

        /// <summary>
        /// Check if the trigger condition is met
        /// </summary>
        private bool CheckTriggerCondition()
        {
            if (triggerWhenAllDestroyed)
            {
                // Check if all objects are destroyed
                int remainingCount = 0;
                foreach (var obj in targetObjects)
                {
                    if (obj != null)
                    {
                        remainingCount++;
                    }
                }
                return remainingCount == 0;
            }
            else
            {
                // Check if any object is destroyed
                return _destroyedObjects.Count > 0;
            }
        }

        /// <summary>
        /// Trigger the action runner
        /// </summary>
        private void TriggerActions()
        {
            _hasTriggered = true;

            if (actionRunner == null)
            {
                Debug.LogWarning("OnObjectsDestroyedTrigger: ActionRunner is null");
                return;
            }

            if (actionRunner.IsEmpty())
            {
                Debug.LogWarning("OnObjectsDestroyedTrigger: No actions to run");
                return;
            }

            if (debugMode)
            {
                Debug.Log("OnObjectsDestroyedTrigger: Triggering actions!");
            }

            actionRunner.RunAll(gameObject);

            if (destroyOnTrigger)
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// Manually add a target object to monitor
        /// </summary>
        public void AddTargetObject(GameObject obj)
        {
            if (obj != null && !targetObjects.Contains(obj))
            {
                targetObjects.Add(obj);
                ObjectDestroyedListener listener = obj.GetComponent<ObjectDestroyedListener>();
                if (listener == null)
                {
                    listener = obj.AddComponent<ObjectDestroyedListener>();
                }
                listener.OnDestroyed += HandleObjectDestroyed;
            }
        }

        /// <summary>
        /// Manually remove a target object from monitoring
        /// </summary>
        public void RemoveTargetObject(GameObject obj)
        {
            if (obj != null && targetObjects.Contains(obj))
            {
                targetObjects.Remove(obj);
                ObjectDestroyedListener listener = obj.GetComponent<ObjectDestroyedListener>();
                if (listener != null)
                {
                    listener.OnDestroyed -= HandleObjectDestroyed;
                }
            }
        }

        /// <summary>
        /// Get the count of remaining (not destroyed) objects
        /// </summary>
        public int GetRemainingObjectCount()
        {
            int count = 0;
            foreach (var obj in targetObjects)
            {
                if (obj != null)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Get the count of destroyed objects
        /// </summary>
        public int GetDestroyedObjectCount()
        {
            return _destroyedObjects.Count;
        }

        /// <summary>
        /// Reset the trigger state (for re-triggering)
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            _destroyedObjects.Clear();
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (targetObjects != null)
            {
                foreach (var obj in targetObjects)
                {
                    if (obj != null)
                    {
                        ObjectDestroyedListener listener = obj.GetComponent<ObjectDestroyedListener>();
                        if (listener != null)
                        {
                            listener.OnDestroyed -= HandleObjectDestroyed;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Internal helper component that detects when a GameObject is destroyed
    /// and raises an event.
    /// </summary>
    internal class ObjectDestroyedListener : MonoBehaviour
    {
        public event System.Action<GameObject> OnDestroyed;

        private void OnDestroy()
        {
            OnDestroyed?.Invoke(gameObject);
        }
    }
}

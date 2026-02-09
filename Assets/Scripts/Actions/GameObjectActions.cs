using UnityEngine;
using System;

namespace Actions
{
    #region GameObject Actions

    /// <summary>
    /// Activates or deactivates a GameObject.
    /// </summary>
    [Serializable]
    public class SetActiveAction : IAction
    {
        [Tooltip("The target GameObject to activate/deactivate")]
        public GameObject target;

        [Tooltip("Whether to activate (true) or deactivate (false) the GameObject")]
        public bool setActive = true;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                target.SetActive(setActive);
            }
            else
            {
                Debug.LogWarning("SetActiveAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Activates or deactivates a list of GameObjects.
    /// </summary>
    [Serializable]
    public class SetActiveListAction : IAction
    {
        [Tooltip("The list of GameObjects to activate/deactivate")]
        public GameObject[] targets;

        [Tooltip("Whether to activate (true) or deactivate (false) the GameObjects")]
        public bool setActive = true;

        [Tooltip("Whether to log warnings for null GameObjects in the list")]
        public bool logWarnings = true;

        public void Execute(object context = null)
        {
            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("SetActiveListAction: No GameObjects in the list");
                return;
            }

            int nullCount = 0;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(setActive);
                }
                else
                {
                    nullCount++;
                }
            }

            if (nullCount > 0 && logWarnings)
            {
                Debug.LogWarning($"SetActiveListAction: {nullCount} null GameObject(s) found in the list");
            }
        }
    }

    /// <summary>
    /// Destroys a GameObject.
    /// </summary>
    [Serializable]
    public class DestroyAction : IAction
    {
        [Tooltip("The target GameObject to destroy")]
        public GameObject target;

        [Tooltip("Delay before destruction in seconds")]
        public float delay = 0f;

        public void Execute(object context = null)
        {
            if (target != null)
            {
                if (delay > 0)
                {
                    GameObject.Destroy(target, delay);
                }
                else
                {
                    GameObject.Destroy(target);
                }
            }
            else
            {
                Debug.LogWarning("DestroyAction: Target GameObject is null");
            }
        }
    }

    /// <summary>
    /// Destroys a list of GameObjects.
    /// </summary>
    [Serializable]
    public class DestroyListAction : IAction
    {
        [Tooltip("The list of GameObjects to destroy")]
        public GameObject[] targets;

        [Tooltip("Delay before destruction in seconds")]
        public float delay = 0f;

        [Tooltip("Whether to log warnings for null GameObjects in the list")]
        public bool logWarnings = true;

        public void Execute(object context = null)
        {
            if (targets == null || targets.Length == 0)
            {
                Debug.LogWarning("DestroyListAction: No GameObjects in the list");
                return;
            }

            int nullCount = 0;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    if (delay > 0)
                    {
                        GameObject.Destroy(target, delay);
                    }
                    else
                    {
                        GameObject.Destroy(target);
                    }
                }
                else
                {
                    nullCount++;
                }
            }

            if (nullCount > 0 && logWarnings)
            {
                Debug.LogWarning($"DestroyListAction: {nullCount} null GameObject(s) found in the list");
            }
        }
    }

    /// <summary>
    /// Instantiates a prefab.
    /// </summary>
    [Serializable]
    public class InstantiateAction : IAction
    {
        [Tooltip("The prefab to instantiate")]
        public GameObject prefab;

        [Tooltip("Parent transform for the instantiated object (optional)")]
        public Transform parent;

        [Tooltip("Position for the instantiated object")]
        public Vector3 position = Vector3.zero;

        [Tooltip("Rotation for the instantiated object")]
        public Quaternion rotation = Quaternion.identity;

        public void Execute(object context = null)
        {
            if (prefab != null)
            {
                GameObject.Instantiate(prefab, position, rotation, parent);
            }
            else
            {
                Debug.LogWarning("InstantiateAction: Prefab is null");
            }
        }
    }

    /// <summary>
    /// Instantiates multiple prefabs.
    /// </summary>
    [Serializable]
    public class InstantiateMultipleAction : IAction
    {
        [Tooltip("The list of prefabs to instantiate")]
        public GameObject[] prefabs;

        [Tooltip("Parent transform for the instantiated objects (optional)")]
        public Transform parent;

        [Tooltip("Position for the instantiated objects")]
        public Vector3 position = Vector3.zero;

        [Tooltip("Rotation for the instantiated objects")]
        public Quaternion rotation = Quaternion.identity;

        [Tooltip("Whether to log warnings for null prefabs in the list")]
        public bool logWarnings = true;

        public void Execute(object context = null)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("InstantiateMultipleAction: No prefabs in the list");
                return;
            }

            int nullCount = 0;
            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    GameObject.Instantiate(prefab, position, rotation, parent);
                }
                else
                {
                    nullCount++;
                }
            }

            if (nullCount > 0 && logWarnings)
            {
                Debug.LogWarning($"InstantiateMultipleAction: {nullCount} null prefab(s) found in the list");
            }
        }
    }

    #endregion
}

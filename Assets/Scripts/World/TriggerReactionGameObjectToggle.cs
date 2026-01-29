using UnityEngine;
using System.Collections.Generic;

namespace World
{
    /// <summary>
    /// A trigger reaction that toggles GameObjects when the trigger activates.
    /// This component should be placed on the same GameObject as the GenericTrigger.
    /// </summary>
    public class TriggerReactionGameObjectToggle : MonoBehaviour, ITriggerReaction
    {
        [Header("GameObject Toggle Settings")]
        [Tooltip("List of GameObjects to enable when the trigger activates.")]
        [SerializeField] private List<GameObject> objectsToEnable = new List<GameObject>();

        [Tooltip("List of GameObjects to disable when the trigger activates.")]
        [SerializeField] private List<GameObject> objectsToDisable = new List<GameObject>();

        [Tooltip("Whether to toggle the active state (enable if disabled, disable if enabled) instead of forcing enable/disable.")]
        [SerializeField] private bool toggleState = false;

        [Tooltip("List of GameObjects to toggle when the trigger activates (only used if toggleState is true).")]
        [SerializeField] private List<GameObject> objectsToToggle = new List<GameObject>();

        private Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();

        /// <summary>
        /// Called when the trigger activates (all options passed).
        /// Toggles the GameObjects as configured.
        /// </summary>
        /// <param name="triggeringObject">The object that triggered the activation.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        public void OnActivated(GameObject triggeringObject, GenericTrigger trigger)
        {
            Debug.Log($"TriggerReactionGameObjectToggle: Trigger {trigger.gameObject.name} activated by {triggeringObject.name}", this);

            if (toggleState)
            {
                // Toggle the active state of objects in the toggle list
                foreach (GameObject obj in objectsToToggle)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!obj.activeSelf);
                        Debug.Log($"TriggerReactionGameObjectToggle: Toggled {obj.name} to {obj.activeSelf}", this);
                    }
                }
            }
            else
            {
                // Enable objects in the enable list
                foreach (GameObject obj in objectsToEnable)
                {
                    if (obj != null)
                    {
                        // Store original state before enabling
                        if (!originalStates.ContainsKey(obj))
                        {
                            originalStates[obj] = obj.activeSelf;
                        }
                        obj.SetActive(true);
                        Debug.Log($"TriggerReactionGameObjectToggle: Enabled {obj.name}", this);
                    }
                }

                // Disable objects in the disable list
                foreach (GameObject obj in objectsToDisable)
                {
                    if (obj != null)
                    {
                        // Store original state before disabling
                        if (!originalStates.ContainsKey(obj))
                        {
                            originalStates[obj] = obj.activeSelf;
                        }
                        obj.SetActive(false);
                        Debug.Log($"TriggerReactionGameObjectToggle: Disabled {obj.name}", this);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the triggering object leaves the trigger zone and the trigger
        /// is configured to reset state on exit.
        /// </summary>
        /// <param name="triggeringObject">The object that left the trigger zone.</param>
        /// <param name="trigger">The GenericTrigger component.</param>
        public void OnReset(GameObject triggeringObject, GenericTrigger trigger)
        {
            Debug.Log($"TriggerReactionGameObjectToggle: Trigger {trigger.gameObject.name} resetting for {triggeringObject.name}", this);

            if (toggleState)
            {
                // For toggle mode, we can't really reset since we don't know what the original state was
                // So we just toggle back
                foreach (GameObject obj in objectsToToggle)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!obj.activeSelf);
                        Debug.Log($"TriggerReactionGameObjectToggle: Reset toggled {obj.name} to {obj.activeSelf}", this);
                    }
                }
            }
            else
            {
                // Restore original states
                foreach (GameObject obj in objectsToEnable)
                {
                    if (obj != null && originalStates.ContainsKey(obj))
                    {
                        obj.SetActive(originalStates[obj]);
                        Debug.Log($"TriggerReactionGameObjectToggle: Reset {obj.name} to {originalStates[obj]}", this);
                    }
                }

                foreach (GameObject obj in objectsToDisable)
                {
                    if (obj != null && originalStates.ContainsKey(obj))
                    {
                        obj.SetActive(originalStates[obj]);
                        Debug.Log($"TriggerReactionGameObjectToggle: Reset {obj.name} to {originalStates[obj]}", this);
                    }
                }

                // Clear stored states
                originalStates.Clear();
            }
        }
    }
}

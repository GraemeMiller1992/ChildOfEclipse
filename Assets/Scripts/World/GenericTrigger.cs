using UnityEngine;
using System.Collections.Generic;

namespace World
{
    /// <summary>
    /// A generic overlap box that only activates when an object is within the overlap box
    /// and all configured trigger options evaluate to true.
    /// </summary>
    public class GenericTrigger : MonoBehaviour
    {
        [Header("Overlap Box Settings")]
        [Tooltip("The size of the overlap box area.")]
        [SerializeField] private Vector3 overlapBoxSize = new Vector3(3f, 3f, 3f);

        [Tooltip("The center offset of the overlap box relative to the transform position.")]
        [SerializeField] private Vector3 overlapBoxCenter = Vector3.zero;

        [Header("Trigger Settings")]
        [Tooltip("The layer mask for objects that can trigger this trigger.")]
        [SerializeField] private LayerMask triggerLayers = -1;

        [Tooltip("Whether to trigger only once per object entry.")]
        [SerializeField] private bool triggerOncePerEntry = true;

        [Tooltip("The cooldown time in seconds before an object can trigger again.")]
        [SerializeField] private float triggerCooldown = 0.5f;

        [Tooltip("Whether to reset reactions when object leaves the trigger zone.")]
        [SerializeField] private bool resetStateOnExit = true;

        [Header("Visualization")]
        [Tooltip("Color of the overlap box gizmo in the editor.")]
        [SerializeField] private Color triggerColor = new Color(1f, 1f, 0f, 0.3f);

        [Tooltip("Whether to show the overlap box gizmo in the editor.")]
        [SerializeField] private bool showGizmo = true;

        private Collider[] overlappingColliders = new Collider[32];
        private Dictionary<GameObject, float> triggerCooldowns = new Dictionary<GameObject, float>();
        private HashSet<GameObject> objectsInsideZone = new HashSet<GameObject>();
        private HashSet<GameObject> objectsThatTriggered = new HashSet<GameObject>();
        private ITriggerOption[] triggerOptions;
        private ITriggerReaction[] triggerReactions;

        private void Awake()
        {
            // Find all trigger options and reactions on this GameObject
            triggerOptions = GetComponents<ITriggerOption>();
            triggerReactions = GetComponents<ITriggerReaction>();
        }

        private void Update()
        {
            // Update cooldowns
            UpdateCooldowns();

            // Check for objects in the overlap box
            CheckForTriggerableObjects();
        }

        /// <summary>
        /// Checks for objects within the overlap box and triggers if eligible.
        /// </summary>
        private void CheckForTriggerableObjects()
        {
            // Create a new set to track objects currently inside the zone
            HashSet<GameObject> currentObjectsInside = new HashSet<GameObject>();

            int count = Physics.OverlapBoxNonAlloc(
                transform.position + transform.TransformDirection(overlapBoxCenter),
                overlapBoxSize * 0.5f,
                overlappingColliders,
                transform.rotation,
                triggerLayers
            );

            for (int i = 0; i < count; i++)
            {
                GameObject obj = overlappingColliders[i].gameObject;
                currentObjectsInside.Add(obj);

                // Skip if object is on cooldown
                if (triggerOncePerEntry && triggerCooldowns.ContainsKey(obj))
                {
                    continue;
                }

                // Skip if object has already triggered while inside the zone
                if (triggerOncePerEntry && objectsThatTriggered.Contains(obj))
                {
                    continue;
                }

                // Evaluate all trigger options
                if (EvaluateTriggerOptions(obj))
                {
                    // All options passed, activate the trigger
                    ActivateTrigger(obj);

                    // Mark object as having triggered
                    if (triggerOncePerEntry)
                    {
                        objectsThatTriggered.Add(obj);
                        triggerCooldowns[obj] = Time.time + triggerCooldown;
                    }
                }
            }

            // Remove objects that are no longer inside the zone from the triggered set
            foreach (GameObject obj in objectsInsideZone)
            {
                if (!currentObjectsInside.Contains(obj))
                {
                    objectsThatTriggered.Remove(obj);

                    // Reset reactions if configured
                    if (resetStateOnExit && triggerReactions != null)
                    {
                        foreach (var reaction in triggerReactions)
                        {
                            if (reaction != null)
                            {
                                reaction.OnReset(obj, this);
                            }
                        }
                    }
                }
            }

            // Update the set of objects inside the zone
            objectsInsideZone = currentObjectsInside;
        }

        /// <summary>
        /// Evaluates all trigger options to determine if activation should occur.
        /// </summary>
        /// <param name="triggeringObject">The object that entered the trigger.</param>
        /// <returns>True if all options pass, false otherwise.</returns>
        private bool EvaluateTriggerOptions(GameObject triggeringObject)
        {
            // If no options are configured, allow activation by default
            if (triggerOptions == null || triggerOptions.Length == 0)
            {
                return true;
            }

            // All options must pass for activation
            foreach (var option in triggerOptions)
            {
                if (option == null)
                {
                    continue;
                }

                if (!option.ShouldActivate(triggeringObject, this))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Activates the trigger and notifies all options and reactions.
        /// </summary>
        /// <param name="triggeringObject">The object that triggered the activation.</param>
        private void ActivateTrigger(GameObject triggeringObject)
        {
            Debug.Log($"GenericTrigger: {gameObject.name} activated by {triggeringObject.name}", this);

            // Notify all options that the trigger activated
            if (triggerOptions != null)
            {
                foreach (var option in triggerOptions)
                {
                    if (option != null)
                    {
                        option.OnActivated(triggeringObject, this);
                    }
                }
            }

            // Notify all reactions that the trigger activated
            if (triggerReactions != null)
            {
                foreach (var reaction in triggerReactions)
                {
                    if (reaction != null)
                    {
                        reaction.OnActivated(triggeringObject, this);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the cooldown timers for all recently triggered objects.
        /// </summary>
        private void UpdateCooldowns()
        {
            List<GameObject> keysToRemove = new List<GameObject>();

            foreach (var kvp in triggerCooldowns)
            {
                if (Time.time >= kvp.Value)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (GameObject key in keysToRemove)
            {
                triggerCooldowns.Remove(key);
            }
        }

        /// <summary>
        /// Draws editor gizmos to visualize the overlap box area.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmo)
            {
                return;
            }

            Gizmos.color = triggerColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            // Draw wire cube
            Gizmos.DrawWireCube(overlapBoxCenter, overlapBoxSize);

            // Draw semi-transparent cube
            Color transparentColor = triggerColor;
            transparentColor.a *= 0.3f;
            Gizmos.color = transparentColor;
            Gizmos.DrawCube(overlapBoxCenter, overlapBoxSize);

            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// Resets the component to default values when added via inspector.
        /// </summary>
        private void Reset()
        {
            overlapBoxSize = new Vector3(3f, 3f, 3f);
            overlapBoxCenter = Vector3.zero;
            triggerLayers = -1;
            triggerOncePerEntry = true;
            triggerCooldown = 0.5f;
            resetStateOnExit = true;
            triggerColor = new Color(1f, 1f, 0f, 0.3f);
            showGizmo = true;
        }

        /// <summary>
        /// Public method to manually trigger activation for testing purposes.
        /// </summary>
        [ContextMenu("Test Trigger Activation")]
        public void TestTriggerActivation()
        {
            // Use the player or first found object for testing
            GameObject testObject = null;

            // Try to find the player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                testObject = player;
            }
            else
            {
                // Use this gameObject as fallback
                testObject = gameObject;
            }

            Debug.Log($"GenericTrigger: Testing activation for {gameObject.name} with {testObject.name}", this);

            // Evaluate all trigger options
            if (EvaluateTriggerOptions(testObject))
            {
                // All options passed, activate the trigger
                ActivateTrigger(testObject);
            }
        }
    }
}

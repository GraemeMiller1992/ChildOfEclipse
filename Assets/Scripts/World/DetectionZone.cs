using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace World
{
    /// <summary>
    /// A detection zone that monitors for colliders within an overlap box area.
    /// Triggers when a specified number of colliders from a layermask are detected.
    /// </summary>
    public class DetectionZone : MonoBehaviour
    {
        [Header("Overlap Box Settings")]
        [Tooltip("The size of the overlap box area.")]
        [SerializeField] private Vector3 boxSize = new Vector3(3f, 3f, 3f);

        [Tooltip("The center offset of the overlap box relative to the transform position.")]
        [SerializeField] private Vector3 boxCenter = Vector3.zero;

        [Header("Detection Settings")]
        [Tooltip("The layer mask for objects that can be detected.")]
        [SerializeField] private LayerMask detectionLayers = -1;

        [Tooltip("The required tag for detected objects. Leave empty to allow any tag.")]
        [SerializeField] private string requiredTag = "";

        [Tooltip("The minimum number of colliders required to trigger the zone.")]
        [SerializeField] private int triggerThreshold = 1;

        [Tooltip("Should the zone only trigger once per unique object entry?")]
        [SerializeField] private bool triggerOncePerObject = true;

        [Tooltip("Should the zone maintain the triggered state even after objects leave?")]
        [SerializeField] private bool maintainTriggeredState = false;

        [Header("Visualization")]
        [Tooltip("Color of the detection box gizmo when not triggered.")]
        [SerializeField] private Color normalColor = new Color(0f, 1f, 0f, 0.3f);

        [Tooltip("Color of the detection box gizmo when triggered.")]
        [SerializeField] private Color triggeredColor = new Color(1f, 0f, 0f, 0.5f);

        [Tooltip("Whether to show the detection box gizmo in the editor.")]
        [SerializeField] private bool showGizmo = true;

        [Header("Events")]
        [Space]
        [Tooltip("Invoked when the zone becomes triggered (threshold met).")]
        public UnityEvent OnTriggered;

        [Tooltip("Invoked when the zone becomes untriggered (falls below threshold).")]
        public UnityEvent OnUntriggered;

        [Tooltip("Invoked when any object enters the detection zone.")]
        public UnityEvent<GameObject> OnDetectionEnter;

        [Tooltip("Invoked when any object leaves the detection zone.")]
        public UnityEvent<GameObject> OnDetectionExit;

        private Collider[] detectedColliders = new Collider[32];
        private HashSet<GameObject> objectsInZone = new HashSet<GameObject>();
        private HashSet<GameObject> objectsThatTriggered = new HashSet<GameObject>();

        private bool _isTriggered = false;
        private bool _hasDetections = false;
        private int _currentDetectionCount = 0;

        /// <summary>
        /// Gets whether the zone is currently triggered (threshold met).
        /// </summary>
        public bool IsTriggered => _isTriggered;

        /// <summary>
        /// Gets whether there are any detections in the zone.
        /// </summary>
        public bool HasDetections => _hasDetections;

        /// <summary>
        /// Gets the current number of detected colliders in the zone.
        /// </summary>
        public int DetectionCount => _currentDetectionCount;

        private void Update()
        {
            CheckForDetections();
        }

        /// <summary>
        /// Checks for colliders within the overlap box and updates detection states.
        /// </summary>
        private void CheckForDetections()
        {
            // Create a new set to track objects currently inside the zone
            HashSet<GameObject> currentObjectsInZone = new HashSet<GameObject>();

            // Use OverlapBox to detect colliders
            int count = Physics.OverlapBoxNonAlloc(
                transform.position + transform.TransformDirection(boxCenter),
                boxSize * 0.5f,
                detectedColliders,
                transform.rotation,
                detectionLayers
            );

            // Process detected colliders
            for (int i = 0; i < count; i++)
            {
                GameObject obj = detectedColliders[i].gameObject;

                // Check tag filter
                if (!string.IsNullOrEmpty(requiredTag) && !obj.CompareTag(requiredTag))
                {
                    continue;
                }

                currentObjectsInZone.Add(obj);

                // Check if this is a new object entering the zone
                if (!objectsInZone.Contains(obj))
                {
                    OnDetectionEnter?.Invoke(obj);
                }
            }

            // Check for objects that left the zone
            foreach (GameObject obj in objectsInZone)
            {
                if (!currentObjectsInZone.Contains(obj))
                {
                    OnDetectionExit?.Invoke(obj);
                    objectsThatTriggered.Remove(obj);
                }
            }

            // Update the set of objects in the zone
            objectsInZone = currentObjectsInZone;

            // Update detection count
            _currentDetectionCount = objectsInZone.Count;

            // Update has detections state
            bool newHasDetections = _currentDetectionCount > 0;
            if (newHasDetections != _hasDetections)
            {
                _hasDetections = newHasDetections;
            }

            // Check trigger threshold
            int eligibleTriggerCount = 0;
            foreach (GameObject obj in objectsInZone)
            {
                if (!triggerOncePerObject || !objectsThatTriggered.Contains(obj))
                {
                    eligibleTriggerCount++;
                }
            }

            // Update triggered state
            bool newIsTriggered = eligibleTriggerCount >= triggerThreshold;
            
            if (newIsTriggered != _isTriggered)
            {
                _isTriggered = newIsTriggered;

                if (_isTriggered)
                {
                    OnTriggered?.Invoke();

                    // Mark objects as having triggered if configured
                    if (triggerOncePerObject)
                    {
                        foreach (GameObject obj in objectsInZone)
                        {
                            objectsThatTriggered.Add(obj);
                        }
                    }
                }
                else if (!maintainTriggeredState)
                {
                    OnUntriggered?.Invoke();
                }
            }
        }

        /// <summary>
        /// Resets the triggered state, allowing objects to trigger the zone again.
        /// </summary>
        public void ResetTriggeredState()
        {
            objectsThatTriggered.Clear();
            
            if (_isTriggered && !maintainTriggeredState)
            {
                _isTriggered = false;
                OnUntriggered?.Invoke();
            }
        }

        /// <summary>
        /// Manually sets the triggered state (useful for testing or external control).
        /// </summary>
        /// <param name="triggered">Whether to set as triggered or not.</param>
        public void SetTriggered(bool triggered)
        {
            if (triggered != _isTriggered)
            {
                _isTriggered = triggered;

                if (_isTriggered)
                {
                    OnTriggered?.Invoke();
                }
                else
                {
                    OnUntriggered?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets the list of GameObjects currently in the detection zone.
        /// </summary>
        /// <returns>An array of GameObjects in the zone.</returns>
        public GameObject[] GetObjectsInZone()
        {
            GameObject[] objects = new GameObject[objectsInZone.Count];
            objectsInZone.CopyTo(objects);
            return objects;
        }

        /// <summary>
        /// Draws editor gizmos to visualize the detection box area.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmo)
            {
                return;
            }

            // Choose color based on state
            Gizmos.color = _isTriggered ? triggeredColor : normalColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            // Draw wire cube
            Color wireColor = _isTriggered ? triggeredColor : normalColor;
            wireColor.a = 1f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(boxCenter, boxSize);

            // Draw semi-transparent cube
            Color fillColor = _isTriggered ? triggeredColor : normalColor;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(boxCenter, boxSize);

            Gizmos.matrix = Matrix4x4.identity;

            // Draw label with detection info
#if UNITY_EDITOR
            string label = $"Detection Zone\n" +
                          $"Objects: {_currentDetectionCount}\n" +
                          $"Threshold: {triggerThreshold}\n" +
                          $"Triggered: {_isTriggered}\n" +
                          $"Detected: {_hasDetections}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * (boxSize.y + 0.5f), label);
#endif
        }

        /// <summary>
        /// Resets the component to default values when added via inspector.
        /// </summary>
        private void Reset()
        {
            boxSize = new Vector3(3f, 3f, 3f);
            boxCenter = Vector3.zero;
            detectionLayers = -1;
            requiredTag = "";
            triggerThreshold = 1;
            triggerOncePerObject = true;
            maintainTriggeredState = false;
            normalColor = new Color(0f, 1f, 0f, 0.3f);
            triggeredColor = new Color(1f, 0f, 0f, 0.5f);
            showGizmo = true;
        }
    }
}

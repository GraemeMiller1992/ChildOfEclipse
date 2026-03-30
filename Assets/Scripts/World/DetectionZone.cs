using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using ChildOfEclipse;

namespace World
{
    /// <summary>
    /// A detection zone that monitors for colliders within an overlap box area.
    /// Triggers when a specified number of colliders from a layermask are detected.
    /// Can also automatically disable / enable SolarStateSwapInteractable objects
    /// when they enter or leave the zone.
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

        [Header("Interaction Locking")]
        [Tooltip("If enabled, SolarStateSwapInteractable components will be disabled when they enter the zone.")]
        [SerializeField] private bool disableSwapInteractionOnEnter = true;

        [Tooltip("If enabled, SolarStateSwapInteractable components will be re-enabled when they leave the zone.")]
        [SerializeField] private bool reEnableSwapInteractionOnExit = true;

        [Tooltip("Show debug messages for enter/exit interaction locking.")]
        [SerializeField] private bool showDebugMessages = false;

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
            HashSet<GameObject> currentObjectsInZone = new HashSet<GameObject>();

            int count = Physics.OverlapBoxNonAlloc(
                transform.position + transform.TransformDirection(boxCenter),
                boxSize * 0.5f,
                detectedColliders,
                transform.rotation,
                detectionLayers
            );

            for (int i = 0; i < count; i++)
            {
                if (detectedColliders[i] == null)
                {
                    continue;
                }

                GameObject obj = detectedColliders[i].gameObject;

                if (!string.IsNullOrEmpty(requiredTag) && !obj.CompareTag(requiredTag))
                {
                    continue;
                }

                currentObjectsInZone.Add(obj);

                if (!objectsInZone.Contains(obj))
                {
                    OnObjectEntered(obj);
                }
            }

            foreach (GameObject obj in objectsInZone)
            {
                if (!currentObjectsInZone.Contains(obj))
                {
                    OnObjectExited(obj);
                    objectsThatTriggered.Remove(obj);
                }
            }

            objectsInZone = currentObjectsInZone;
            _currentDetectionCount = objectsInZone.Count;

            bool newHasDetections = _currentDetectionCount > 0;
            if (newHasDetections != _hasDetections)
            {
                _hasDetections = newHasDetections;
            }

            int eligibleTriggerCount = 0;
            foreach (GameObject obj in objectsInZone)
            {
                if (!triggerOncePerObject || !objectsThatTriggered.Contains(obj))
                {
                    eligibleTriggerCount++;
                }
            }

            bool newIsTriggered = eligibleTriggerCount >= triggerThreshold;

            if (newIsTriggered != _isTriggered)
            {
                _isTriggered = newIsTriggered;

                if (_isTriggered)
                {
                    OnTriggered?.Invoke();

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

        private void OnObjectEntered(GameObject obj)
        {
            if (disableSwapInteractionOnEnter)
            {
                SolarStateSwapInteractable swapInteractable = obj.GetComponentInParent<SolarStateSwapInteractable>();
                if (swapInteractable != null)
                {
                    swapInteractable.DisableInteraction();

                    if (showDebugMessages)
                    {
                        Debug.Log($"{name}: Disabled swap interaction on {swapInteractable.gameObject.name}", this);
                    }
                }
            }

            OnDetectionEnter?.Invoke(obj);
        }

        private void OnObjectExited(GameObject obj)
        {
            if (reEnableSwapInteractionOnExit)
            {
                SolarStateSwapInteractable swapInteractable = obj.GetComponentInParent<SolarStateSwapInteractable>();
                if (swapInteractable != null)
                {
                    swapInteractable.EnableInteraction();

                    if (showDebugMessages)
                    {
                        Debug.Log($"{name}: Re-enabled swap interaction on {swapInteractable.gameObject.name}", this);
                    }
                }
            }

            OnDetectionExit?.Invoke(obj);
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
        /// Manually sets the triggered state.
        /// </summary>
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
        public GameObject[] GetObjectsInZone()
        {
            GameObject[] objects = new GameObject[objectsInZone.Count];
            objectsInZone.CopyTo(objects);
            return objects;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;

            Color wireColor = _isTriggered ? triggeredColor : normalColor;
            wireColor.a = 1f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(boxCenter, boxSize);

            Color fillColor = _isTriggered ? triggeredColor : normalColor;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(boxCenter, boxSize);

            Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
            string label =
                $"Detection Zone\n" +
                $"Objects: {_currentDetectionCount}\n" +
                $"Threshold: {triggerThreshold}\n" +
                $"Triggered: {_isTriggered}\n" +
                $"Detected: {_hasDetections}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * (boxSize.y + 0.5f), label);
#endif
        }

        private void Reset()
        {
            boxSize = new Vector3(3f, 3f, 3f);
            boxCenter = Vector3.zero;
            detectionLayers = -1;
            requiredTag = "";
            triggerThreshold = 1;
            triggerOncePerObject = true;
            maintainTriggeredState = false;
            disableSwapInteractionOnEnter = true;
            reEnableSwapInteractionOnExit = true;
            showDebugMessages = false;
            normalColor = new Color(0f, 1f, 0f, 0.3f);
            triggeredColor = new Color(1f, 0f, 0f, 0.5f);
            showGizmo = true;
        }
    }
}

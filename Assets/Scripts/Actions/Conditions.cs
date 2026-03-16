using System;
using UnityEngine;
using World;

namespace Actions
{
    #region GameObject Conditions

    /// <summary>
    /// Condition that checks if a GameObject is active.
    /// </summary>
    [Serializable]
    public class GameObjectActiveCondition : ICondition
    {
        [Tooltip("The GameObject to check")]
        public GameObject target;

        [Tooltip("Whether the GameObject should be active (true) or inactive (false)")]
        public bool shouldBeActive = true;

        public bool IsMet()
        {
            if (target == null)
            {
                Debug.LogWarning("GameObjectActiveCondition: Target GameObject is null");
                return false;
            }
            return target.activeSelf == shouldBeActive;
        }

        public string GetDescription()
        {
            string targetName = target != null ? target.name : "null";
            return $"GameObject '{targetName}' should be active: {shouldBeActive}";
        }
    }

    /// <summary>
    /// Condition that checks if a GameObject has a specific tag.
    /// </summary>
    [Serializable]
    public class TagCondition : ICondition
    {
        [Tooltip("The GameObject to check")]
        public GameObject target;

        [Tooltip("The tag to check for")]
        public string requiredTag = string.Empty;

        public bool IsMet()
        {
            if (target == null)
            {
                Debug.LogWarning("TagCondition: Target GameObject is null");
                return false;
            }
            if (string.IsNullOrEmpty(requiredTag))
            {
                Debug.LogWarning("TagCondition: Required tag is not set");
                return false;
            }
            return target.CompareTag(requiredTag);
        }

        public string GetDescription()
        {
            string targetName = target != null ? target.name : "null";
            return $"GameObject '{targetName}' should have tag: {requiredTag}";
        }
    }

    /// <summary>
    /// Condition that checks if a GameObject is on a specific layer.
    /// </summary>
    [Serializable]
    public class LayerCondition : ICondition
    {
        [Tooltip("The GameObject to check")]
        public GameObject target;

        [Tooltip("The layer to check for")]
        public int requiredLayer = 0;

        public bool IsMet()
        {
            if (target == null)
            {
                Debug.LogWarning("LayerCondition: Target GameObject is null");
                return false;
            }
            return target.layer == requiredLayer;
        }

        public string GetDescription()
        {
            string targetName = target != null ? target.name : "null";
            string layerName = LayerMask.LayerToName(requiredLayer);
            return $"GameObject '{targetName}' should be on layer: {layerName} ({requiredLayer})";
        }
    }

    /// <summary>
    /// Condition that checks if a GameObject is within a certain distance of another GameObject.
    /// </summary>
    [Serializable]
    public class DistanceCondition : ICondition
    {
        [Tooltip("The first GameObject")]
        public GameObject targetA;

        [Tooltip("The second GameObject")]
        public GameObject targetB;

        [Tooltip("The maximum distance between the two GameObjects")]
        public float maxDistance = 5f;

        [Tooltip("The minimum distance between the two GameObjects")]
        public float minDistance = 0f;

        public bool IsMet()
        {
            if (targetA == null || targetB == null)
            {
                Debug.LogWarning("DistanceCondition: One or both target GameObjects are null");
                return false;
            }
            float distance = Vector3.Distance(targetA.transform.position, targetB.transform.position);
            return distance >= minDistance && distance <= maxDistance;
        }

        public string GetDescription()
        {
            string nameA = targetA != null ? targetA.name : "null";
            string nameB = targetB != null ? targetB.name : "null";
            return $"Distance between '{nameA}' and '{nameB}' should be between {minDistance} and {maxDistance}";
        }
    }

    #endregion

    #region Solar State Conditions

    /// <summary>
    /// Condition that checks if a SolarState component is in a specific state.
    /// </summary>
    [Serializable]
    public class SolarStateValueCondition : ICondition
    {
        [Tooltip("The SolarState component to check")]
        public SolarState solarState;

        [Tooltip("The required state for this condition to be met")]
        public SolarStateValue requiredState = SolarStateValue.Sun;

        public bool IsMet()
        {
            if (solarState == null)
            {
                Debug.LogWarning("SolarStateValueCondition: SolarState component is null");
                return false;
            }
            return solarState.CurrentState == requiredState;
        }

        public string GetDescription()
        {
            string objectName = solarState != null ? solarState.gameObject.name : "null";
            return $"SolarState on '{objectName}' should be: {requiredState}";
        }
    }

    #endregion

    #region Component Conditions

    /// <summary>
    /// Condition that checks if a GameObject has a specific component.
    /// </summary>
    [Serializable]
    public class HasComponentCondition : ICondition
    {
        [Tooltip("The GameObject to check")]
        public GameObject target;

        [Tooltip("The type of component to check for (e.g., Rigidbody, Collider, etc.)")]
        public string componentType = "Rigidbody";

        public bool IsMet()
        {
            if (target == null)
            {
                Debug.LogWarning("HasComponentCondition: Target GameObject is null");
                return false;
            }
            if (string.IsNullOrEmpty(componentType))
            {
                Debug.LogWarning("HasComponentCondition: Component type is not set");
                return false;
            }
            return target.GetComponent(componentType) != null;
        }

        public string GetDescription()
        {
            string targetName = target != null ? target.name : "null";
            return $"GameObject '{targetName}' should have component: {componentType}";
        }
    }

    /// <summary>
    /// Condition that checks if a component on a GameObject is enabled.
    /// </summary>
    [Serializable]
    public class ComponentEnabledCondition : ICondition
    {
        [Tooltip("The GameObject to check")]
        public GameObject target;

        [Tooltip("The type of component to check (e.g., Rigidbody, Collider, etc.)")]
        public string componentType = "Rigidbody";

        [Tooltip("Whether the component should be enabled (true) or disabled (false)")]
        public bool shouldBeEnabled = true;

        public bool IsMet()
        {
            if (target == null)
            {
                Debug.LogWarning("ComponentEnabledCondition: Target GameObject is null");
                return false;
            }
            if (string.IsNullOrEmpty(componentType))
            {
                Debug.LogWarning("ComponentEnabledCondition: Component type is not set");
                return false;
            }
            var component = target.GetComponent(componentType);
            if (component == null)
            {
                Debug.LogWarning($"ComponentEnabledCondition: GameObject '{target.name}' does not have component '{componentType}'");
                return false;
            }
            if (component is Behaviour behaviour)
            {
                return behaviour.enabled == shouldBeEnabled;
            }
            if (component is Renderer renderer)
            {
                return renderer.enabled == shouldBeEnabled;
            }
            if (component is Collider collider)
            {
                return collider.enabled == shouldBeEnabled;
            }
            Debug.LogWarning($"ComponentEnabledCondition: Component '{componentType}' does not have an enabled property");
            return false;
        }

        public string GetDescription()
        {
            string targetName = target != null ? target.name : "null";
            return $"Component '{componentType}' on '{targetName}' should be enabled: {shouldBeEnabled}";
        }
    }

    #endregion

    #region Time Conditions

    /// <summary>
    /// Condition that checks if the game has been running for at least a certain amount of time.
    /// </summary>
    [Serializable]
    public class TimeElapsedCondition : ICondition
    {
        [Tooltip("The minimum time in seconds that must have elapsed")]
        public float minTime = 5f;

        [Tooltip("The maximum time in seconds (0 = no maximum)")]
        public float maxTime = 0f;

        private float _startTime;

        public void Initialize()
        {
            _startTime = Time.time;
        }

        public bool IsMet()
        {
            float elapsedTime = Time.time - _startTime;
            if (elapsedTime < minTime)
            {
                return false;
            }
            if (maxTime > 0f && elapsedTime > maxTime)
            {
                return false;
            }
            return true;
        }

        public string GetDescription()
        {
            if (maxTime > 0f)
            {
                return $"Time elapsed should be between {minTime} and {maxTime} seconds";
            }
            return $"Time elapsed should be at least {minTime} seconds";
        }
    }

    #endregion

    #region Boolean Conditions

    /// <summary>
    /// Condition that always returns true.
    /// </summary>
    [Serializable]
    public class AlwaysTrueCondition : ICondition
    {
        public bool IsMet()
        {
            return true;
        }

        public string GetDescription()
        {
            return "Always true";
        }
    }

    /// <summary>
    /// Condition that always returns false.
    /// </summary>
    [Serializable]
    public class AlwaysFalseCondition : ICondition
    {
        public bool IsMet()
        {
            return false;
        }

        public string GetDescription()
        {
            return "Always false";
        }
    }

    #endregion

    #region Player Conditions

    /// <summary>
    /// Condition that checks if the player is within a certain distance of a GameObject.
    /// </summary>
    [Serializable]
    public class PlayerDistanceCondition : ICondition
    {
        [Tooltip("The GameObject to check distance from")]
        public GameObject target;

        [Tooltip("The maximum distance from the player")]
        public float maxDistance = 5f;

        [Tooltip("The minimum distance from the player")]
        public float minDistance = 0f;

        public bool IsMet()
        {
            if (target == null)
            {
                Debug.LogWarning("PlayerDistanceCondition: Target GameObject is null");
                return false;
            }
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("PlayerDistanceCondition: Player not found (no GameObject with 'Player' tag)");
                return false;
            }
            float distance = Vector3.Distance(target.transform.position, player.transform.position);
            return distance >= minDistance && distance <= maxDistance;
        }

        public string GetDescription()
        {
            string targetName = target != null ? target.name : "null";
            return $"Player should be between {minDistance} and {maxDistance} units from '{targetName}'";
        }
    }

    #endregion

    #region Detection Zone Conditions

    /// <summary>
    /// Condition that checks if a DetectionZone is triggered (threshold met).
    /// </summary>
    [Serializable]
    public class DetectionZoneTriggeredCondition : ICondition
    {
        [Tooltip("The DetectionZone to check")]
        public DetectionZone detectionZone;

        [Tooltip("Whether the zone should be triggered (true) or not triggered (false)")]
        public bool shouldBeTriggered = true;

        public bool IsMet()
        {
            if (detectionZone == null)
            {
                Debug.LogWarning("DetectionZoneTriggeredCondition: DetectionZone is null");
                return false;
            }
            return detectionZone.IsTriggered == shouldBeTriggered;
        }

        public string GetDescription()
        {
            string zoneName = detectionZone != null ? detectionZone.gameObject.name : "null";
            return $"DetectionZone '{zoneName}' should be triggered: {shouldBeTriggered}";
        }
    }

    /// <summary>
    /// Condition that checks if a DetectionZone has any detections.
    /// </summary>
    [Serializable]
    public class DetectionZoneDetectedCondition : ICondition
    {
        [Tooltip("The DetectionZone to check")]
        public DetectionZone detectionZone;

        [Tooltip("Whether the zone should have detections (true) or no detections (false)")]
        public bool shouldHaveDetections = true;

        public bool IsMet()
        {
            if (detectionZone == null)
            {
                Debug.LogWarning("DetectionZoneDetectedCondition: DetectionZone is null");
                return false;
            }
            return detectionZone.HasDetections == shouldHaveDetections;
        }

        public string GetDescription()
        {
            string zoneName = detectionZone != null ? detectionZone.gameObject.name : "null";
            return $"DetectionZone '{zoneName}' should have detections: {shouldHaveDetections}";
        }
    }

    /// <summary>
    /// Condition that checks if a DetectionZone has a specific number of detected objects.
    /// </summary>
    [Serializable]
    public class DetectionZoneCountCondition : ICondition
    {
        [Tooltip("The DetectionZone to check")]
        public DetectionZone detectionZone;

        [Tooltip("The minimum number of detected objects")]
        public int minCount = 0;

        [Tooltip("The maximum number of detected objects (0 = no maximum)")]
        public int maxCount = 0;

        public bool IsMet()
        {
            if (detectionZone == null)
            {
                Debug.LogWarning("DetectionZoneCountCondition: DetectionZone is null");
                return false;
            }
            int count = detectionZone.DetectionCount;
            if (count < minCount)
            {
                return false;
            }
            if (maxCount > 0 && count > maxCount)
            {
                return false;
            }
            return true;
        }

        public string GetDescription()
        {
            string zoneName = detectionZone != null ? detectionZone.gameObject.name : "null";
            if (maxCount > 0)
            {
                return $"DetectionZone '{zoneName}' should have between {minCount} and {maxCount} detected objects";
            }
            return $"DetectionZone '{zoneName}' should have at least {minCount} detected objects";
        }
    }

    #endregion
}

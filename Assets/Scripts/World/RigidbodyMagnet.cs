using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A magnetic field that attracts or repels rigidbody objects based on their layers.
/// Similar to a magnet, objects will be drawn towards or pushed away from this object.
/// </summary>
public class RigidbodyMagnet : MonoBehaviour
{
    /// <summary>
    /// Determines whether the magnet attracts or repels objects.
    /// </summary>
    public enum MagnetMode
    {
        Attract,
        Repulse
    }

    [Header("Magnet Settings")]
    [Tooltip("Whether this magnet attracts or repels objects.")]
    [SerializeField] private MagnetMode mode = MagnetMode.Attract;

    [Tooltip("The strength of the magnetic force.")]
    [SerializeField] private float forceMagnitude = 10f;

    [Tooltip("The maximum range at which the magnet affects objects.")]
    [SerializeField] private float maxRange = 10f;

    [Tooltip("The minimum distance at which the magnet stops applying force (prevents objects from getting too close).")]
    [SerializeField] private float minDistance = 1f;

    [Header("Force Curve Settings")]
    [Tooltip("How the force scales with distance. 0 = constant force, 1 = linear falloff, 2 = inverse square (realistic physics).")]
    [Range(0f, 2f)]
    [SerializeField] private float distanceFalloff = 2f;

    [Tooltip("Whether to use the rigidbody's mass when applying force.")]
    [SerializeField] private bool useMass = true;

    [Header("Detection Settings")]
    [Tooltip("The layer mask for objects that can be affected by this magnet.")]
    [SerializeField] private LayerMask affectLayers = -1;

    [Tooltip("The maximum number of objects that can be affected at once.")]
    [SerializeField] private int maxAffectedObjects = 32;

    [Header("Advanced Settings")]
    [Tooltip("The force mode to use when applying forces.")]
    [SerializeField] private ForceMode forceMode = ForceMode.Force;

    [Tooltip("Whether to apply force continuously or only once per object.")]
    [SerializeField] private bool continuousForce = true;

    [Tooltip("How often to apply force in seconds (only used if continuousForce is false).")]
    [SerializeField] private float forceInterval = 0.1f;

    [Header("Events")]
    [Space]
    [Tooltip("Invoked when an object enters the magnetic field.")]
    public UnityEvent<Rigidbody> OnObjectEntered;

    [Tooltip("Invoked when an object exits the magnetic field.")]
    public UnityEvent<Rigidbody> OnObjectExited;

    [Tooltip("Invoked every frame while an object is being affected.")]
    public UnityEvent<Rigidbody> OnObjectAffected;

    [Header("Visualization")]
    [Tooltip("Color of the magnetic field range in the editor.")]
    [SerializeField] private Color rangeColor = new Color(0.5f, 0.5f, 1f, 0.2f);

    [Tooltip("Color of the minimum distance zone in the editor.")]
    [SerializeField] private Color minDistanceColor = new Color(1f, 0.5f, 0.5f, 0.3f);

    [Tooltip("Color of force lines to affected objects in the editor.")]
    [SerializeField] private Color forceLineColor = new Color(1f, 1f, 0f, 0.5f);

    private Collider[] overlappingColliders;
    private HashSet<Rigidbody> affectedObjects = new HashSet<Rigidbody>();
    private Dictionary<Rigidbody, float> forceTimers = new Dictionary<Rigidbody, float>();
    private float lastForceTime;

    private void Awake()
    {
        // Initialize collider array
        overlappingColliders = new Collider[maxAffectedObjects];
    }

    private void Update()
    {
        // Update affected objects
        UpdateAffectedObjects();
    }

    private void FixedUpdate()
    {
        // Apply forces in FixedUpdate for consistent physics
        if (continuousForce || Time.time - lastForceTime >= forceInterval)
        {
            ApplyMagneticForce();
            lastForceTime = Time.time;
        }
    }

    /// <summary>
    /// Updates the list of affected objects and triggers enter/exit events.
    /// </summary>
    private void UpdateAffectedObjects()
    {
        // Find all objects within range
        int count = Physics.OverlapSphereNonAlloc(transform.position, maxRange, overlappingColliders, affectLayers);

        HashSet<Rigidbody> currentObjects = new HashSet<Rigidbody>();

        for (int i = 0; i < count; i++)
        {
            Collider collider = overlappingColliders[i];
            Rigidbody rb = collider.attachedRigidbody;

            if (rb != null && !rb.isKinematic)
            {
                currentObjects.Add(rb);

                // Check if this is a new object
                if (!affectedObjects.Contains(rb))
                {
                    affectedObjects.Add(rb);
                    OnObjectEntered?.Invoke(rb);
                }
            }
        }

        // Find objects that exited the range
        List<Rigidbody> exitedObjects = new List<Rigidbody>();
        foreach (Rigidbody rb in affectedObjects)
        {
            if (!currentObjects.Contains(rb))
            {
                exitedObjects.Add(rb);
            }
        }

        // Remove exited objects and trigger exit events
        foreach (Rigidbody rb in exitedObjects)
        {
            affectedObjects.Remove(rb);
            forceTimers.Remove(rb);
            OnObjectExited?.Invoke(rb);
        }
    }

    /// <summary>
    /// Applies magnetic force to all affected objects.
    /// </summary>
    private void ApplyMagneticForce()
    {
        foreach (Rigidbody rb in affectedObjects)
        {
            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Calculate distance and direction
            Vector3 direction = transform.position - rb.position;
            float distance = direction.magnitude;

            // Check if object is within range and not too close
            if (distance > maxRange || distance < minDistance)
            {
                continue;
            }

            // Normalize direction
            direction = direction.normalized;

            // Calculate force magnitude based on distance
            float force = CalculateForce(distance, rb);

            // Apply force based on mode
            Vector3 finalForce = direction * force;
            if (mode == MagnetMode.Repulse)
            {
                finalForce = -finalForce;
            }

            // Apply force
            rb.AddForce(finalForce, forceMode);

            // Trigger affected event
            OnObjectAffected?.Invoke(rb);
        }
    }

    /// <summary>
    /// Calculates the force magnitude based on distance.
    /// </summary>
    private float CalculateForce(float distance, Rigidbody rb = null)
    {
        float normalizedDistance = (distance - minDistance) / (maxRange - minDistance);
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        float forceMultiplier = 1f;

        switch (distanceFalloff)
        {
            case 0f:
                // Constant force
                forceMultiplier = 1f;
                break;
            case 1f:
                // Linear falloff
                forceMultiplier = 1f - normalizedDistance;
                break;
            case 2f:
                // Inverse square falloff (more realistic)
                forceMultiplier = 1f / (1f + normalizedDistance * normalizedDistance * 10f);
                break;
            default:
                // Interpolated falloff
                forceMultiplier = Mathf.Lerp(1f, 1f / (1f + normalizedDistance * normalizedDistance * 10f), distanceFalloff / 2f);
                break;
        }

        float finalForce = forceMagnitude * forceMultiplier;

        // Apply mass if needed
        if (useMass && rb != null)
        {
            finalForce *= rb.mass;
        }

        return finalForce;
    }

    /// <summary>
    /// Temporarily disables the magnet for a specified duration.
    /// </summary>
    public void DisableForDuration(float duration)
    {
        StartCoroutine(DisableCoroutine(duration));
    }

    private IEnumerator DisableCoroutine(float duration)
    {
        enabled = false;
        yield return new WaitForSeconds(duration);
        enabled = true;
    }

    /// <summary>
    /// Gets the current list of affected objects.
    /// </summary>
    public IReadOnlyCollection<Rigidbody> GetAffectedObjects()
    {
        return affectedObjects;
    }

    /// <summary>
    /// Checks if a specific rigidbody is being affected.
    /// </summary>
    public bool IsAffected(Rigidbody rb)
    {
        return affectedObjects.Contains(rb);
    }

    /// <summary>
    /// Manually adds a rigidbody to be affected (bypasses range check).
    /// </summary>
    public void AddAffectedObject(Rigidbody rb)
    {
        if (rb != null && !rb.isKinematic)
        {
            affectedObjects.Add(rb);
            OnObjectEntered?.Invoke(rb);
        }
    }

    /// <summary>
    /// Manually removes a rigidbody from being affected.
    /// </summary>
    public void RemoveAffectedObject(Rigidbody rb)
    {
        if (affectedObjects.Remove(rb))
        {
            forceTimers.Remove(rb);
            OnObjectExited?.Invoke(rb);
        }
    }

    /// <summary>
    /// Clears all affected objects.
    /// </summary>
    public void ClearAffectedObjects()
    {
        foreach (Rigidbody rb in affectedObjects)
        {
            OnObjectExited?.Invoke(rb);
        }
        affectedObjects.Clear();
        forceTimers.Clear();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draws editor gizmos to visualize the magnetic field.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw max range
        Gizmos.color = rangeColor;
        Gizmos.DrawWireSphere(transform.position, maxRange);
        Gizmos.color = new Color(rangeColor.r, rangeColor.g, rangeColor.b, rangeColor.a * 0.3f);
        Gizmos.DrawSphere(transform.position, maxRange);

        // Draw min distance
        Gizmos.color = minDistanceColor;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = new Color(minDistanceColor.r, minDistanceColor.g, minDistanceColor.b, minDistanceColor.a * 0.3f);
        Gizmos.DrawSphere(transform.position, minDistance);

        // Draw force lines to affected objects
        if (Application.isPlaying)
        {
            Gizmos.color = forceLineColor;
            foreach (Rigidbody rb in affectedObjects)
            {
                if (rb != null)
                {
                    Vector3 direction = (transform.position - rb.position).normalized;
                    if (mode == MagnetMode.Repulse)
                    {
                        direction = -direction;
                    }
                    Gizmos.DrawLine(rb.position, rb.position + direction * 2f);
                }
            }
        }
    }

    /// <summary>
    /// Draws editor gizmos when the object is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        OnDrawGizmos();

        // Draw label
        string modeText = mode == MagnetMode.Attract ? "Attractor" : "Repulser";
        string label = $"{modeText}\nForce: {forceMagnitude}N\nRange: {maxRange}m";
        UnityEditor.Handles.Label(transform.position + Vector3.up * (maxRange + 1f), label);
    }
#endif

    /// <summary>
    /// Resets the component to default values when added via inspector.
    /// </summary>
    private void Reset()
    {
        mode = MagnetMode.Attract;
        forceMagnitude = 10f;
        maxRange = 10f;
        minDistance = 1f;
        distanceFalloff = 2f;
        useMass = true;
        affectLayers = -1;
        maxAffectedObjects = 32;
        forceMode = ForceMode.Force;
        continuousForce = true;
        forceInterval = 0.1f;
        rangeColor = new Color(0.5f, 0.5f, 1f, 0.2f);
        minDistanceColor = new Color(1f, 0.5f, 0.5f, 0.3f);
        forceLineColor = new Color(1f, 1f, 0f, 0.5f);
    }
}

using UnityEngine;

/// <summary>
/// A physics manipulator that pushes rigidbody objects in a specific direction
/// while they are within the overlap box area. Similar to a wind effect.
/// </summary>
public class PhysicsManipulator : MonoBehaviour
{
    [Header("Box Settings")]
    [Tooltip("The size of the overlap box area.")]
    [SerializeField] private Vector3 boxSize = new Vector3(5f, 5f, 5f);
    
    [Tooltip("The center offset of the overlap box relative to the transform position.")]
    [SerializeField] private Vector3 boxCenter = Vector3.zero;
    
    [Header("Force Settings")]
    [Tooltip("The direction in which to push objects. Normalized automatically.")]
    [SerializeField] private Vector3 forceDirection = Vector3.forward;
    
    [Tooltip("The strength of the force applied to objects.")]
    [SerializeField] private float forceMagnitude = 10f;
    
    [Tooltip("Whether to use the force direction relative to the object's rotation or world space.")]
    [SerializeField] private bool useLocalDirection = false;
    
    [Header("Detection Settings")]
    [Tooltip("The layer mask for objects to affect.")]
    [SerializeField] private LayerMask affectLayers = -1;
    
    [Tooltip("Whether to continuously apply force while objects are in the area.")]
    [SerializeField] private bool continuousForce = true;
    
    [Tooltip("How often to apply force (in seconds). Only used if continuousForce is false.")]
    [SerializeField] private float forceInterval = 0.5f;
    
    [Header("Visualization")]
    [Tooltip("Color of the gizmo box in the editor.")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.3f);
    
    [Tooltip("Color of the force direction arrow in the editor.")]
    [SerializeField] private Color arrowColor = new Color(1f, 0.5f, 0f, 0.8f);
    
    [Tooltip("Length of the force direction arrow in the editor.")]
    [SerializeField] private float arrowLength = 2f;
    
    private Collider[] overlappingColliders = new Collider[32];
    private float lastForceTime;
    
    private void Start()
    {
        // Normalize the force direction
        forceDirection = forceDirection.normalized;
    }
    
    private void Update()
    {
        if (continuousForce || Time.time - lastForceTime >= forceInterval)
        {
            ApplyForceToOverlappingObjects();
            lastForceTime = Time.time;
        }
    }
    
    /// <summary>
    /// Applies force to all rigidbody objects within the overlap box.
    /// </summary>
    private void ApplyForceToOverlappingObjects()
    {
        int count = Physics.OverlapBoxNonAlloc(
            transform.position + transform.TransformDirection(boxCenter),
            boxSize * 0.5f,
            overlappingColliders,
            transform.rotation,
            affectLayers
        );
        
        for (int i = 0; i < count; i++)
        {
            Rigidbody rb = overlappingColliders[i].attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                Vector3 direction = useLocalDirection 
                    ? transform.TransformDirection(forceDirection) 
                    : forceDirection;
                
                rb.AddForce(direction * forceMagnitude, ForceMode.Force);
            }
        }
    }
    
    /// <summary>
    /// Checks if a layer is included in the layer mask.
    /// </summary>
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }
    
    /// <summary>
    /// Draws editor gizmos to visualize the force area and direction.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw the overlap box
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCenter, boxSize);
        
        // Draw a semi-transparent box
        Color transparentColor = gizmoColor;
        transparentColor.a *= 0.3f;
        Gizmos.color = transparentColor;
        Gizmos.DrawCube(boxCenter, boxSize);
        
        // Reset matrix for arrow drawing
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw force direction arrow
        Vector3 direction = useLocalDirection 
            ? transform.TransformDirection(forceDirection) 
            : forceDirection;
        
        Vector3 start = transform.position + transform.TransformDirection(boxCenter);
        Vector3 end = start + direction * arrowLength;
        
        Gizmos.color = arrowColor;
        Gizmos.DrawLine(start, end);
        
        // Draw arrow head
        float arrowHeadSize = 0.3f;
        Vector3 arrowHeadBase = end - direction * arrowHeadSize;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right == Vector3.zero)
        {
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        }
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        Gizmos.DrawLine(end, arrowHeadBase + right * arrowHeadSize * 0.5f + up * arrowHeadSize * 0.5f);
        Gizmos.DrawLine(end, arrowHeadBase - right * arrowHeadSize * 0.5f + up * arrowHeadSize * 0.5f);
        Gizmos.DrawLine(end, arrowHeadBase + right * arrowHeadSize * 0.5f - up * arrowHeadSize * 0.5f);
        Gizmos.DrawLine(end, arrowHeadBase - right * arrowHeadSize * 0.5f - up * arrowHeadSize * 0.5f);
    }
    
    /// <summary>
    /// Draws editor gizmos when the object is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw additional visualization when selected
        OnDrawGizmos();
        
        // Draw force magnitude text
        Vector3 direction = useLocalDirection 
            ? transform.TransformDirection(forceDirection) 
            : forceDirection;
        
        Vector3 labelPos = transform.position + transform.TransformDirection(boxCenter) + direction * (arrowLength + 0.5f);
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"Force: {forceMagnitude}N");
#endif
    }
    
    /// <summary>
    /// Resets the component to default values when added via inspector.
    /// </summary>
    private void Reset()
    {
        boxSize = new Vector3(5f, 5f, 5f);
        boxCenter = Vector3.zero;
        forceDirection = Vector3.forward;
        forceMagnitude = 10f;
        useLocalDirection = false;
        affectLayers = -1;
        continuousForce = true;
        forceInterval = 0.5f;
        gizmoColor = new Color(0.2f, 0.8f, 1f, 0.3f);
        arrowColor = new Color(1f, 0.5f, 0f, 0.8f);
        arrowLength = 2f;
    }
}

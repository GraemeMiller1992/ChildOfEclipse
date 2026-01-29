using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A teleporter that moves objects from an entry overlap box to a destination location.
/// Objects are teleported when they enter the entry area.
/// </summary>
public class Teleporter : MonoBehaviour
{
    [Header("Entry Box Settings")]
    [Tooltip("The size of the entry overlap box area.")]
    [SerializeField] private Vector3 entryBoxSize = new Vector3(3f, 3f, 3f);
    
    [Tooltip("The center offset of the entry overlap box relative to the transform position.")]
    [SerializeField] private Vector3 entryBoxCenter = Vector3.zero;
    
    [Header("Destination Settings")]
    [Tooltip("The destination position where objects will be teleported.")]
    [SerializeField] private Transform destination;
    
    [Tooltip("The offset from the destination position where objects will be placed.")]
    [SerializeField] private Vector3 destinationOffset = Vector3.zero;
    
    [Tooltip("Whether to preserve the object's rotation upon teleportation.")]
    [SerializeField] private bool preserveRotation = true;
    
    [Tooltip("Whether to preserve the object's velocity upon teleportation.")]
    [SerializeField] private bool preserveVelocity = false;
    
    [Tooltip("Whether to preserve the object's angular velocity upon teleportation.")]
    [SerializeField] private bool preserveAngularVelocity = false;
    
    [Header("Detection Settings")]
    [Tooltip("The layer mask for objects that can be teleported.")]
    [SerializeField] private LayerMask teleportLayers = -1;
    
    [Tooltip("The cooldown time in seconds before an object can be teleported again.")]
    [SerializeField] private float teleportCooldown = 0.5f;
    
    [Tooltip("Whether to teleport the object only once per cooldown, or continuously while in the box.")]
    [SerializeField] private bool teleportOncePerEntry = true;
    
    [Header("Visualization")]
    [Tooltip("Color of the entry box gizmo in the editor.")]
    [SerializeField] private Color entryBoxColor = new Color(0f, 1f, 0.5f, 0.3f);
    
    [Tooltip("Color of the destination gizmo in the editor.")]
    [SerializeField] private Color destinationColor = new Color(1f, 0f, 0.5f, 0.5f);
    
    [Tooltip("Color of the connection line between entry and destination.")]
    [SerializeField] private Color connectionLineColor = new Color(1f, 1f, 1f, 0.3f);
    
    private Collider[] overlappingColliders = new Collider[32];
    private Dictionary<GameObject, float> teleportCooldowns = new Dictionary<GameObject, float>();
    
    private void Update()
    {
        // Update cooldowns
        UpdateCooldowns();
        
        // Check for objects in the entry box
        CheckForTeleportableObjects();
    }
    
    /// <summary>
    /// Updates the cooldown timers for all recently teleported objects.
    /// </summary>
    private void UpdateCooldowns()
    {
        List<GameObject> keysToRemove = new List<GameObject>();
        
        foreach (var kvp in teleportCooldowns)
        {
            if (Time.time >= kvp.Value)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (GameObject key in keysToRemove)
        {
            teleportCooldowns.Remove(key);
        }
    }
    
    /// <summary>
    /// Checks for objects within the entry box and teleports them if eligible.
    /// </summary>
    private void CheckForTeleportableObjects()
    {
        if (destination == null) return;
        
        int count = Physics.OverlapBoxNonAlloc(
            transform.position + transform.TransformDirection(entryBoxCenter),
            entryBoxSize * 0.5f,
            overlappingColliders,
            transform.rotation,
            teleportLayers
        );
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = overlappingColliders[i].gameObject;
            
            // Skip if object is on cooldown
            if (teleportOncePerEntry && teleportCooldowns.ContainsKey(obj))
            {
                continue;
            }
            
            // Teleport the object
            TeleportObject(obj);
            
            // Add to cooldown if needed
            if (teleportOncePerEntry)
            {
                teleportCooldowns[obj] = Time.time + teleportCooldown;
            }
        }
    }
    
    /// <summary>
    /// Teleports an object to the destination.
    /// </summary>
    private void TeleportObject(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        
        // Calculate destination position
        Vector3 targetPosition = destination.position + destinationOffset;
        
        // Store velocity before teleportation if needed
        Vector3 velocity = Vector3.zero;
        Vector3 angularVelocity = Vector3.zero;
        
        if (rb != null)
        {
            velocity = rb.linearVelocity;
            angularVelocity = rb.angularVelocity;
        }
        
        // Teleport the object
        obj.transform.position = targetPosition;
        
        // Handle rotation
        if (!preserveRotation)
        {
            obj.transform.rotation = destination.rotation;
        }
        
        // Handle rigidbody velocity
        if (rb != null)
        {
            if (preserveVelocity)
            {
                rb.linearVelocity = velocity;
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }
            
            if (preserveAngularVelocity)
            {
                rb.angularVelocity = angularVelocity;
            }
            else
            {
                rb.angularVelocity = Vector3.zero;
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
    /// Draws editor gizmos to visualize the entry box, destination, and connection.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw entry box
        Gizmos.color = entryBoxColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(entryBoxCenter, entryBoxSize);
        
        // Draw semi-transparent entry box
        Color transparentEntryColor = entryBoxColor;
        transparentEntryColor.a *= 0.3f;
        Gizmos.color = transparentEntryColor;
        Gizmos.DrawCube(entryBoxCenter, entryBoxSize);
        
        // Reset matrix for other gizmos
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw destination if set
        if (destination != null)
        {
            Vector3 entryPosition = transform.position + transform.TransformDirection(entryBoxCenter);
            Vector3 destinationPosition = destination.position + destinationOffset;
            
            // Draw connection line
            Gizmos.color = connectionLineColor;
            Gizmos.DrawLine(entryPosition, destinationPosition);
            
            // Draw destination marker
            Gizmos.color = destinationColor;
            Gizmos.DrawWireSphere(destinationPosition, 0.5f);
            
            // Draw semi-transparent destination sphere
            Color transparentDestColor = destinationColor;
            transparentDestColor.a *= 0.3f;
            Gizmos.color = transparentDestColor;
            Gizmos.DrawSphere(destinationPosition, 0.5f);
            
            // Draw destination arrow
            Vector3 direction = (destinationPosition - entryPosition).normalized;
            Vector3 arrowStart = destinationPosition - direction * 0.5f;
            Vector3 arrowEnd = destinationPosition + direction * 0.5f;
            
            Gizmos.color = destinationColor;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            
            // Draw arrow head
            float arrowHeadSize = 0.2f;
            Vector3 arrowHeadBase = arrowEnd - direction * arrowHeadSize;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right == Vector3.zero)
            {
                right = Vector3.Cross(direction, Vector3.forward).normalized;
            }
            Vector3 up = Vector3.Cross(right, direction).normalized;
            
            Gizmos.DrawLine(arrowEnd, arrowHeadBase + right * arrowHeadSize * 0.5f + up * arrowHeadSize * 0.5f);
            Gizmos.DrawLine(arrowEnd, arrowHeadBase - right * arrowHeadSize * 0.5f + up * arrowHeadSize * 0.5f);
            Gizmos.DrawLine(arrowEnd, arrowHeadBase + right * arrowHeadSize * 0.5f - up * arrowHeadSize * 0.5f);
            Gizmos.DrawLine(arrowEnd, arrowHeadBase - right * arrowHeadSize * 0.5f - up * arrowHeadSize * 0.5f);
        }
    }
    
    /// <summary>
    /// Draws editor gizmos when the object is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw additional visualization when selected
        OnDrawGizmos();
        
        // Draw labels
        Vector3 entryPosition = transform.position + transform.TransformDirection(entryBoxCenter);
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(entryPosition + Vector3.up * 2f, "Entry");
        
        if (destination != null)
        {
            Vector3 destinationPosition = destination.position + destinationOffset;
            UnityEditor.Handles.Label(destinationPosition + Vector3.up * 1f, "Destination");
        }
#endif
    }
    
    /// <summary>
    /// Resets the component to default values when added via inspector.
    /// </summary>
    private void Reset()
    {
        entryBoxSize = new Vector3(3f, 3f, 3f);
        entryBoxCenter = Vector3.zero;
        destination = null;
        destinationOffset = Vector3.zero;
        preserveRotation = true;
        preserveVelocity = false;
        preserveAngularVelocity = false;
        teleportLayers = -1;
        teleportCooldown = 0.5f;
        teleportOncePerEntry = true;
        entryBoxColor = new Color(0f, 1f, 0.5f, 0.3f);
        destinationColor = new Color(1f, 0f, 0.5f, 0.5f);
        connectionLineColor = new Color(1f, 1f, 1f, 0.3f);
    }
}

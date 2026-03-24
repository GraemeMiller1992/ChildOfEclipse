using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Robust controller for the PlayerWallCutout effect.
/// Detects occluding objects between camera and player and swaps their materials.
/// </summary>
[ExecuteAlways]
public class PlayerWallCutoutController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Offset the cutout position from the player's transform position")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("The player's collider used to check if player is behind a wall")]
    public Collider playerCollider;

    [Tooltip("Layer mask for objects that can be cut out")]
    public LayerMask layerMask = -1;

    [Header("Tag Filtering")]
    [Tooltip("Only objects with these tags will be cut out (leave empty to allow all)")]
    public List<string> allowedTags = new List<string>();

    [Tooltip("Enable tag filtering")]
    public bool useTagFilter = false;

    [Tooltip("The cutout shader material template. Its properties will be updated from original materials.")]
    public Material cutoutMaterialTemplate;

    [Tooltip("Radius of the spherecast used to detect occluding objects")]
    public float detectionRadius = 0.5f;

    [Tooltip("Max number of occluding objects to track")]
    public int maxOccluders = 10;

    private class OccludedRenderer
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] cutoutMaterials;
        public float lastSeenTime;
      
    }

    private Dictionary<Renderer, OccludedRenderer> _occludedRenderers = new Dictionary<Renderer, OccludedRenderer>();
    private List<Renderer> _toRemove = new List<Renderer>();

    private int _playerPositionID;
    private int _playerScreenPosID;
    private int _playerRadiusID;

    private Camera _mainCamera;

    private void Awake()
    {
        _playerPositionID = Shader.PropertyToID("_PlayerPosition");
        _playerScreenPosID = Shader.PropertyToID("_PlayerScreenPos");
        _playerRadiusID = Shader.PropertyToID("_PlayerRadius");

        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null || cutoutMaterialTemplate == null)
            return;

        Vector3 playerPos = transform.position + positionOffset;
        Vector3 cameraPos = _mainCamera.transform.position;
        Vector3 dir = (playerPos - cameraPos).normalized;
        float dist = Vector3.Distance(cameraPos, playerPos);

        // Calculate screen space position for the shader
        Vector3 screenPos = _mainCamera.WorldToViewportPoint(playerPos);

        // Update Global Shader Properties
        Shader.SetGlobalVector(_playerPositionID, playerPos);
        Shader.SetGlobalVector(_playerScreenPosID, new Vector4(screenPos.x, screenPos.y, screenPos.z, 1.0f));
        Shader.SetGlobalFloat(_playerRadiusID, 1.0f);

        // Detect Occluders
        RaycastHit[] hits = Physics.SphereCastAll(cameraPos, detectionRadius, dir, dist, layerMask);

        float currentTime = Time.time;

        foreach (var hit in hits)
        {
            Renderer r = hit.collider.GetComponent<Renderer>();
            if (r == null) continue;

            if (!PassesTagFilter(hit.collider.gameObject))
                continue;

            // Check if player is actually behind the wall
            if (!IsPlayerBehindWall(hit.collider, playerPos, cameraPos))
                continue;

            if (!_occludedRenderers.ContainsKey(r))
            {
                SwapToCutout(r);
            }

            if (_occludedRenderers.TryGetValue(r, out OccludedRenderer occluded))
            {
                occluded.lastSeenTime = currentTime;
            }
        }

        // Clean up occluders no longer hit
        _toRemove.Clear();
        foreach (var pair in _occludedRenderers)
        {
            if (currentTime - pair.Value.lastSeenTime > 0.1f)
            {
                _toRemove.Add(pair.Key);
            }
        }

        foreach (var r in _toRemove)
        {
            SwapBack(r);
        }
    }
    private bool PassesTagFilter(GameObject obj)
    {
        if (!useTagFilter) return true;

        if (allowedTags == null || allowedTags.Count == 0)
            return true;

        foreach (var tag in allowedTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            if (obj.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void SwapToCutout(Renderer r)
    {
        if (r == null || cutoutMaterialTemplate == null) return;

        OccludedRenderer occluded = new OccludedRenderer
        {
            renderer = r,
            originalMaterials = r.sharedMaterials,
            cutoutMaterials = new Material[r.sharedMaterials.Length],
            lastSeenTime = Time.time
        };

        for (int i = 0; i < occluded.originalMaterials.Length; i++)
        {
            Material orig = occluded.originalMaterials[i];
            if (orig == null) continue;

            Material cutout = new Material(cutoutMaterialTemplate);
            cutout.name = orig.name + " (Cutout Instance)";

            // Copy properties from original to cutout
            // Base Color
            if (orig.HasProperty("_BaseColor")) cutout.SetColor("_BaseColor", orig.GetColor("_BaseColor"));
            else if (orig.HasProperty("_Color")) cutout.SetColor("_BaseColor", orig.GetColor("_Color"));

            // Base Map
            if (orig.HasProperty("_BaseMap")) cutout.SetTexture("_BaseMap", orig.GetTexture("_BaseMap"));
            else if (orig.HasProperty("_MainTex")) cutout.SetTexture("_BaseMap", orig.GetTexture("_MainTex"));

            // Metallic
            if (orig.HasProperty("_Metallic")) cutout.SetFloat("_Metallic", orig.GetFloat("_Metallic"));
            
            // Smoothness
            if (orig.HasProperty("_Smoothness")) cutout.SetFloat("_Smoothness", orig.GetFloat("_Smoothness"));
            else if (orig.HasProperty("_Glossiness")) cutout.SetFloat("_Smoothness", orig.GetFloat("_Glossiness"));

            // Normal Map
            if (orig.HasProperty("_BumpMap")) cutout.SetTexture("_BumpMap", orig.GetTexture("_BumpMap"));
            else if (orig.HasProperty("_NormalMap")) cutout.SetTexture("_BumpMap", orig.GetTexture("_NormalMap"));

            // Normal Scale
            if (orig.HasProperty("_BumpScale")) cutout.SetFloat("_BumpScale", orig.GetFloat("_BumpScale"));
            else if (orig.HasProperty("_NormalScale")) cutout.SetFloat("_BumpScale", orig.GetFloat("_NormalScale"));

            occluded.cutoutMaterials[i] = cutout;
        }

        r.materials = occluded.cutoutMaterials;
        _occludedRenderers.Add(r, occluded);
    }

    private void SwapBack(Renderer r)
    {
        if (_occludedRenderers.TryGetValue(r, out OccludedRenderer occluded))
        {
            r.materials = occluded.originalMaterials;
            
            // Clean up instances
            foreach (var m in occluded.cutoutMaterials)
            {
                if (Application.isPlaying) Destroy(m);
                else DestroyImmediate(m);
            }
            
            _occludedRenderers.Remove(r);
        }
    }

    private bool IsPlayerBehindWall(Collider wallCollider, Vector3 playerPos, Vector3 cameraPos)
    {
        if (playerCollider == null || wallCollider == null)
            return true;

        // Get the closest point on the wall to the player
        Vector3 closestPoint = wallCollider.ClosestPoint(playerPos);
        
        // Calculate direction from closest point to player
        Vector3 wallToPlayer = (playerPos - closestPoint).normalized;
        
        // Calculate direction from closest point to camera
        Vector3 wallToCamera = (cameraPos - closestPoint).normalized;
        
        // Check if player is on the opposite side of the wall from the camera
        // If dot product is negative, they are on opposite sides
        return Vector3.Dot(wallToPlayer, wallToCamera) < 0;
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(_playerRadiusID, 0f);

        // Restore all materials
        List<Renderer> active = new List<Renderer>(_occludedRenderers.Keys);
        foreach (var r in active)
        {
            SwapBack(r);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Vector3 playerPos = transform.position + positionOffset;
        Vector3 cameraPos = _mainCamera.transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cameraPos, playerPos);
        Gizmos.DrawWireSphere(playerPos, detectionRadius);
        Gizmos.DrawWireSphere(cameraPos, detectionRadius);
    }
}

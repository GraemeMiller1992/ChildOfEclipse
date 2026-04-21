using System.Collections.Generic;
using UnityEngine;
using World;

[RequireComponent(typeof(SolarState))]
public class PortalPassThroughController : MonoBehaviour
{
    public enum PortalAxis { X, Y, Z }

    public enum ContactPositionMode { TransformPosition, ColliderCenter }

    public enum DetectionMode { Sphere, Box }

    [Header("Portal Axis")]
    [Tooltip("Which local axis is the portal's normal direction.")]
    public PortalAxis portalAxis = PortalAxis.Y;

    [Header("Detection")]
    [Tooltip("Shape used to detect nearby objects")]
    public DetectionMode detectionMode = DetectionMode.Sphere;

    [Tooltip("Radius of the OverlapSphere used to detect nearby objects")]
    public float detectionRadius = 2.0f;

    [Tooltip("Half-extents of the OverlapBox (used only in Box mode)")]
    public Vector3 detectionBoxSize = new Vector3(2f, 2f, 1f);

    [Tooltip("Local offset applied to the OverlapBox center (used only in Box mode)")]
    public Vector3 detectionBoxOffset = Vector3.zero;

    [Tooltip("Max number of simultaneous contact points sent to the shader")]
    public int maxContacts = 8;

    [Tooltip("Layer mask for overlap detection")]
    public LayerMask detectionLayerMask = -1;

    [Header("Contact Position")]
    [Tooltip("Where to place the contact point on the entering object.")]
    public ContactPositionMode contactPositionMode = ContactPositionMode.TransformPosition;

    [Header("Force Cutout Shape")]
    [Tooltip("When enabled, all contacts use the shape/size below instead of PortalPassThroughSize values.")]
    public bool forceCutoutShape = false;

    [Tooltip("The shape to use when forceCutoutShape is enabled.")]
    public PortalCutoutShape forcedShape = PortalCutoutShape.Sphere;

    [Tooltip("Radius used when forceCutoutShape is enabled.")]
    public float forcedRadius = 0.5f;

    [Tooltip("Height used when forceCutoutShape is enabled (Capsule).")]
    public float forcedHeight = 2.0f;

    [Tooltip("Size used when forceCutoutShape is enabled (Box).")]
    public Vector3 forcedBoxSize = new Vector3(1f, 1f, 1f);

    [Header("Animation")]
    [Tooltip("How fast the cutout opens (units/sec)")]
    public float openSpeed = 4.0f;

    [Tooltip("How fast the cutout closes after the object exits (units/sec)")]
    public float closeSpeed = 2.5f;

    [Tooltip("Extra delay before closing begins")]
    public float closeDelay = 0.15f;

    private class Contact
    {
        public int instanceID;
        public Vector3 worldPos;
        public float radius;
        public int shape;
        public float height;
        public Vector3 boxSize;
        public float transition;
        public float targetTransition;
        public float lastSeenTime;
    }

    private SolarState _solarState;
    private Contact[] _contacts;
    private Vector3[] _positionData;
    private Vector4[] _contactData;
    private List<Vector4> _positionDataV4;
    private List<Vector4> _contactDataV4;

    private Renderer _portalRenderer;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _solarState = GetComponent<SolarState>();

        _portalRenderer = GetComponentInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        _contacts = new Contact[maxContacts];
        _positionData = new Vector3[maxContacts];
        _contactData = new Vector4[maxContacts];
        _positionDataV4 = new List<Vector4>(maxContacts);
        _contactDataV4 = new List<Vector4>(maxContacts);

        for (int i = 0; i < maxContacts; i++)
        {
            _contacts[i] = new Contact();
        }
    }

    private void OnValidate()
    {
        maxContacts = Mathf.Clamp(maxContacts, 1, 8);
    }

    private void Update()
    {
        DetectOverlaps();

        float dt = Time.deltaTime;
        float now = Time.time;
        int activeCount = 0;

        Vector3 portalNormal = GetPortalNormalWorld();

        for (int i = 0; i < maxContacts; i++)
        {
            Contact c = _contacts[i];

            if (c.instanceID == 0)
            {
                _positionData[i] = Vector3.zero;
                _contactData[i] = Vector4.zero;
                continue;
            }

            float speed = c.targetTransition > 0.5f ? openSpeed : closeSpeed;

            if (c.targetTransition < 0.5f && (now - c.lastSeenTime) < closeDelay)
            {
                speed = 0.0f;
            }

            c.transition = Mathf.MoveTowards(c.transition, c.targetTransition, speed * dt);

            if (c.transition < 0.001f && c.targetTransition < 0.5f)
            {
                c.instanceID = 0;
                c.transition = 0.0f;
                c.targetTransition = 0.0f;
                _contacts[i] = c;
                _positionData[i] = Vector3.zero;
                _contactData[i] = Vector4.zero;
                continue;
            }

            _contacts[i] = c;

            float eased = SmoothStep(c.transition);
            _positionData[i] = c.worldPos;
            float boxAspect = c.radius > 0.001f ? c.boxSize.x / c.boxSize.y : 1f;
            _contactData[i] = new Vector4(eased, c.radius, (float)c.shape, c.shape == 1 ? c.height : boxAspect);
            activeCount = i + 1;
        }

        if (_portalRenderer != null)
        {
            _propBlock.Clear();
            _propBlock.SetVectorArray("_ContactPositions", ToVector4List(_positionData));
            _propBlock.SetVectorArray("_ContactData", _contactData);
            _propBlock.SetVector("_PortalNormal", portalNormal);
            _propBlock.SetInt("_ContactCount", activeCount);
            _portalRenderer.SetPropertyBlock(_propBlock);
        }
    }

    private Collider[] QueryOverlaps()
    {
        return detectionMode switch
        {
            DetectionMode.Box => Physics.OverlapBox(transform.TransformPoint(detectionBoxOffset), detectionBoxSize, transform.rotation, detectionLayerMask),
            _ => Physics.OverlapSphere(transform.position, detectionRadius, detectionLayerMask),
        };
    }

    private void DetectOverlaps()
    {
        Collider[] hits = QueryOverlaps();
        float now = Time.time;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit.gameObject == gameObject) continue;

            SolarState otherState = hit.GetComponentInParent<SolarState>();
            if (otherState == null) continue;
            if (otherState.CurrentState == _solarState.CurrentState) continue;
            if (hit.GetComponentInParent<PortalPassThroughSize>() == null) continue;

            int id = hit.transform.root.GetInstanceID();
            Vector3 contactPos = GetContactPosition(hit);

            int slot = FindSlot(id);
            if (slot >= 0)
            {
                _contacts[slot].worldPos = contactPos;
                _contacts[slot].targetTransition = 1.0f;
                _contacts[slot].lastSeenTime = now;
            }
            else
            {
                slot = FindOrCreateSlot(id, hit);
                if (slot >= 0)
                {
                    _contacts[slot].worldPos = contactPos;
                    _contacts[slot].targetTransition = 1.0f;
                    _contacts[slot].lastSeenTime = now;
                }
            }
        }

        for (int i = 0; i < maxContacts; i++)
        {
            if (_contacts[i].instanceID == 0) continue;
            if (now - _contacts[i].lastSeenTime > 0.05f)
            {
                _contacts[i].targetTransition = 0.0f;
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < maxContacts; i++)
        {
            _contacts[i] = new Contact();
            _positionData[i] = Vector3.zero;
            _contactData[i] = Vector4.zero;
        }

        if (_portalRenderer != null)
        {
            _propBlock.Clear();
            _propBlock.SetVectorArray("_ContactPositions", ToVector4List(_positionData));
            _propBlock.SetVectorArray("_ContactData", _contactData);
            _propBlock.SetVector("_PortalNormal", Vector3.zero);
            _propBlock.SetInt("_ContactCount", 0);
            _portalRenderer.SetPropertyBlock(_propBlock);
        }
    }

    private Vector3 GetPortalNormalWorld()
    {
        return portalAxis switch
        {
            PortalAxis.X => transform.right,
            PortalAxis.Y => transform.up,
            PortalAxis.Z => transform.forward,
            _ => transform.forward,
        };
    }

    private Vector3 GetContactPosition(Collider hit)
    {
        if (contactPositionMode == ContactPositionMode.ColliderCenter)
        {
            if (hit is BoxCollider box)
                return box.transform.TransformPoint(box.center);
            if (hit is SphereCollider sphere)
                return sphere.transform.TransformPoint(sphere.center);
            if (hit is CapsuleCollider capsule)
                return capsule.transform.TransformPoint(capsule.center);
        }

        return hit.transform.position;
    }

    private int FindSlot(int instanceID)
    {
        for (int i = 0; i < maxContacts; i++)
        {
            if (_contacts[i].instanceID == instanceID)
                return i;
        }
        return -1;
    }

    private int FindOrCreateSlot(int instanceID, Collider other)
    {
        int slot = FindSlot(instanceID);
        if (slot >= 0) return slot;

        for (int i = 0; i < maxContacts; i++)
        {
            if (_contacts[i].instanceID == 0)
            {
                _contacts[i] = new Contact
                {
                    instanceID = instanceID,
                    worldPos = GetContactPosition(other),
                    transition = 0.0f,
                    targetTransition = 0.0f,
                    lastSeenTime = Time.time,
                };

                ApplySize(other, ref _contacts[i]);
                return i;
            }
        }
        return -1;
    }

    private void ApplySize(Collider other, ref Contact contact)
    {
        if (forceCutoutShape)
        {
            contact.shape = (int)forcedShape;
            contact.radius = forcedRadius;
            contact.height = forcedHeight;
            contact.boxSize = forcedBoxSize;
            return;
        }

        PortalPassThroughSize sizeOverride = other.GetComponentInParent<PortalPassThroughSize>();
        if (sizeOverride != null)
        {
            contact.shape = (int)sizeOverride.shape;
            contact.radius = sizeOverride.radius;
            contact.height = sizeOverride.height;
            contact.boxSize = sizeOverride.size;
            return;
        }

        Renderer renderer = other.GetComponentInParent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            contact.shape = 2;
            contact.radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
            contact.height = bounds.size.y;
            contact.boxSize = bounds.size;
            return;
        }

        if (other is SphereCollider sphere)
        {
            contact.shape = 0;
            contact.radius = sphere.radius;
            contact.height = sphere.radius * 2f;
            contact.boxSize = Vector3.one * sphere.radius * 2f;
            return;
        }

        if (other is CapsuleCollider capsule)
        {
            contact.shape = 1;
            contact.radius = capsule.radius;
            contact.height = capsule.height;
            contact.boxSize = new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f);
            return;
        }

        if (other is BoxCollider box)
        {
            contact.shape = 2;
            contact.radius = Mathf.Max(box.size.x, box.size.y, box.size.z) * 0.5f;
            contact.height = box.size.y;
            contact.boxSize = box.size;
            return;
        }

        if (other is CharacterController cc)
        {
            contact.shape = 1;
            contact.radius = cc.radius;
            contact.height = cc.height;
            contact.boxSize = new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f);
            return;
        }

        contact.shape = 0;
        contact.radius = 0.5f;
        contact.height = 1.0f;
        contact.boxSize = Vector3.one;
    }

    private List<Vector4> ToVector4List(Vector3[] arr)
    {
        _positionDataV4.Clear();
        for (int i = 0; i < arr.Length; i++)
            _positionDataV4.Add(arr[i]);
        return _positionDataV4;
    }

    private static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3.0f - 2.0f * t);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);

        switch (detectionMode)
        {
            case DetectionMode.Box:
                Gizmos.DrawWireCube(detectionBoxOffset, detectionBoxSize * 2f);
                break;
            default:
                Gizmos.DrawWireSphere(Vector3.zero, detectionRadius);
                break;
        }

        Gizmos.color = Color.cyan;
        Vector3 normal = portalAxis switch
        {
            PortalAxis.X => Vector3.right,
            PortalAxis.Y => Vector3.up,
            PortalAxis.Z => Vector3.forward,
            _ => Vector3.forward,
        };
        Gizmos.DrawRay(Vector3.zero, normal * detectionRadius);
        Gizmos.matrix = Matrix4x4.identity;
    }
}

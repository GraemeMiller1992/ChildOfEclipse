using UnityEngine;

namespace World
{
    public enum PortalCutoutShape
    {
        Sphere,
        Capsule,
        Box
    }

    public class PortalPassThroughSize : MonoBehaviour
    {
        [Tooltip("The shape of the portal cutout.")]
        public PortalCutoutShape shape = PortalCutoutShape.Sphere;

        [Tooltip("Radius for Sphere/Capsule shapes.")]
        public float radius = 0.5f;

        [Tooltip("Height for Capsule shape (total, not half).")]
        public float height = 2.0f;

        [Tooltip("Size for Box shape.")]
        public Vector3 size = new Vector3(1f, 1f, 1f);

        public float GetEffectiveRadius()
        {
            switch (shape)
            {
                case PortalCutoutShape.Sphere:
                    return radius;
                case PortalCutoutShape.Capsule:
                    return Mathf.Max(radius, height * 0.5f);
                case PortalCutoutShape.Box:
                    return Mathf.Max(size.x, size.y, size.z) * 0.5f;
                default:
                    return radius;
            }
        }
    }
}

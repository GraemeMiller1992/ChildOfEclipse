using UnityEngine;

public class GroundProjectedFX : MonoBehaviour
{
    public Transform target;
    public Transform projectedFX;
    public float rayHeight = 5f;
    public float maxDistance = 20f;
    public float offset = 0.02f;
    public LayerMask groundMask = -1;
    public ParticleSystem ps;

    void LateUpdate()
    {
        Vector3 origin = target.position + Vector3.up * rayHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, groundMask))
        {
            if (!projectedFX.gameObject.activeSelf)
            {
                projectedFX.gameObject.SetActive(true);

                // instant start (no warmup delay)
                ps.Simulate(0f, true, true, true);
                ps.Play(true);
            }

            projectedFX.position = hit.point + hit.normal * offset;
            projectedFX.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            projectedFX.gameObject.SetActive(false);
        }
    }
}

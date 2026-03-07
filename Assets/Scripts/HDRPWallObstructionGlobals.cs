using UnityEngine;

[ExecuteAlways]
public class HDRPWallObstructionGlobals : MonoBehaviour
{
    [SerializeField] Transform playerTarget;
    [SerializeField] Camera targetCamera;
    [SerializeField] Vector3 playerOffset = new Vector3(0f, 1f, 0f);

    static readonly int PlayerWSId = Shader.PropertyToID("_ObstructionPlayerWS");
    static readonly int CameraWSId = Shader.PropertyToID("_ObstructionCameraWS");

    void LateUpdate()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera || !playerTarget) return;

        Shader.SetGlobalVector(PlayerWSId, playerTarget.position + playerOffset);
        Shader.SetGlobalVector(CameraWSId, targetCamera.transform.position);
    }
}
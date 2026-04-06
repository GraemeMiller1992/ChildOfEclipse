using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        // Automatically find the main camera's transform
        camTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Option A: Point the object's forward vector at the camera
        // Note: For Sprites or UI, this might make the object appear backwards.
        transform.LookAt(camTransform);

        // Option B: Match the camera's exact rotation (Commonly used for 2D sprites)
        // transform.rotation = camTransform.rotation;
    }
}


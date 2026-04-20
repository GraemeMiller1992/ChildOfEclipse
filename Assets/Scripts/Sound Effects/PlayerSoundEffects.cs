using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(AudioSource))]
public class FootstepAndJump : MonoBehaviour
{
    public AudioClip[] footstepClips;
    public AudioClip jumpClip;

    public float moveThreshold = 0.02f;
    public float stepInterval = 0.4f;
    public float volume = 1f;
    public float jumpPauseDuration = 1.3f;

    [Range(0.5f, 1.5f)] public float minPitch = 0.95f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.05f;

    private AudioSource source;
    private Vector3 lastPosition;
    private float stepTimer;
    private int lastClipIndex = -1;

    private float jumpTimer;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = volume;

        lastPosition = transform.position;
    }

    void Update()
    {
        HandleJumpInput();
        HandleFootsteps();

        if (jumpTimer > 0f)
            jumpTimer -= Time.deltaTime;

        lastPosition = transform.position;
    }

    void HandleJumpInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Space))
#endif
        {
            PlayJump();
        }
    }

    void HandleFootsteps()
    {
        if (jumpTimer > 0f) return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;

        bool isMoving = delta.magnitude > moveThreshold;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;

        lastClipIndex = index;

        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(footstepClips[index], volume);
    }

    void PlayJump()
    {
        if (jumpClip == null) return;

        jumpTimer = jumpPauseDuration;

        source.pitch = 1f;
        source.PlayOneShot(jumpClip, volume);
    }
}
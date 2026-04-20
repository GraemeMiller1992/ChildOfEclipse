using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FootstepAndJump : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepSource;
    public AudioSource jumpSource;

    [Header("Clips")]
    public AudioClip[] footstepClips;
    public AudioClip jumpClip;

    [Header("Footsteps")]
    public float moveThreshold = 0.02f;
    public float stepInterval = 0.4f;
    public float footstepVolume = 1f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.95f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.05f;

    [Header("Jump")]
    public float jumpVolume = 1f;
    public float jumpPauseDuration = 1.3f;
    public float jumpCooldown = 1f;

    private Vector3 lastPosition;
    private float stepTimer;
    private float jumpTimer;
    private float jumpCooldownTimer;
    private int lastClipIndex = -1;

    void Awake()
    {
        if (footstepSource != null)
        {
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 1f;
        }

        if (jumpSource != null)
        {
            jumpSource.playOnAwake = false;
            jumpSource.loop = false;
            jumpSource.spatialBlend = 0f;
            jumpSource.volume = 1f;
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        HandleJumpInput();
        HandleFootsteps();

        if (jumpTimer > 0f)
            jumpTimer -= Time.deltaTime;

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;

        lastPosition = transform.position;
    }

    void HandleJumpInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Space))
#endif
        {
            if (jumpCooldownTimer <= 0f)
            {
                PlayJump();
                jumpCooldownTimer = jumpCooldown;
            }
        }
    }

    void HandleFootsteps()
    {
        if (footstepSource == null) return;
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
        if (footstepSource == null) return;
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);

        if (footstepClips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;

        lastClipIndex = index;

        footstepSource.pitch = Random.Range(minPitch, maxPitch);
        footstepSource.PlayOneShot(footstepClips[index], footstepVolume);
    }

    void PlayJump()
    {
        if (jumpSource == null) return;
        if (jumpClip == null) return;

        jumpTimer = jumpPauseDuration;
        stepTimer = 0f;

        footstepSource.Stop();

        jumpSource.pitch = 1f;
        jumpSource.PlayOneShot(jumpClip, jumpVolume);
    }
}
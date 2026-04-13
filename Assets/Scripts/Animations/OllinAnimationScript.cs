using UnityEngine;
using World;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private SolarState playerSolarState;

    [Header("Movement Detection")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerSolarState = GetComponent<SolarState>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (playerSolarState == null)
            playerSolarState = GetComponent<SolarState>();
    }

    private void OnEnable()
    {
        if (playerSolarState != null)
            playerSolarState.OnSolarStateChanged += OnPlayerSolarStateChanged;
    }

    private void OnDisable()
    {
        if (playerSolarState != null)
            playerSolarState.OnSolarStateChanged -= OnPlayerSolarStateChanged;
    }

    private void Update()
    {
        if (animator == null) return;

        bool isGrounded = CheckGrounded();
        bool isMoving = CheckMoving();

        animator.SetFloat("Speed", isMoving ? 1f : 0f);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", !isGrounded);
    }

    private bool CheckMoving()
    {
        if (rb != null)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            return horizontalVelocity.magnitude > moveThreshold;
        }

        return false;
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
        {
            Debug.LogWarning("GroundCheck not assigned on PlayerAnimationController.");
            return true;
        }

        Collider[] hits = Physics.OverlapSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void OnPlayerSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
    {
        if (oldState != newState)
        {
            PlayCastAnimation();
        }
    }

    public void PlayCastAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Cast");
        animator.SetTrigger("Cast");

        Debug.Log("Cast animation triggered");
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
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

    private bool isCasting;

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
        {
            playerSolarState.OnSolarStateChanged += OnPlayerSolarStateChanged;
        }
    }

    private void OnDisable()
    {
        if (playerSolarState != null)
        {
            playerSolarState.OnSolarStateChanged -= OnPlayerSolarStateChanged;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        bool isGrounded = CheckGrounded();
        bool isMoving = CheckMoving();

        if (!isCasting)
        {
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", !isGrounded);
        }
        else
        {
            // Still keep ground values updated during cast if you want
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", !isGrounded);
        }
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

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
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

        isCasting = true;

        animator.ResetTrigger("Cast");
        animator.SetTrigger("Cast");

        Debug.Log("Cast animation triggered");
    }

    // Add this as an Animation Event near the end of the cast animation clip
    public void EndCastAnimation()
    {
        isCasting = false;
        Debug.Log("Cast animation ended");
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
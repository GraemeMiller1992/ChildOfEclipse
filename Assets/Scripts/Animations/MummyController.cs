using UnityEngine;
using UnityEngine.AI;

public class MummyController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        float speed = agent.velocity.magnitude;

        // Optional: clamp tiny values to 0 to stop jitter
        if (speed < 0.05f) speed = 0f;

        animator.SetFloat("Speed", speed);
    }
}
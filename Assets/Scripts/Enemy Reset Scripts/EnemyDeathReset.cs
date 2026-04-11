using UnityEngine;
using UnityEngine.AI;

namespace World
{
    public class EnemyResetOnPlayerDeath : MonoBehaviour
    {
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Rigidbody rb;

        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
            if (rb == null) rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            EnemyResetManager.Register(this);
        }

        private void OnDisable()
        {
            EnemyResetManager.Unregister(this);
        }

        public void ResetEnemy()
        {
            if (enemyAI != null)
                enemyAI.DisableAI();

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                navMeshAgent.Warp(startPosition);
            }
            else
            {
                transform.position = startPosition;
            }

            transform.rotation = startRotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (enemyAI != null)
            {
                enemyAI.EnableAI();
                enemyAI.ForceState(EnemyAI.AIState.Patrol);
            }
        }
    }
}

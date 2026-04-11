using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    public class EnemyDeathReset : MonoBehaviour
    {
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SolarState solarState;

        [Header("Reset")]
        [SerializeField] private EnemyAI.AIState stateAfterReset = EnemyAI.AIState.Patrol;
        [SerializeField] private bool resetRotation = true;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private SolarStateValue startSolarState;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            if (enemyAI == null)
                enemyAI = GetComponent<EnemyAI>();

            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (solarState == null)
                solarState = GetComponent<SolarState>();

            if (solarState != null)
                startSolarState = solarState.CurrentState;
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
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                navMeshAgent.Warp(startPosition);
            }
            else
            {
                transform.position = startPosition;
            }

            if (resetRotation)
                transform.rotation = startRotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (solarState != null)
                solarState.CurrentState = startSolarState;

            if (enemyAI != null)
            {
                enemyAI.ResetDetectionState();
                enemyAI.EnableAI();
                enemyAI.ForceState(stateAfterReset);
            }
        }
    }
}

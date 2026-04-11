using System.Collections.Generic;
using UnityEngine;
using ChildOfEclipse.Health;

namespace World
{
    public class EnemyResetManager : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerHealth;

        private static readonly List<EnemyDeathReset> enemies = new();

        private void Awake()
        {
            if (playerHealth == null)
            {
                playerHealth = GetComponent<HealthComponent>();

                if (playerHealth == null)
                    playerHealth = GetComponentInParent<HealthComponent>();

                if (playerHealth == null)
                    playerHealth = GetComponentInChildren<HealthComponent>();
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath.AddListener(ResetAllEnemies);
        }

        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath.RemoveListener(ResetAllEnemies);
        }

        public static void Register(EnemyDeathReset enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public static void Unregister(EnemyDeathReset enemy)
        {
            if (enemy != null)
                enemies.Remove(enemy);
        }

        private void ResetAllEnemies()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                enemies[i].ResetEnemy();
            }
        }
    }
}
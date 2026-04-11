using System.Collections.Generic;
using UnityEngine;
using ChildOfEclipse.Health;

namespace World
{
    public class EnemyResetManager : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerHealth;

        private static List<EnemyResetOnPlayerDeath> enemies = new();

        private void Awake()
        {
            if (playerHealth == null)
                playerHealth = GetComponent<HealthComponent>();
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

        public static void Register(EnemyResetOnPlayerDeath enemy)
        {
            if (!enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public static void Unregister(EnemyResetOnPlayerDeath enemy)
        {
            enemies.Remove(enemy);
        }

        private void ResetAllEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    enemy.ResetEnemy();
            }
        }
    }
}

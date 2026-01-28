using UnityEngine;
using UnityEngine.Events;

namespace ChildOfEclipse.Health
{
    /// <summary>
    /// Manages health for a game object, including damage, healing, and death mechanics.
    /// Attach this component to any game object that needs health and can be killed.
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health this entity can have")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Current health value (starts at maxHealth by default)")]
        [SerializeField] private float currentHealth;

        [Tooltip("Should the object be destroyed when health reaches zero?")]
        [SerializeField] private bool destroyOnDeath = true;

        [Tooltip("Delay in seconds before destroying the object (only if destroyOnDeath is true)")]
        [SerializeField] private float destroyDelay = 0f;

        [Tooltip("Should the object be disabled instead of destroyed?")]
        [SerializeField] private bool disableOnDeath = false;

        [Header("Events")]
        [Space]
        [Tooltip("Invoked when this entity takes damage")]
        public UnityEvent<float> OnDamageTaken;

        [Tooltip("Invoked when this entity is healed")]
        public UnityEvent<float> OnHealed;

        [Tooltip("Invoked when health reaches zero (before destruction/disabling)")]
        public UnityEvent OnDeath;

        [Tooltip("Invoked when health changes (passes current health)")]
        public UnityEvent<float> OnHealthChanged;

        /// <summary>
        /// Gets the current health value
        /// </summary>
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Gets the maximum health value
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Gets whether this entity is alive (health > 0)
        /// </summary>
        public bool IsAlive => currentHealth > 0;

        /// <summary>
        /// Gets whether this entity is dead (health <= 0)
        /// </summary>
        public bool IsDead => currentHealth <= 0;

        /// <summary>
        /// Gets the current health as a percentage (0-1)
        /// </summary>
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;

        private bool _hasDied = false;

        private void Awake()
        {
            // Initialize health to max if not set in inspector
            if (currentHealth <= 0)
            {
                currentHealth = maxHealth;
            }
        }

        private void Start()
        {
            // Notify initial health state
            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Apply damage to this entity. If health reaches zero, death is triggered.
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply (must be positive)</param>
        public void TakeDamage(float damageAmount)
        {
            if (IsDead || _hasDied || damageAmount <= 0)
            {
                return;
            }

            currentHealth -= damageAmount;
            currentHealth = Mathf.Max(0f, currentHealth);

            OnDamageTaken?.Invoke(damageAmount);
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal this entity by the specified amount.
        /// </summary>
        /// <param name="healAmount">Amount of health to restore (must be positive)</param>
        public void Heal(float healAmount)
        {
            if (IsDead || _hasDied || healAmount <= 0)
            {
                return;
            }

            float oldHealth = currentHealth;
            currentHealth += healAmount;
            currentHealth = Mathf.Min(maxHealth, currentHealth);

            float actualHealAmount = currentHealth - oldHealth;

            if (actualHealAmount > 0)
            {
                OnHealed?.Invoke(actualHealAmount);
                OnHealthChanged?.Invoke(currentHealth);
            }
        }

        /// <summary>
        /// Set health directly to a specific value.
        /// </summary>
        /// <param name="healthValue">The new health value (clamped to 0-maxHealth)</param>
        public void SetHealth(float healthValue)
        {
            if (_hasDied)
            {
                return;
            }

            float oldHealth = currentHealth;
            currentHealth = Mathf.Clamp(healthValue, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth < oldHealth)
            {
                OnDamageTaken?.Invoke(oldHealth - currentHealth);
            }
            else if (currentHealth > oldHealth)
            {
                OnHealed?.Invoke(currentHealth - oldHealth);
            }

            if (currentHealth <= 0 && !_hasDied)
            {
                Die();
            }
        }

        /// <summary>
        /// Set the maximum health value. Optionally scales current health proportionally.
        /// </summary>
        /// <param name="newMaxHealth">New maximum health value</param>
        /// <param name="scaleCurrentHealth">Whether to scale current health proportionally</param>
        public void SetMaxHealth(float newMaxHealth, bool scaleCurrentHealth = true)
        {
            if (newMaxHealth <= 0)
            {
                Debug.LogWarning("Cannot set max health to zero or negative value.", this);
                return;
            }

            if (scaleCurrentHealth && maxHealth > 0)
            {
                float healthPercentage = currentHealth / maxHealth;
                currentHealth = newMaxHealth * healthPercentage;
            }

            maxHealth = newMaxHealth;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Kill this entity instantly, setting health to zero and triggering death.
        /// </summary>
        public void Kill()
        {
            if (_hasDied)
            {
                return;
            }

            currentHealth = 0;
            OnHealthChanged?.Invoke(0f);
            Die();
        }

        /// <summary>
        /// Revive this entity, restoring it to full health and resetting death state.
        /// </summary>
        public void Revive()
        {
            if (!_hasDied)
            {
                return;
            }

            _hasDied = false;
            currentHealth = maxHealth;
            
            // Re-enable the game object if it was disabled
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Handle death logic: invoke events, disable/destroy object as configured.
        /// </summary>
        private void Die()
        {
            if (_hasDied)
            {
                return;
            }

            _hasDied = true;
            OnDeath?.Invoke();

            if (disableOnDeath)
            {
                gameObject.SetActive(false);
            }
            else if (destroyOnDeath)
            {
                if (destroyDelay > 0)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Reset health to maximum and clear death state.
        /// Useful for respawning or resetting game state.
        /// </summary>
        public void ResetHealth()
        {
            _hasDied = false;
            currentHealth = maxHealth;
            
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            OnHealthChanged?.Invoke(currentHealth);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate inspector values to prevent invalid states.
        /// </summary>
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }
#endif
    }
}

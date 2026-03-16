using UnityEngine;
using Actions;

namespace ChildOfEclipse.Health
{
    /// <summary>
    /// A component that runs actions in response to HealthComponent events.
    /// Attach this to the same GameObject as a HealthComponent to execute actions
    /// when damage is taken, healing occurs, death happens, or health changes.
    /// </summary>
    public class HealthActionRunner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The HealthComponent to listen to events from. If null, will try to find one on this GameObject.")]
        [SerializeField] private HealthComponent _healthComponent;

        [Header("Action Runners")]
        [Space]
        [Tooltip("Actions to execute when damage is taken")]
        [SerializeField] private ActionRunner _onDamageTakenActions = new ActionRunner();

        [Tooltip("Actions to execute when healed")]
        [SerializeField] private ActionRunner _onHealedActions = new ActionRunner();

        [Tooltip("Actions to execute when death occurs")]
        [SerializeField] private ActionRunner _onDeathActions = new ActionRunner();

        [Tooltip("Actions to execute when health changes")]
        [SerializeField] private ActionRunner _onHealthChangedActions = new ActionRunner();

        /// <summary>
        /// Gets the HealthComponent reference.
        /// </summary>
        public HealthComponent HealthComponent => _healthComponent;

        /// <summary>
        /// Gets the actions that run when damage is taken.
        /// </summary>
        public ActionRunner OnDamageTakenActions => _onDamageTakenActions;

        /// <summary>
        /// Gets the actions that run when healed.
        /// </summary>
        public ActionRunner OnHealedActions => _onHealedActions;

        /// <summary>
        /// Gets the actions that run on death.
        /// </summary>
        public ActionRunner OnDeathActions => _onDeathActions;

        /// <summary>
        /// Gets the actions that run when health changes.
        /// </summary>
        public ActionRunner OnHealthChangedActions => _onHealthChangedActions;

        private void Awake()
        {
            // Find HealthComponent if not assigned
            if (_healthComponent == null)
            {
                _healthComponent = GetComponent<HealthComponent>();
            }

            if (_healthComponent == null)
            {
                Debug.LogError("HealthActionRunner: No HealthComponent found on this GameObject!", this);
                return;
            }

            // Subscribe to health events
            _healthComponent.OnDamageTaken.AddListener(HandleDamageTaken);
            _healthComponent.OnHealed.AddListener(HandleHealed);
            _healthComponent.OnDeath.AddListener(HandleDeath);
            _healthComponent.OnHealthChanged.AddListener(HandleHealthChanged);
        }

        private void OnDestroy()
        {
            // Unsubscribe from health events to prevent memory leaks
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken.RemoveListener(HandleDamageTaken);
                _healthComponent.OnHealed.RemoveListener(HandleHealed);
                _healthComponent.OnDeath.RemoveListener(HandleDeath);
                _healthComponent.OnHealthChanged.RemoveListener(HandleHealthChanged);
            }
        }

        /// <summary>
        /// Handles the damage taken event and runs the associated actions.
        /// </summary>
        /// <param name="damageAmount">The amount of damage taken</param>
        private void HandleDamageTaken(float damageAmount)
        {
            HealthEventContext context = new HealthEventContext
            {
                HealthComponent = _healthComponent,
                DamageAmount = damageAmount,
                HealAmount = 0f,
                EventType = HealthEventType.DamageTaken
            };

            _onDamageTakenActions.RunAll(context);
        }

        /// <summary>
        /// Handles the healed event and runs the associated actions.
        /// </summary>
        /// <param name="healAmount">The amount healed</param>
        private void HandleHealed(float healAmount)
        {
            HealthEventContext context = new HealthEventContext
            {
                HealthComponent = _healthComponent,
                DamageAmount = 0f,
                HealAmount = healAmount,
                EventType = HealthEventType.Healed
            };

            _onHealedActions.RunAll(context);
        }

        /// <summary>
        /// Handles the death event and runs the associated actions.
        /// </summary>
        private void HandleDeath()
        {
            HealthEventContext context = new HealthEventContext
            {
                HealthComponent = _healthComponent,
                DamageAmount = 0f,
                HealAmount = 0f,
                EventType = HealthEventType.Death
            };

            _onDeathActions.RunAll(context);
        }

        /// <summary>
        /// Handles the health changed event and runs the associated actions.
        /// </summary>
        /// <param name="currentHealth">The current health value</param>
        private void HandleHealthChanged(float currentHealth)
        {
            HealthEventContext context = new HealthEventContext
            {
                HealthComponent = _healthComponent,
                DamageAmount = 0f,
                HealAmount = 0f,
                CurrentHealth = currentHealth,
                EventType = HealthEventType.HealthChanged
            };

            _onHealthChangedActions.RunAll(context);
        }

        /// <summary>
        /// Manually trigger all damage taken actions.
        /// </summary>
        /// <param name="damageAmount">The damage amount to pass to the context</param>
        public void TriggerDamageTakenActions(float damageAmount)
        {
            HandleDamageTaken(damageAmount);
        }

        /// <summary>
        /// Manually trigger all healed actions.
        /// </summary>
        /// <param name="healAmount">The heal amount to pass to the context</param>
        public void TriggerHealedActions(float healAmount)
        {
            HandleHealed(healAmount);
        }

        /// <summary>
        /// Manually trigger all death actions.
        /// </summary>
        public void TriggerDeathActions()
        {
            HandleDeath();
        }

        /// <summary>
        /// Manually trigger all health changed actions.
        /// </summary>
        /// <param name="currentHealth">The current health to pass to the context</param>
        public void TriggerHealthChangedActions(float currentHealth)
        {
            HandleHealthChanged(currentHealth);
        }
    }

    /// <summary>
    /// Context object passed to actions when health events occur.
    /// </summary>
    public class HealthEventContext
    {
        /// <summary>
        /// The HealthComponent that triggered the event.
        /// </summary>
        public HealthComponent HealthComponent { get; set; }

        /// <summary>
        /// The amount of damage taken (if applicable).
        /// </summary>
        public float DamageAmount { get; set; }

        /// <summary>
        /// The amount healed (if applicable).
        /// </summary>
        public float HealAmount { get; set; }

        /// <summary>
        /// The current health value (if applicable).
        /// </summary>
        public float CurrentHealth { get; set; }

        /// <summary>
        /// The type of health event that occurred.
        /// </summary>
        public HealthEventType EventType { get; set; }
    }

    /// <summary>
    /// Enumeration of possible health event types.
    /// </summary>
    public enum HealthEventType
    {
        /// <summary>
        /// Damage was taken.
        /// </summary>
        DamageTaken,

        /// <summary>
        /// Healing occurred.
        /// </summary>
        Healed,

        /// <summary>
        /// Death occurred.
        /// </summary>
        Death,

        /// <summary>
        /// Health changed in any way.
        /// </summary>
        HealthChanged
    }
}

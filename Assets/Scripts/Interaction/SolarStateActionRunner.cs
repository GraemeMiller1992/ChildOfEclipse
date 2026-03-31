using UnityEngine;
using System;
using Actions;
using World;

namespace ChildOfEclipse.Interaction
{
    /// <summary>
    /// Holds the action runners for a single solar state's interaction events.
    /// </summary>
    [Serializable]
    public class SolarStateInteractionActions
    {
        [Tooltip("Actions to execute when hovering enters while in this solar state")]
        [SerializeField] private ActionRunner _onHoverEnterActions = new ActionRunner();

        [Tooltip("Actions to execute when hovering exits while in this solar state")]
        [SerializeField] private ActionRunner _onHoverExitActions = new ActionRunner();

        [Tooltip("Actions to execute when interacted with while in this solar state")]
        [SerializeField] private ActionRunner _onInteractActions = new ActionRunner();

        /// <summary>
        /// Gets the actions that run on hover enter.
        /// </summary>
        public ActionRunner OnHoverEnterActions => _onHoverEnterActions;

        /// <summary>
        /// Gets the actions that run on hover exit.
        /// </summary>
        public ActionRunner OnHoverExitActions => _onHoverExitActions;

        /// <summary>
        /// Gets the actions that run on interact.
        /// </summary>
        public ActionRunner OnInteractActions => _onInteractActions;
    }

    /// <summary>
    /// An interactable component that runs actions based on the current SolarState.
    /// Each solar state (Sun, Moon, Eclipse) has its own set of action runners for
    /// hover enter, hover exit, and interact events. The appropriate actions are
    /// determined by the SolarState component's current state at the time of the event.
    /// </summary>
    public class SolarStateActionRunner : MonoBehaviour, IInteractable
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("The SolarState component to check. If null, will try to find one on this GameObject.")]
        [SerializeField] private SolarState _solarState;

        [Header("Sun State Actions")]
        [Tooltip("Actions to run when this object is in the Sun solar state")]
        [SerializeField] private SolarStateInteractionActions _sunActions = new SolarStateInteractionActions();

        [Header("Moon State Actions")]
        [Tooltip("Actions to run when this object is in the Moon solar state")]
        [SerializeField] private SolarStateInteractionActions _moonActions = new SolarStateInteractionActions();

        [Header("Eclipse State Actions")]
        [Tooltip("Actions to run when this object is in the Eclipse solar state")]
        [SerializeField] private SolarStateInteractionActions _eclipseActions = new SolarStateInteractionActions();

        [Header("Interaction Settings")]
        [Tooltip("If true, this object cannot be interacted with.")]
        [SerializeField] private bool _interactionLocked = false;

        [Tooltip("Custom description for this interactable. If empty, generates one automatically.")]
        [SerializeField] private string _customInteractionDescription = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the SolarState component reference.
        /// </summary>
        public SolarState SolarState => _solarState;

        /// <summary>
        /// Gets the action runners for the Sun solar state.
        /// </summary>
        public SolarStateInteractionActions SunActions => _sunActions;

        /// <summary>
        /// Gets the action runners for the Moon solar state.
        /// </summary>
        public SolarStateInteractionActions MoonActions => _moonActions;

        /// <summary>
        /// Gets the action runners for the Eclipse solar state.
        /// </summary>
        public SolarStateInteractionActions EclipseActions => _eclipseActions;

        /// <summary>
        /// Returns whether this object can currently be interacted with.
        /// </summary>
        public bool CanInteract => !_interactionLocked && _solarState != null;

        /// <summary>
        /// Returns whether interaction is currently locked.
        /// </summary>
        public bool IsInteractionLocked => _interactionLocked;

        /// <summary>
        /// Returns the description of what will happen when interacted with.
        /// </summary>
        public string InteractionDescription
        {
            get
            {
                if (!string.IsNullOrEmpty(_customInteractionDescription))
                {
                    return _customInteractionDescription;
                }

                if (_interactionLocked)
                {
                    return "Interaction disabled";
                }

                if (_solarState == null)
                {
                    return "No SolarState assigned";
                }

                return $"{_solarState.CurrentState} interactable";
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_solarState == null)
            {
                _solarState = GetComponent<SolarState>();
            }

            if (_solarState == null)
            {
                Debug.LogError("SolarStateActionRunner: No SolarState found on this GameObject!", this);
            }
        }

        #endregion

        #region IInteractable Implementation

        /// <summary>
        /// Called when the object is clicked by the interact pointer.
        /// Runs the interact actions for the current solar state.
        /// </summary>
        public void OnInteract(GameObject interactor, RaycastHit hitInfo)
        {
            if (_interactionLocked || _solarState == null)
            {
                return;
            }

            SolarStateInteractionActions actions = GetActionsForState(_solarState.CurrentState);
            if (actions == null)
            {
                return;
            }

            SolarStateInteractionContext context = new SolarStateInteractionContext
            {
                ActionRunner = this,
                SolarState = _solarState,
                Interactor = interactor,
                HitInfo = hitInfo,
                CurrentState = _solarState.CurrentState,
                EventType = SolarStateInteractionEventType.Interact
            };

            actions.OnInteractActions.RunAll(context);
        }

        /// <summary>
        /// Called when the object is hovered over by the interact pointer.
        /// Runs the hover enter actions for the current solar state.
        /// </summary>
        public void OnHoverEnter(GameObject interactor)
        {
            if (_interactionLocked || _solarState == null)
            {
                return;
            }

            SolarStateInteractionActions actions = GetActionsForState(_solarState.CurrentState);
            if (actions == null)
            {
                return;
            }

            SolarStateInteractionContext context = new SolarStateInteractionContext
            {
                ActionRunner = this,
                SolarState = _solarState,
                Interactor = interactor,
                CurrentState = _solarState.CurrentState,
                EventType = SolarStateInteractionEventType.HoverEnter
            };

            actions.OnHoverEnterActions.RunAll(context);
        }

        /// <summary>
        /// Called when the object is no longer being hovered over.
        /// Runs the hover exit actions for the current solar state.
        /// </summary>
        public void OnHoverExit(GameObject interactor)
        {
            if (_interactionLocked || _solarState == null)
            {
                return;
            }

            SolarStateInteractionActions actions = GetActionsForState(_solarState.CurrentState);
            if (actions == null)
            {
                return;
            }

            SolarStateInteractionContext context = new SolarStateInteractionContext
            {
                ActionRunner = this,
                SolarState = _solarState,
                Interactor = interactor,
                CurrentState = _solarState.CurrentState,
                EventType = SolarStateInteractionEventType.HoverExit
            };

            actions.OnHoverExitActions.RunAll(context);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the action runners for a specific solar state.
        /// </summary>
        /// <param name="state">The solar state to get actions for.</param>
        /// <returns>The interaction actions for the specified state, or null if invalid.</returns>
        public SolarStateInteractionActions GetActionsForState(SolarStateValue state)
        {
            switch (state)
            {
                case SolarStateValue.Sun:
                    return _sunActions;
                case SolarStateValue.Moon:
                    return _moonActions;
                case SolarStateValue.Eclipse:
                    return _eclipseActions;
                default:
                    Debug.LogWarning($"SolarStateActionRunner: Unknown solar state '{state}'", this);
                    return null;
            }
        }

        /// <summary>
        /// Disable interaction on this object.
        /// </summary>
        public void DisableInteraction()
        {
            _interactionLocked = true;
        }

        /// <summary>
        /// Enable interaction on this object.
        /// </summary>
        public void EnableInteraction()
        {
            _interactionLocked = false;
        }

        /// <summary>
        /// Set whether interaction is locked.
        /// </summary>
        /// <param name="locked">Whether the interaction should be locked.</param>
        public void SetInteractionLocked(bool locked)
        {
            _interactionLocked = locked;
        }

        /// <summary>
        /// Manually triggers the interact actions for the current solar state.
        /// </summary>
        /// <param name="interactor">The GameObject performing the interaction. Defaults to this GameObject.</param>
        public void TriggerInteract(GameObject interactor = null)
        {
            OnInteract(interactor ?? gameObject, new RaycastHit { point = transform.position, normal = Vector3.up });
        }

        /// <summary>
        /// Manually triggers the hover enter actions for the current solar state.
        /// </summary>
        /// <param name="interactor">The GameObject performing the hover. Defaults to this GameObject.</param>
        public void TriggerHoverEnter(GameObject interactor = null)
        {
            OnHoverEnter(interactor ?? gameObject);
        }

        /// <summary>
        /// Manually triggers the hover exit actions for the current solar state.
        /// </summary>
        /// <param name="interactor">The GameObject that was hovering. Defaults to this GameObject.</param>
        public void TriggerHoverExit(GameObject interactor = null)
        {
            OnHoverExit(interactor ?? gameObject);
        }

        #endregion
    }

    /// <summary>
    /// Context object passed to actions when solar state interaction events occur.
    /// </summary>
    public class SolarStateInteractionContext
    {
        /// <summary>
        /// The SolarStateActionRunner that triggered the event.
        /// </summary>
        public SolarStateActionRunner ActionRunner { get; set; }

        /// <summary>
        /// The SolarState component on the interacted object.
        /// </summary>
        public SolarState SolarState { get; set; }

        /// <summary>
        /// The GameObject that initiated the interaction or hover.
        /// </summary>
        public GameObject Interactor { get; set; }

        /// <summary>
        /// The raycast hit information (only available for interact events).
        /// </summary>
        public RaycastHit HitInfo { get; set; }

        /// <summary>
        /// The current solar state at the time of the event.
        /// </summary>
        public SolarStateValue CurrentState { get; set; }

        /// <summary>
        /// The type of interaction event that occurred.
        /// </summary>
        public SolarStateInteractionEventType EventType { get; set; }
    }

    /// <summary>
    /// Enumeration of possible solar state interaction event types.
    /// </summary>
    public enum SolarStateInteractionEventType
    {
        /// <summary>
        /// The pointer entered the object's hover zone.
        /// </summary>
        HoverEnter,

        /// <summary>
        /// The pointer exited the object's hover zone.
        /// </summary>
        HoverExit,

        /// <summary>
        /// The object was clicked/interacted with.
        /// </summary>
        Interact
    }
}

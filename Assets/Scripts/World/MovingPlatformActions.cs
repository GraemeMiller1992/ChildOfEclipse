using System;
using UnityEngine;
using Actions;

namespace World
{
    /// <summary>
    /// Base class for MovingPlatform actions that provides common functionality.
    /// </summary>
    [Serializable]
    public abstract class MovingPlatformAction : IAction
    {
        [Tooltip("The MovingPlatform to control. If null, will try to find one on the same GameObject as the context.")]
        [SerializeField] protected MovingPlatform _platform;

        /// <summary>
        /// Gets the MovingPlatform to control, either from the assigned reference or from the context.
        /// </summary>
        protected MovingPlatform GetPlatform(object context)
        {
            if (_platform != null)
            {
                return _platform;
            }

            // Try to get platform from context
            if (context is MovingPlatform platform)
            {
                return platform;
            }

            // Try to get from GameObject context
            if (context is GameObject gameObject)
            {
                return gameObject.GetComponent<MovingPlatform>();
            }

            // Try to get from Component context
            if (context is Component component)
            {
                return component.GetComponent<MovingPlatform>();
            }

            Debug.LogWarning("MovingPlatformAction: No MovingPlatform found. Assign one in the inspector or pass it as context.");
            return null;
        }

        public abstract void Execute(object context = null);
    }

    #region Movement Control Actions

    /// <summary>
    /// Action that starts the platform movement.
    /// </summary>
    [Serializable]
    public class StartMovementAction : MovingPlatformAction
    {
        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.StartMovement();
            }
        }
    }

    /// <summary>
    /// Action that stops the platform movement.
    /// </summary>
    [Serializable]
    public class StopMovementAction : MovingPlatformAction
    {
        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.StopMovement();
            }
        }
    }

    /// <summary>
    /// Action that pauses the platform movement at its current position.
    /// </summary>
    [Serializable]
    public class PauseMovementAction : MovingPlatformAction
    {
        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.PauseMovement();
            }
        }
    }

    /// <summary>
    /// Action that resumes the platform movement from its current position.
    /// </summary>
    [Serializable]
    public class ResumeMovementAction : MovingPlatformAction
    {
        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.ResumeMovement();
            }
        }
    }

    #endregion

    #region Configuration Actions

    /// <summary>
    /// Action that sets the movement mode of the platform.
    /// </summary>
    [Serializable]
    public class SetMovementModeAction : MovingPlatformAction
    {
        [Tooltip("The movement mode to set.")]
        [SerializeField] private MovingPlatform.MovementMode _movementMode = MovingPlatform.MovementMode.Loop;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.SetMovementMode(_movementMode);
            }
        }
    }

    /// <summary>
    /// Action that sets the move speed of the platform.
    /// </summary>
    [Serializable]
    public class SetMoveSpeedAction : MovingPlatformAction
    {
        [Tooltip("The speed to set.")]
        [SerializeField] private float _speed = 3f;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.SetMoveSpeed(_speed);
            }
        }
    }

    /// <summary>
    /// Action that sets the move speed of the platform using a value from the context.
    /// </summary>
    [Serializable]
    public class SetMoveSpeedFromContextAction : MovingPlatformAction
    {
        [Tooltip("Default speed to use if context doesn't contain a float value.")]
        [SerializeField] private float _defaultSpeed = 3f;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform == null)
            {
                return;
            }

            float speed = _defaultSpeed;

            // Try to get speed from context
            if (context is float floatContext)
            {
                speed = floatContext;
            }
            else if (context is int intContext)
            {
                speed = intContext;
            }
            // Check if context is a MovingPlatformEventContext with speed
            else if (context is MovingPlatformEventContext eventContext)
            {
                speed = eventContext.Speed;
            }

            platform.SetMoveSpeed(speed);
        }
    }

    #endregion

    #region Waypoint Management Actions

    /// <summary>
    /// Action that adds a waypoint to the platform's movement path.
    /// </summary>
    [Serializable]
    public class AddWaypointAction : MovingPlatformAction
    {
        [Tooltip("The waypoint transform to add.")]
        [SerializeField] private Transform _waypoint;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null && _waypoint != null)
            {
                platform.AddWaypoint(_waypoint);
            }
            else if (platform != null)
            {
                Debug.LogWarning("AddWaypointAction: No waypoint assigned.", platform);
            }
        }
    }

    /// <summary>
    /// Action that removes a waypoint from the platform's movement path by index.
    /// </summary>
    [Serializable]
    public class RemoveWaypointAction : MovingPlatformAction
    {
        [Tooltip("The index of the waypoint to remove.")]
        [SerializeField] private int _waypointIndex = 0;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.RemoveWaypoint(_waypointIndex);
            }
        }
    }

    /// <summary>
    /// Action that removes a waypoint from the platform's movement path using index from context.
    /// </summary>
    [Serializable]
    public class RemoveWaypointFromContextAction : MovingPlatformAction
    {
        [Tooltip("Default waypoint index to use if context doesn't contain an integer value.")]
        [SerializeField] private int _defaultIndex = 0;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform == null)
            {
                return;
            }

            int index = _defaultIndex;

            // Try to get index from context
            if (context is int intContext)
            {
                index = intContext;
            }
            else if (context is float floatContext)
            {
                index = Mathf.RoundToInt(floatContext);
            }
            // Check if context is a MovingPlatformEventContext with waypoint index
            else if (context is MovingPlatformEventContext eventContext)
            {
                index = eventContext.WaypointIndex;
            }

            platform.RemoveWaypoint(index);
        }
    }

    /// <summary>
    /// Action that clears all waypoints from the platform.
    /// </summary>
    [Serializable]
    public class ClearWaypointsAction : MovingPlatformAction
    {
        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.ClearWaypoints();
            }
        }
    }

    /// <summary>
    /// Action that sets the current waypoint index and immediately moves to that waypoint.
    /// </summary>
    [Serializable]
    public class SetCurrentWaypointAction : MovingPlatformAction
    {
        [Tooltip("The waypoint index to set as current.")]
        [SerializeField] private int _waypointIndex = 0;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.SetCurrentWaypoint(_waypointIndex);
            }
        }
    }

    /// <summary>
    /// Action that sets the current waypoint index using a value from the context.
    /// </summary>
    [Serializable]
    public class SetCurrentWaypointFromContextAction : MovingPlatformAction
    {
        [Tooltip("Default waypoint index to use if context doesn't contain an integer value.")]
        [SerializeField] private int _defaultIndex = 0;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform == null)
            {
                return;
            }

            int index = _defaultIndex;

            // Try to get index from context
            if (context is int intContext)
            {
                index = intContext;
            }
            else if (context is float floatContext)
            {
                index = Mathf.RoundToInt(floatContext);
            }
            // Check if context is a MovingPlatformEventContext with waypoint index
            else if (context is MovingPlatformEventContext eventContext)
            {
                index = eventContext.WaypointIndex;
            }

            platform.SetCurrentWaypoint(index);
        }
    }

    /// <summary>
    /// Action that teleports the platform to a specific waypoint instantly.
    /// </summary>
    [Serializable]
    public class TeleportToWaypointAction : MovingPlatformAction
    {
        [Tooltip("The waypoint index to teleport to.")]
        [SerializeField] private int _waypointIndex = 0;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform != null)
            {
                platform.TeleportToWaypoint(_waypointIndex);
            }
        }
    }

    /// <summary>
    /// Action that teleports the platform to a waypoint using index from context.
    /// </summary>
    [Serializable]
    public class TeleportToWaypointFromContextAction : MovingPlatformAction
    {
        [Tooltip("Default waypoint index to use if context doesn't contain an integer value.")]
        [SerializeField] private int _defaultIndex = 0;

        public override void Execute(object context = null)
        {
            MovingPlatform platform = GetPlatform(context);
            if (platform == null)
            {
                return;
            }

            int index = _defaultIndex;

            // Try to get index from context
            if (context is int intContext)
            {
                index = intContext;
            }
            else if (context is float floatContext)
            {
                index = Mathf.RoundToInt(floatContext);
            }
            // Check if context is a MovingPlatformEventContext with waypoint index
            else if (context is MovingPlatformEventContext eventContext)
            {
                index = eventContext.WaypointIndex;
            }

            platform.TeleportToWaypoint(index);
        }
    }

    #endregion

    #region Utility Actions

    /// <summary>
    /// Action that waits for a specified duration before continuing.
    /// Useful for creating delays in action sequences.
    /// </summary>
    [Serializable]
    public class WaitAction : IAction
    {
        [Tooltip("Duration to wait in seconds.")]
        [SerializeField] private float _duration = 1f;

        public void Execute(object context = null)
        {
            // Note: This action is synchronous and will block.
            // For asynchronous waiting, consider implementing a coroutine-based action system.
            // For now, this serves as a placeholder or can be used with a custom coroutine runner.
            Debug.Log($"WaitAction: Waiting for {_duration} seconds (synchronous - consider using coroutine-based actions for delays).");
        }
    }

    /// <summary>
    /// Action that logs a debug message.
    /// Useful for debugging action sequences.
    /// </summary>
    [Serializable]
    public class DebugLogAction : IAction
    {
        [Tooltip("The message to log.")]
        [SerializeField] private string _message = "Debug Log";

        [Tooltip("Log type (Info, Warning, or Error).")]
        [SerializeField] private LogType _logType = LogType.Log;

        public void Execute(object context = null)
        {
            string contextInfo = context != null ? $" [Context: {context.GetType().Name}]" : "";
            string fullMessage = _message + contextInfo;

            switch (_logType)
            {
                case LogType.Warning:
                    Debug.LogWarning(fullMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(fullMessage);
                    break;
                default:
                    Debug.Log(fullMessage);
                    break;
            }
        }
    }

    #endregion

    #region Context Classes

    /// <summary>
    /// Context object passed to actions when moving platform events occur.
    /// </summary>
    public class MovingPlatformEventContext
    {
        /// <summary>
        /// The MovingPlatform that triggered the event.
        /// </summary>
        public MovingPlatform Platform { get; set; }

        /// <summary>
        /// The current waypoint index (if applicable).
        /// </summary>
        public int WaypointIndex { get; set; }

        /// <summary>
        /// The current speed (if applicable).
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// The current movement mode (if applicable).
        /// </summary>
        public MovingPlatform.MovementMode MovementMode { get; set; }

        /// <summary>
        /// The event type that occurred.
        /// </summary>
        public MovingPlatformEventType EventType { get; set; }
    }

    /// <summary>
    /// Enumeration of possible moving platform event types.
    /// </summary>
    public enum MovingPlatformEventType
    {
        /// <summary>
        /// Platform started moving.
        /// </summary>
        Started,

        /// <summary>
        /// Platform stopped moving.
        /// </summary>
        Stopped,

        /// <summary>
        /// Platform paused movement.
        /// </summary>
        Paused,

        /// <summary>
        /// Platform resumed movement.
        /// </summary>
        Resumed,

        /// <summary>
        /// Platform arrived at a waypoint.
        /// </summary>
        WaypointArrived,

        /// <summary>
        /// Platform teleported to a waypoint.
        /// </summary>
        Teleported,

        /// <summary>
        /// Movement mode changed.
        /// </summary>
        MovementModeChanged,

        /// <summary>
        /// Speed changed.
        /// </summary>
        SpeedChanged
    }

    #endregion
}

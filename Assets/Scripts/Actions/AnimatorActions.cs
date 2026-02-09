using UnityEngine;
using System;

namespace Actions
{
    #region Animator Actions

    /// <summary>
    /// Plays an animation state on an Animator.
    /// </summary>
    [Serializable]
    public class PlayAnimationAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The name of the animation state to play")]
        public string stateName;

        [Tooltip("The layer index to play the animation on")]
        public int layer = 0;

        [Tooltip("The normalized time at which the animation will start playing")]
        public float normalizedTime = 0f;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName, layer, normalizedTime);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("PlayAnimationAction: Animator is null");
                if (string.IsNullOrEmpty(stateName))
                    Debug.LogWarning("PlayAnimationAction: State name is empty");
            }
        }
    }

    /// <summary>
    /// Crossfades to an animation state on an Animator.
    /// </summary>
    [Serializable]
    public class CrossFadeAnimationAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The name of the animation state to crossfade to")]
        public string stateName;

        [Tooltip("The duration of the crossfade transition in seconds")]
        public float transitionDuration = 0.1f;

        [Tooltip("The layer index to crossfade the animation on")]
        public int layer = 0;

        [Tooltip("The normalized time at which the animation will start")]
        public float normalizedTime = 0f;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(stateName))
            {
                animator.CrossFade(stateName, transitionDuration, layer, normalizedTime);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("CrossFadeAnimationAction: Animator is null");
                if (string.IsNullOrEmpty(stateName))
                    Debug.LogWarning("CrossFadeAnimationAction: State name is empty");
            }
        }
    }

    /// <summary>
    /// Crossfades to an animation state by its hash ID.
    /// </summary>
    [Serializable]
    public class CrossFadeInFixedTimeAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The name of the animation state to crossfade to")]
        public string stateName;

        [Tooltip("The duration of the crossfade transition in seconds")]
        public float transitionDuration = 0.1f;

        [Tooltip("The layer index to crossfade the animation on")]
        public int layer = 0;

        [Tooltip("The time at which the animation will start")]
        public float fixedTime = 0f;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(stateName))
            {
                animator.CrossFadeInFixedTime(stateName, transitionDuration, layer, fixedTime);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("CrossFadeInFixedTimeAction: Animator is null");
                if (string.IsNullOrEmpty(stateName))
                    Debug.LogWarning("CrossFadeInFixedTimeAction: State name is empty");
            }
        }
    }

    /// <summary>
    /// Plays an animation state by its hash ID.
    /// </summary>
    [Serializable]
    public class PlayInFixedTimeAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The name of the animation state to play")]
        public string stateName;

        [Tooltip("The layer index to play the animation on")]
        public int layer = 0;

        [Tooltip("The time at which the animation will start")]
        public float fixedTime = 0f;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(stateName))
            {
                animator.PlayInFixedTime(stateName, layer, fixedTime);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("PlayInFixedTimeAction: Animator is null");
                if (string.IsNullOrEmpty(stateName))
                    Debug.LogWarning("PlayInFixedTimeAction: State name is empty");
            }
        }
    }

    /// <summary>
    /// Sets a trigger parameter on an Animator.
    /// </summary>
    [Serializable]
    public class SetAnimatorTriggerAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The trigger parameter name")]
        public string triggerName;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("SetAnimatorTriggerAction: Animator is null");
                if (string.IsNullOrEmpty(triggerName))
                    Debug.LogWarning("SetAnimatorTriggerAction: Trigger name is empty");
            }
        }
    }

    /// <summary>
    /// Resets a trigger parameter on an Animator.
    /// </summary>
    [Serializable]
    public class ResetAnimatorTriggerAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The trigger parameter name")]
        public string triggerName;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
            {
                animator.ResetTrigger(triggerName);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("ResetAnimatorTriggerAction: Animator is null");
                if (string.IsNullOrEmpty(triggerName))
                    Debug.LogWarning("ResetAnimatorTriggerAction: Trigger name is empty");
            }
        }
    }

    /// <summary>
    /// Sets a boolean parameter on an Animator.
    /// </summary>
    [Serializable]
    public class SetAnimatorBoolAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The boolean parameter name")]
        public string parameterName;

        [Tooltip("The value to set")]
        public bool value;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                animator.SetBool(parameterName, value);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("SetAnimatorBoolAction: Animator is null");
                if (string.IsNullOrEmpty(parameterName))
                    Debug.LogWarning("SetAnimatorBoolAction: Parameter name is empty");
            }
        }
    }

    /// <summary>
    /// Sets a float parameter on an Animator.
    /// </summary>
    [Serializable]
    public class SetAnimatorFloatAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The float parameter name")]
        public string parameterName;

        [Tooltip("The value to set")]
        public float value;

        [Tooltip("The damping time for smoothing the transition")]
        public float dampTime = 0f;

        [Tooltip("The delta time for the damping")]
        public float deltaTime = 0f;

        [Tooltip("Whether to use damping")]
        public bool useDamping = false;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                if (useDamping)
                {
                    animator.SetFloat(parameterName, value, dampTime, deltaTime);
                }
                else
                {
                    animator.SetFloat(parameterName, value);
                }
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("SetAnimatorFloatAction: Animator is null");
                if (string.IsNullOrEmpty(parameterName))
                    Debug.LogWarning("SetAnimatorFloatAction: Parameter name is empty");
            }
        }
    }

    /// <summary>
    /// Sets an integer parameter on an Animator.
    /// </summary>
    [Serializable]
    public class SetAnimatorIntAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The integer parameter name")]
        public string parameterName;

        [Tooltip("The value to set")]
        public int value;

        public void Execute(object context = null)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                animator.SetInteger(parameterName, value);
            }
            else
            {
                if (animator == null)
                    Debug.LogWarning("SetAnimatorIntAction: Animator is null");
                if (string.IsNullOrEmpty(parameterName))
                    Debug.LogWarning("SetAnimatorIntAction: Parameter name is empty");
            }
        }
    }

    /// <summary>
    /// Stops the playback of an Animator.
    /// </summary>
    [Serializable]
    public class StopAnimatorPlaybackAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        public void Execute(object context = null)
        {
            if (animator != null)
            {
                animator.StopPlayback();
            }
            else
            {
                Debug.LogWarning("StopAnimatorPlaybackAction: Animator is null");
            }
        }
    }

    /// <summary>
    /// Starts the playback of an Animator.
    /// </summary>
    [Serializable]
    public class StartAnimatorPlaybackAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        public void Execute(object context = null)
        {
            if (animator != null)
            {
                animator.StartPlayback();
            }
            else
            {
                Debug.LogWarning("StartAnimatorPlaybackAction: Animator is null");
            }
        }
    }

    /// <summary>
    /// Sets the playback speed of an Animator.
    /// </summary>
    [Serializable]
    public class SetAnimatorSpeedAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The playback speed (1.0 = normal speed)")]
        public float speed = 1f;

        public void Execute(object context = null)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
            else
            {
                Debug.LogWarning("SetAnimatorSpeedAction: Animator is null");
            }
        }
    }

    /// <summary>
    /// Enables or disables an Animator.
    /// </summary>
    [Serializable]
    public class SetAnimatorEnabledAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("Whether to enable (true) or disable (false) the Animator")]
        public bool enabled = true;

        public void Execute(object context = null)
        {
            if (animator != null)
            {
                animator.enabled = enabled;
            }
            else
            {
                Debug.LogWarning("SetAnimatorEnabledAction: Animator is null");
            }
        }
    }

    /// <summary>
    /// Rebinds the Animator to the avatar.
    /// </summary>
    [Serializable]
    public class RebindAnimatorAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        public void Execute(object context = null)
        {
            if (animator != null)
            {
                animator.Rebind();
            }
            else
            {
                Debug.LogWarning("RebindAnimatorAction: Animator is null");
            }
        }
    }

    /// <summary>
    /// Updates the Animator manually.
    /// </summary>
    [Serializable]
    public class UpdateAnimatorAction : IAction
    {
        [Tooltip("The target Animator component")]
        public Animator animator;

        [Tooltip("The delta time to use for the update")]
        public float deltaTime = 0f;

        public void Execute(object context = null)
        {
            if (animator != null)
            {
                animator.Update(deltaTime);
            }
            else
            {
                Debug.LogWarning("UpdateAnimatorAction: Animator is null");
            }
        }
    }

    #endregion
}

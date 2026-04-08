using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace FindersCheesers
{
    /// <summary>
    /// Handles asynchronous level loading with a loading screen UI.
    /// Moves to DontDestroyOnLoad when instantiated, loads a scene asynchronously,
    /// updates a slider and text with progress, fades the canvas group in/out,
    /// and destroys itself after the load completes and fade-out finishes.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Level Loader")]
    public class LevelLoader : MonoBehaviour
    {
        #region Settings

        [Header("Scene")]
        [Tooltip("The name of the scene to load asynchronously")]
        [SerializeField]
        private string sceneName = string.Empty;

        [Tooltip("Whether to start loading automatically on Awake")]
        [SerializeField]
        private bool loadOnAwake = true;

        [Header("UI References")]
        [Tooltip("The slider that displays the loading progress (0 to 1)")]
        [SerializeField]
        private Slider loadingSlider;

        [Tooltip("The TMP text element that displays the loading progress percentage")]
        [SerializeField]
        private TMP_Text loadingText;

        [Tooltip("The canvas group used to fade the loading screen in and out")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [Header("Fade Settings")]
        [Tooltip("Duration in seconds for the fade-in effect when loading starts")]
        [SerializeField]
        private float fadeInDuration = 0.5f;

        [Tooltip("Duration in seconds for the fade-out effect after loading completes")]
        [SerializeField]
        private float fadeOutDuration = 0.5f;

        [Tooltip("Delay in seconds after loading completes before the fade-out begins")]
        [SerializeField]
        private float postLoadDelay = 1f;

        [Tooltip("The ease curve used for fading")]
        [SerializeField]
        private AnimationCurve fadeCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f));

        [Header("Progress Settings")]
        [Tooltip("Format string for the loading progress text ({0} = percentage 0-100)")]
        [SerializeField]
        private string progressFormat = "{0:F0}%";

        [Tooltip("Minimum display time in seconds for the loading screen, even if loading finishes faster")]
        [SerializeField]
        private float minimumDisplayTime = 1f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        #endregion

        #region Private State

        private AsyncOperation asyncOperation;
        private bool isLoading = false;
        private bool allowSceneActivation = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the loader is currently loading a scene.
        /// </summary>
        public bool IsLoading => isLoading;

        /// <summary>
        /// Gets the current loading progress from 0 to 1.
        /// </summary>
        public float Progress => asyncOperation?.progress ?? 0f;

        /// <summary>
        /// Gets or sets the scene name to load. Must be set before calling LoadLevel().
        /// </summary>
        public string SceneName
        {
            get => sceneName;
            set => sceneName = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // Initialize canvas group alpha to 0 for fade-in
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            // Initialize slider to 0
            if (loadingSlider != null)
            {
                loadingSlider.value = 0f;
            }

            // Initialize text
            UpdateProgressText(0f);
        }

        private void Start()
        {
            if (loadOnAwake)
            {
                LoadLevel();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Begins the async level loading process with fade-in.
        /// </summary>
        public void LoadLevel()
        {
            if (isLoading)
            {
                Debug.LogWarning("[LevelLoader] Already loading a scene.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[LevelLoader] Scene name is empty! Set the sceneName field before loading.");
                return;
            }

            isLoading = true;
            StartCoroutine(LoadLevelRoutine());
        }

        /// <summary>
        /// Begins loading a specific scene by name.
        /// </summary>
        /// <param name="scene">The name of the scene to load.</param>
        public void LoadLevel(string scene)
        {
            sceneName = scene;
            LoadLevel();
        }

        /// <summary>
        /// Sets whether the loaded scene should activate immediately when ready.
        /// If false, call AllowSceneActivation() manually to activate.
        /// </summary>
        /// <param name="allow">Whether to allow automatic scene activation.</param>
        public void SetAllowSceneActivation(bool allow)
        {
            allowSceneActivation = allow;
            if (asyncOperation != null)
            {
                asyncOperation.allowSceneActivation = allow;
            }
        }

        /// <summary>
        /// Manually allows the loaded scene to activate.
        /// Only call this after setting SetAllowSceneActivation(false).
        /// </summary>
        public void AllowSceneActivation()
        {
            allowSceneActivation = true;
            if (asyncOperation != null)
            {
                asyncOperation.allowSceneActivation = true;
            }
        }

        #endregion

        #region Loading Coroutine

        /// <summary>
        /// Main coroutine that handles the full loading sequence:
        /// fade in → async load → post-load delay → fade out → destroy.
        /// </summary>
        private IEnumerator LoadLevelRoutine()
        {
            if (debugMode)
            {
                Debug.Log($"[LevelLoader] Starting to load scene: {sceneName}");
            }

            // Fade in the loading screen
            yield return FadeIn();

            // Record start time for minimum display enforcement
            float loadStartTime = Time.unscaledTime;

            // Begin async scene load
            asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = allowSceneActivation;

            // Track progress while loading
            while (!asyncOperation.isDone)
            {
                float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                UpdateProgressUI(progress);

                if (debugMode)
                {
                    Debug.Log($"[LevelLoader] Progress: {progress:P0} (raw: {asyncOperation.progress:F2})");
                }

                yield return null;
            }

            // Ensure progress shows 100%
            UpdateProgressUI(1f);

            if (debugMode)
            {
                Debug.Log($"[LevelLoader] Scene '{sceneName}' loaded successfully.");
            }

            // Enforce minimum display time
            float elapsed = Time.unscaledTime - loadStartTime;
            if (elapsed < minimumDisplayTime)
            {
                yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);
            }

            // Wait for the post-load delay
            if (postLoadDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(postLoadDelay);
            }

            // Fade out the loading screen
            yield return FadeOut();

            if (debugMode)
            {
                Debug.Log("[LevelLoader] Destroying loader GameObject.");
            }

            // Destroy this GameObject
            Destroy(gameObject);
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Updates the slider and text elements with the current progress.
        /// </summary>
        /// <param name="progress">Progress value from 0 to 1.</param>
        private void UpdateProgressUI(float progress)
        {
            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            UpdateProgressText(progress);
        }

        /// <summary>
        /// Updates the loading progress text.
        /// </summary>
        /// <param name="progress">Progress value from 0 to 1.</param>
        private void UpdateProgressText(float progress)
        {
            if (loadingText != null)
            {
                float percentage = progress * 100f;
                loadingText.text = string.Format(progressFormat, percentage);
            }
        }

        #endregion

        #region Fade Coroutines

        /// <summary>
        /// Fades the canvas group alpha from 0 to 1 over fadeInDuration.
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null || fadeInDuration <= 0f)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                yield break;
            }

            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                float curveValue = fadeCurve.Evaluate(t);
                canvasGroup.alpha = curveValue;

                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Fades the canvas group alpha from 1 to 0 over fadeOutDuration.
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (canvasGroup == null || fadeOutDuration <= 0f)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }

                yield break;
            }

            // Disable interaction during fade out
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float curveValue = fadeCurve.Evaluate(1f - t);
                canvasGroup.alpha = curveValue;

                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        #endregion
    }
}

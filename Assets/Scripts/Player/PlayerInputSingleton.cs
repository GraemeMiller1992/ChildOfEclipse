using UnityEngine;
using UnityEngine.InputSystem;

namespace ChildOfEclipse
{
    /// <summary>
    /// Singleton that provides access to the PlayerInput component.
    /// This singleton is created in Awake() to ensure it's available in Start().
    /// </summary>
    public class PlayerInputSingleton : MonoBehaviour
    {
        private static PlayerInputSingleton _instance;
        public static PlayerInputSingleton Instance => _instance;

        [Tooltip("The PlayerInput component on this GameObject.")]
        public PlayerInput PlayerInput;

        private void Awake()
        {
            // Singleton pattern - ensure only one instance exists
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Multiple PlayerInputSingleton instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Get PlayerInput component if not assigned
            if (PlayerInput == null)
            {
                PlayerInput = GetComponent<PlayerInput>();
                if (PlayerInput == null)
                {
                    Debug.LogError("PlayerInput component not found on PlayerInputSingleton GameObject!");
                }
            }

            // Optional: Don't destroy on load if you want the singleton to persist between scenes
            // DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

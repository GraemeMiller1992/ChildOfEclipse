using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ChildOfEclipse;

public class PauseMenu : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Reference to the Pause input action.")]
    [SerializeField] private InputActionReference pauseActionReference;

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private ButtonSceneLoader[] buttonSceneLoaders;
    public static bool GameIsPaused = false;

    private InputAction _pauseAction;

    void Start()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        var playerInput = PlayerInputSingleton.Instance?.PlayerInput;

        if (playerInput != null && pauseActionReference != null)
        {
            _pauseAction = playerInput.actions.FindAction(pauseActionReference.action.id);
        }
        else if (pauseActionReference != null)
        {
            Debug.LogError("PlayerInputSingleton.Instance or PlayerInput component not found!", this);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        if (buttonSceneLoaders != null)
        {
            foreach (var loader in buttonSceneLoaders)
            {
                loader.Subscribe();
            }
        }

        Resume();
    }

    void Update()
    {
        if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        Time.timeScale = 1f;
        GameIsPaused = false;
        AudioListener.pause = false;
    }

    public void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        Time.timeScale = 0f;
        GameIsPaused = true;
        AudioListener.pause = true;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameIsPaused = false;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

public class SolarStateCursorController : MonoBehaviour
{
    private static SolarStateCursorController instance;

    [Header("Player Search")]
    [SerializeField] private string playerTag = "Player";

    [Header("Cursor Textures")]
    [SerializeField] private Texture2D sunCursor;
    [SerializeField] private Texture2D moonCursor;
    [SerializeField] private Texture2D eclipseCursor;

    [Header("Hotspots")]
    [SerializeField] private Vector2 sunHotspot;
    [SerializeField] private Vector2 moonHotspot;
    [SerializeField] private Vector2 eclipseHotspot;

    [Header("Cursor Mode")]
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    private SolarState playerSolarState;
    private bool useSolarStateCursor;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromSolarState();
    }

    private void Start()
    {
        SetMenuCursor();
        FindPlayerSolarStateInScene();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        RefreshCursor();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) return;
        RefreshCursor();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayerSolarStateInScene();
    }

    private void FindPlayerSolarStateInScene()
    {
        UnsubscribeFromSolarState();

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player != null)
            playerSolarState = player.GetComponent<SolarState>();

        if (playerSolarState != null)
        {
            playerSolarState.OnSolarStateChanged += HandleSolarStateChanged;
            SetGameplayCursor();
        }
        else
        {
            SetMenuCursor();
        }
    }

    private void UnsubscribeFromSolarState()
    {
        if (playerSolarState != null)
        {
            playerSolarState.OnSolarStateChanged -= HandleSolarStateChanged;
            playerSolarState = null;
        }
    }

    public void SetMenuCursor()
    {
        useSolarStateCursor = false;
        ApplyCursor(SolarStateValue.Sun);
    }

    public void SetGameplayCursor()
    {
        useSolarStateCursor = true;
        RefreshCursor();
    }

    private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
    {
        if (!useSolarStateCursor) return;
        ApplyCursor(newState);
    }

    private void RefreshCursor()
    {
        if (useSolarStateCursor && playerSolarState != null)
            ApplyCursor(playerSolarState.CurrentState);
        else
            ApplyCursor(SolarStateValue.Sun);
    }

    private void ApplyCursor(SolarStateValue state)
    {
        switch (state)
        {
            case SolarStateValue.Sun:
                Cursor.SetCursor(sunCursor, sunHotspot, cursorMode);
                break;

            case SolarStateValue.Moon:
                Cursor.SetCursor(moonCursor, moonHotspot, cursorMode);
                break;

            case SolarStateValue.Eclipse:
                Cursor.SetCursor(eclipseCursor, eclipseHotspot, cursorMode);
                break;
        }
    }
}
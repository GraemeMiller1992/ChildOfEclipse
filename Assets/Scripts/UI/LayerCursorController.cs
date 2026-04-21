using UnityEngine;
using World;

public class SolarStateCursorController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SolarState playerSolarState;

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

    private void OnEnable()
    {
        if (playerSolarState != null)
        {
            playerSolarState.OnSolarStateChanged += HandleSolarStateChanged;
            ApplyCursor(playerSolarState.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (playerSolarState != null)
        {
            playerSolarState.OnSolarStateChanged -= HandleSolarStateChanged;
        }
    }

    private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
    {
        ApplyCursor(newState);
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
using UnityEngine;
using World;

public class UIGearLayerIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SolarState solarState;
    [SerializeField] private RectTransform gear;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 720f;
    [SerializeField] private float sunAngle = 0f;
    [SerializeField] private float moonAngle = 120f;
    [SerializeField] private float eclipseAngle = 240f;
    [SerializeField] private float angleOffset = 0f;
    [SerializeField] private bool snapOnStart = true;

    private float currentAngle;
    private float targetAngle;

    private void Reset()
    {
        gear = transform as RectTransform;

        if (solarState == null)
            solarState = FindFirstObjectByType<SolarState>();
    }

    private void Awake()
    {
        if (gear == null)
            gear = transform as RectTransform;

        if (solarState == null)
            solarState = FindFirstObjectByType<SolarState>();
    }

    private void OnEnable()
    {
        if (solarState != null)
            solarState.OnSolarStateChanged += HandleSolarStateChanged;
    }

    private void OnDisable()
    {
        if (solarState != null)
            solarState.OnSolarStateChanged -= HandleSolarStateChanged;
    }

    private void Start()
    {
        targetAngle = GetAngleForState(solarState != null ? solarState.CurrentState : SolarStateValue.Sun) + angleOffset;

        if (snapOnStart)
        {
            currentAngle = targetAngle;
            ApplyRotation(currentAngle);
        }
        else
        {
            currentAngle = NormalizeAngle(gear.localEulerAngles.z);
            ApplyRotation(currentAngle);
        }
    }

    private void Update()
    {
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotateSpeed * Time.deltaTime);
        ApplyRotation(currentAngle);
    }

    private void HandleSolarStateChanged(SolarStateValue oldState, SolarStateValue newState)
    {
        targetAngle = GetAngleForState(newState) + angleOffset;
    }

    private float GetAngleForState(SolarStateValue state)
    {
        return state switch
        {
            SolarStateValue.Sun => sunAngle,
            SolarStateValue.Moon => moonAngle,
            SolarStateValue.Eclipse => eclipseAngle,
            _ => 0f
        };
    }

    private void ApplyRotation(float angle)
    {
        gear.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}
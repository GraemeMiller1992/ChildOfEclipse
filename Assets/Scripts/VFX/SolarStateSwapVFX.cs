using UnityEngine;
using World;

public class SolarStateSwapVFX : MonoBehaviour
{
    [SerializeField] private SolarState solarState;

    [Header("Swap VFX Objects")]
    [SerializeField] private GameObject eclipseToSun;
    [SerializeField] private GameObject eclipseToMoon;
    [SerializeField] private GameObject sunToEclipse;
    [SerializeField] private GameObject sunToMoon;
    [SerializeField] private GameObject moonToSun;
    [SerializeField] private GameObject moonToEclipse;

    private void Reset()
    {
        if (solarState == null)
            solarState = GetComponent<SolarState>();
    }

    private void Awake()
    {
        if (solarState == null)
            solarState = GetComponent<SolarState>();

        DisableAll();
    }

    private void OnEnable()
    {
        if (solarState == null)
            solarState = GetComponent<SolarState>();

        if (solarState != null)
            solarState.OnSolarStateChanged += OnSwap;
    }

    private void OnDisable()
    {
        if (solarState != null)
            solarState.OnSolarStateChanged -= OnSwap;
    }

    private void OnSwap(SolarStateValue oldState, SolarStateValue newState)
    {
        DisableAll();

        GameObject vfx = GetVFX(oldState, newState);
        if (vfx == null) return;

        vfx.SetActive(true);
        RestartParticles(vfx);
    }

    private void RestartParticles(GameObject root)
    {
        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            ps.gameObject.SetActive(true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void DisableAll()
    {
        DisableVFX(eclipseToSun);
        DisableVFX(eclipseToMoon);
        DisableVFX(sunToEclipse);
        DisableVFX(sunToMoon);
        DisableVFX(moonToSun);
        DisableVFX(moonToEclipse);
    }

    private void DisableVFX(GameObject vfx)
    {
        if (vfx == null) return;

        ParticleSystem[] systems = vfx.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }

        vfx.SetActive(false);
    }

    private GameObject GetVFX(SolarStateValue oldState, SolarStateValue newState)
    {
        if (oldState == SolarStateValue.Eclipse && newState == SolarStateValue.Sun) return eclipseToSun;
        if (oldState == SolarStateValue.Eclipse && newState == SolarStateValue.Moon) return eclipseToMoon;
        if (oldState == SolarStateValue.Sun && newState == SolarStateValue.Eclipse) return sunToEclipse;
        if (oldState == SolarStateValue.Sun && newState == SolarStateValue.Moon) return sunToMoon;
        if (oldState == SolarStateValue.Moon && newState == SolarStateValue.Sun) return moonToSun;
        if (oldState == SolarStateValue.Moon && newState == SolarStateValue.Eclipse) return moonToEclipse;
        return null;
    }
}
using UnityEngine;

public class PlayParticlesOnEnable : MonoBehaviour
{
    public ParticleSystem particles;

    void OnEnable()
    {
        particles.Play();
    }
}
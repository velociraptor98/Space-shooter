using UnityEngine;

// Ties a star particle layer to the scroll speed. Scaling simulation speed keeps star density
// constant while making them fly past faster; stretched layers also lengthen their streaks.
[RequireComponent(typeof(ParticleSystem))]
public class StarfieldLayer : MonoBehaviour
{
    // Streak length multiplier at cruise speed (only used by Stretched Billboard renderers).
    [SerializeField] private float streakAtCruise = 0.08f;
    // Only emit while boosting (fully at twice cruise speed), keeping cruising calm and boosts intense.
    [SerializeField] private bool onlyWhenBoosting = false;
    private ParticleSystem stars;
    private ParticleSystemRenderer starRenderer;
    private float baseRate;

    private void Awake()
    {
        stars = GetComponent<ParticleSystem>();
        starRenderer = GetComponent<ParticleSystemRenderer>();
        baseRate = stars.emission.rateOverTimeMultiplier;
    }

    private void Update()
    {
        ParticleSystem.MainModule main = stars.main;
        main.simulationSpeed = SpaceScroller.SpeedFactor;
        if (onlyWhenBoosting)
        {
            ParticleSystem.EmissionModule emission = stars.emission;
            emission.rateOverTimeMultiplier = baseRate * Mathf.Clamp01(SpaceScroller.SpeedFactor - 1.0f);
        }
        if (starRenderer.renderMode == ParticleSystemRenderMode.Stretch)
        {
            starRenderer.velocityScale = streakAtCruise * SpaceScroller.SpeedFactor;
        }
    }
}

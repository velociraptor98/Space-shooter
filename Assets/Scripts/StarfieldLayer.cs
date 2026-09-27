using UnityEngine;

// Ties a star particle layer to the scroll speed and the camera. Scaling simulation speed keeps star density
// constant while making them fly past faster; stretched layers also lengthen their streaks. The emitter stays
// just above the view as the camera moves, and the stars live in a space that follows the camera by
// (1 - depth), so far layers barely shift and near ones shift more: parallax.
[RequireComponent(typeof(ParticleSystem))]
public class StarfieldLayer : MonoBehaviour
{
    // Streak length multiplier at cruise speed (only used by Stretched Billboard renderers).
    [SerializeField] private float streakAtCruise = 0.08f;
    // How much the stars move with the world: 0 is infinitely far (fixed to the screen), 1 moves with it.
    [SerializeField] private float depth = 0.3f;
    // The layer's simulation space (the particle system's custom simulation space), moved for parallax.
    [SerializeField] private Transform anchor;
    // How far above the top of the view stars are emitted.
    [SerializeField] private float emitAbove = 2.0f;
    private ParticleSystem stars;
    private ParticleSystemRenderer starRenderer;

    private void Awake()
    {
        stars = GetComponent<ParticleSystem>();
        starRenderer = GetComponent<ParticleSystemRenderer>();
    }

    private void Update()
    {
        ParticleSystem.MainModule main = stars.main;
        main.simulationSpeed = SpaceScroller.SpeedFactor;
        if (starRenderer.renderMode == ParticleSystemRenderMode.Stretch)
        {
            starRenderer.velocityScale = streakAtCruise * SpaceScroller.SpeedFactor;
        }
    }

    private void LateUpdate()
    {
        Rect view = Playfield.View;
        transform.position = new Vector3(view.center.x, view.yMax + emitAbove, 0.0f);
        if (anchor)
        {
            Vector2 cameraPosition = Camera.main.transform.position;
            anchor.position = cameraPosition * (1.0f - depth);
        }
    }
}

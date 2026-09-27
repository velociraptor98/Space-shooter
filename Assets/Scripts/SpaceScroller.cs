using UnityEngine;

// Owns how fast the world is streaming past the ship. Background layers read Speed/SpeedFactor.
// The speed eases toward its target rather than snapping, which is what gives the sense of momentum:
// the run opens by accelerating up to cruise, and losing the ship lets it drift to a stop. It doesn't
// react to the player's movement: with free aiming, "up" is just another direction, and changing the
// scroll with it made the ship's own motion read wrongly.
public class SpaceScroller : MonoBehaviour
{
    [SerializeField] private float cruiseSpeed = 6.0f;
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float smoothTime = 0.8f;
    [SerializeField] private Movement player;
    private float acceleration;

    // World units per second the scenery should move down the screen.
    public static float Speed { get; private set; }
    // Speed relative to cruise, so layers can scale their own tuning (1 = cruising).
    public static float SpeedFactor { get; private set; }

    private void Awake()
    {
        // Start from a standstill so the scene opens by accelerating up to cruise.
        Speed = 0.0f;
        SpeedFactor = 0.0f;
        if (player == null)
        {
            player = FindAnyObjectByType<Movement>();
        }
    }

    private void Update()
    {
        float targetSpeed = player ? cruiseSpeed : driftSpeed;
        Speed = Mathf.SmoothDamp(Speed, targetSpeed, ref acceleration, smoothTime);
        SpeedFactor = Speed / cruiseSpeed;
    }
}

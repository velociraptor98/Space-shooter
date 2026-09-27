using UnityEngine;
using UnityEngine.InputSystem;

// Owns how fast the world is streaming past the ship. Background layers read Speed/SpeedFactor.
// The speed eases toward its target rather than snapping, which is what gives the sense of momentum:
// pushing forward builds up speed, pulling back bleeds it off, and losing the ship lets it drift to a stop.
public class SpaceScroller : MonoBehaviour
{
    [SerializeField] private float cruiseSpeed = 6.0f;
    [SerializeField] private float boostSpeed = 14.0f;
    [SerializeField] private float brakeSpeed = 3.0f;
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float smoothTime = 0.8f;
    [SerializeField] private Movement player;
    private InputAction moveAction;
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
        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        if (player == null)
        {
            player = FindAnyObjectByType<Movement>();
        }
    }

    private void Update()
    {
        float targetSpeed = driftSpeed;
        if (player)
        {
            float thrust = moveAction.ReadValue<Vector2>().y;
            targetSpeed = thrust >= 0.0f
                ? Mathf.Lerp(cruiseSpeed, boostSpeed, thrust)
                : Mathf.Lerp(cruiseSpeed, brakeSpeed, -thrust);
        }
        Speed = Mathf.SmoothDamp(Speed, targetSpeed, ref acceleration, smoothTime);
        SpeedFactor = Speed / cruiseSpeed;
    }
}

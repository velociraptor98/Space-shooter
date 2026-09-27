using UnityEngine;

public enum FlightPattern
{
    Straight,   // Flies straight across the screen.
    Weave,      // Smooth side-to-side sine wave; staggered spawns from one point form a snake.
    ZigZag,     // Sharp diagonal switchbacks.
    Swoop,      // Flies in, then banks hard into a sideways arc and exits to one side.
    Dive,       // Stops a little way in, tracks the player briefly, then dives at them.
    Hold        // Flies in, holds station near its entry edge while it fires, then retreats.
}

// How an enemy flies. Health and death live in Enemy, and its bullets in BulletEmitter.
// Patterns are worked out in a "path frame" where the ship always flies down the screen, then turned to
// face the ship's entry direction - so every pattern works whichever edge it comes in from.
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 8.0f;
    [SerializeField] private FlightPattern pattern = FlightPattern.Straight;
    [SerializeField] private float weaveAmplitude = 2.5f;
    [SerializeField] private float weaveFrequency = 2.5f;
    [SerializeField] private float zigZagInterval = 0.6f;
    [SerializeField] private float swoopTurnRate = 110.0f;
    [SerializeField] private float swoopMaxAngle = 75.0f;
    [SerializeField] private float diveHoverTime = 0.8f;
    [SerializeField] private float diveSpeedMultiplier = 1.8f;
    // How far in from the arena edge it entered by a Hold ship parks, a Dive ship hovers, and a Swoop turns.
    // The arena extends past the screen, so these reach in far enough to be seen from most of it.
    [SerializeField] private Vector2 holdDepthRange = new Vector2(7.0f, 9.0f);
    [SerializeField] private Vector2 hoverDepthRange = new Vector2(6.0f, 10.0f);
    [SerializeField] private Vector2 swoopDepthRange = new Vector2(6.0f, 10.0f);
    [SerializeField] private float holdDuration = 7.0f;
    [SerializeField] private float maxBankAngle = 35.0f;
    [SerializeField] private float bankSmoothing = 8.0f;
    // Gentle side-to-side drift on otherwise straight or hovering flight, so no ship sits dead still.
    [SerializeField] private float swayAmplitude = 0.5f;
    [SerializeField] private float swayFrequency = 2.0f;

    // Turns path-frame vectors (flying "down") into world space.
    private Quaternion heading = Quaternion.identity;
    // How far along the entry direction the screen edge it came in by lies, for measuring depth.
    private float entryEdge;
    private float direction = 1.0f;
    private float pathTime;
    private float swoopAngle;
    private float swoopStartDepth;
    private float hoverDepth;
    private float hoverTimer;
    private Vector3 diveVelocity;
    private bool isDiving;
    private bool isRetreating;

    // Called by the SpawnManager straight after instantiating, before the first Update.
    // forward: the direction the ship enters the screen in (down for ships coming from the top).
    public void Launch(FlightPattern flightPattern, float flightDirection, Vector2 forward)
    {
        pattern = flightPattern;
        direction = Mathf.Sign(flightDirection);
        // Rotate about Z only: a 180-degree FromToRotation may pick another axis and flip the sprite.
        heading = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.down, forward));
        ResetPath();
        transform.rotation = heading;
    }

    public void Launch(FlightPattern flightPattern, float flightDirection)
    {
        Launch(flightPattern, flightDirection, Vector2.down);
    }

    private void Start()
    {
        if (pathTime == 0.0f)
        {
            ResetPath();
        }
    }

    private void Update()
    {
        pathTime += Time.deltaTime;
        Vector3 pathVelocity = PathVelocity();
        transform.position += heading * pathVelocity * Time.deltaTime;
        Bank(pathVelocity);

        // Ships that have flown their path leave for good rather than looping, keeping the screen readable.
        Rect bounds = Playfield.Bounds;
        Vector3 position = transform.position;
        bool leftScreen = position.x < bounds.xMin - 3.0f || position.x > bounds.xMax + 3.0f
            || position.y < bounds.yMin - 3.0f || position.y > bounds.yMax + 3.0f;
        if (leftScreen)
        {
            Destroy(gameObject);
        }
    }

    private void ResetPath()
    {
        // The entry edge is the first part of the screen met travelling forward: the corner with the smallest
        // projection onto the forward direction.
        Vector3 forward = heading * Vector3.down;
        Rect bounds = Playfield.Bounds;
        entryEdge = Mathf.Min(Mathf.Min(Vector2.Dot(bounds.min, forward), Vector2.Dot(bounds.max, forward)),
                              Mathf.Min(Vector2.Dot(new Vector2(bounds.xMin, bounds.yMax), forward),
                                        Vector2.Dot(new Vector2(bounds.xMax, bounds.yMin), forward)));
        pathTime = 0.0f;
        swoopAngle = 0.0f;
        swoopStartDepth = Random.Range(swoopDepthRange.x, swoopDepthRange.y);
        Vector2 depthRange = pattern == FlightPattern.Hold ? holdDepthRange : hoverDepthRange;
        hoverDepth = Random.Range(depthRange.x, depthRange.y);
        hoverTimer = pattern == FlightPattern.Hold ? holdDuration : diveHoverTime;
        isDiving = false;
        isRetreating = false;
    }

    // How far the ship has come in past its entry edge.
    private float Depth()
    {
        return Vector2.Dot(transform.position, heading * Vector3.down) - entryEdge;
    }

    // A world-space offset expressed in the path frame (x = sideways, -y = forward).
    private Vector3 ToPath(Vector3 world)
    {
        return Quaternion.Inverse(heading) * world;
    }

    private Vector3 PathVelocity()
    {
        switch (pattern)
        {
            case FlightPattern.Weave:
                // Derivative of x = A * sin(f * t), so the path is a clean sine wave.
                float weaveX = weaveAmplitude * weaveFrequency * Mathf.Cos(weaveFrequency * pathTime) * direction;
                return new Vector3(weaveX, -speed, 0.0f);

            case FlightPattern.ZigZag:
                float leg = Mathf.Floor(pathTime / zigZagInterval);
                float zigSide = (leg % 2.0f == 0.0f ? 1.0f : -1.0f) * direction;
                return new Vector3(zigSide * speed * 0.75f, -speed * 0.75f, 0.0f);

            case FlightPattern.Swoop:
                if (Depth() >= swoopStartDepth)
                {
                    swoopAngle = Mathf.Min(swoopAngle + swoopTurnRate * Time.deltaTime, swoopMaxAngle);
                }
                float swoopRadians = swoopAngle * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(swoopRadians) * direction, -Mathf.Cos(swoopRadians), 0.0f) * speed;

            case FlightPattern.Dive:
                return DiveVelocity();

            case FlightPattern.Hold:
                return HoldVelocity();

            default:
                return new Vector3(Sway(), -speed, 0.0f);
        }
    }

    private Vector3 DiveVelocity()
    {
        if (isDiving)
        {
            return diveVelocity;
        }
        if (Depth() < hoverDepth)
        {
            return Vector3.down * speed;
        }

        Movement player = Movement.Instance;
        Vector3 toPlayer = player ? ToPath(player.transform.position - transform.position) : Vector3.down;
        hoverTimer -= Time.deltaTime;
        if (hoverTimer <= 0.0f)
        {
            // Lock onto where the player is right now and commit to it.
            isDiving = true;
            diveVelocity = toPlayer.normalized * speed * diveSpeedMultiplier;
            // Always keep coming forward, even if the player is behind the hover line.
            diveVelocity.y = Mathf.Min(diveVelocity.y, -speed * 0.5f);
            return diveVelocity;
        }

        // Hover: slide sideways to line up with the player, bobbing while it waits.
        float trackX = Mathf.Clamp(toPlayer.x, -1.0f, 1.0f);
        float bob = swayAmplitude * swayFrequency * 2.0f * Mathf.Cos(swayFrequency * 2.0f * pathTime);
        return new Vector3(trackX * speed * 0.4f, bob, 0.0f);
    }

    private Vector3 HoldVelocity()
    {
        if (isRetreating)
        {
            return Vector3.up * speed;
        }
        float remaining = hoverDepth - Depth();
        if (remaining > 0.0f)
        {
            // Ease into position so a heavy settles rather than stopping dead.
            return Vector3.down * Mathf.Min(speed, remaining * 3.0f + 0.5f);
        }

        hoverTimer -= Time.deltaTime;
        if (hoverTimer <= 0.0f)
        {
            isRetreating = true;
        }
        // Slow, wide drift along the edge while it fires.
        float drift = 3.0f * 0.5f * Mathf.Cos(0.5f * pathTime) * direction;
        return new Vector3(drift, 0.0f, 0.0f);
    }

    // Sideways velocity of a gentle sine drift (derivative of A * sin(f * t)).
    private float Sway()
    {
        return swayAmplitude * swayFrequency * Mathf.Cos(swayFrequency * pathTime) * direction;
    }

    // Faces the ship along its heading, tilted towards its sideways movement so turns read clearly.
    private void Bank(Vector3 pathVelocity)
    {
        // The sprite faces down the path, so measure from straight down. Treating forward speed as at least
        // half cruising speed keeps hovering ships tilting into sideways slides and retreats level.
        float forward = Mathf.Max(-pathVelocity.y, speed * 0.5f);
        float bankAngle = Mathf.Clamp(Mathf.Atan2(pathVelocity.x, forward) * Mathf.Rad2Deg, -maxBankAngle, maxBankAngle);
        Quaternion targetRotation = heading * Quaternion.Euler(0.0f, 0.0f, bankAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bankSmoothing * Time.deltaTime);
    }
}

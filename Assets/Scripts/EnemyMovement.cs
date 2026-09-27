using UnityEngine;

public enum FlightPattern
{
    Straight,   // Falls straight down.
    Weave,      // Smooth side-to-side sine wave; staggered spawns from one point form a snake.
    ZigZag,     // Sharp diagonal switchbacks.
    Swoop,      // Drops in, then banks hard into a sideways arc and exits the side of the screen.
    Dive,       // Drops to a hover height, tracks the player briefly, then dives at them.
    Hold        // Flies in, holds station near the top while it fires, then retreats back up.
}

// How an enemy flies. Health and death live in Enemy, and its bullets in BulletEmitter.
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
    // How far below the top of the screen a Hold ship parks, and a Dive ship hovers.
    [SerializeField] private Vector2 holdDepthRange = new Vector2(2.5f, 4.0f);
    [SerializeField] private Vector2 hoverDepthRange = new Vector2(2.5f, 6.0f);
    [SerializeField] private float holdDuration = 7.0f;
    [SerializeField] private float maxBankAngle = 35.0f;
    [SerializeField] private float bankSmoothing = 8.0f;
    // Gentle side-to-side drift on otherwise straight or hovering flight, so no ship sits dead still.
    [SerializeField] private float swayAmplitude = 0.5f;
    [SerializeField] private float swayFrequency = 2.0f;

    private float direction = 1.0f;
    private float pathTime;
    private float swoopAngle;
    private float swoopStartY;
    private float hoverY;
    private float hoverTimer;
    private Vector3 diveVelocity;
    private bool isDiving;
    private bool isRetreating;

    // Called by the SpawnManager straight after instantiating, before the first Update.
    public void Launch(FlightPattern flightPattern, float flightDirection)
    {
        pattern = flightPattern;
        direction = Mathf.Sign(flightDirection);
        ResetPath();
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
        Vector3 velocity = PathVelocity();
        transform.position += velocity * Time.deltaTime;
        Bank(velocity);

        // Ships that have flown their path leave for good rather than looping, keeping the screen readable.
        Vector3 position = transform.position;
        bool leftScreen = position.y < Playfield.Bottom - 2.0f
            || (isRetreating && position.y > Playfield.Top + 2.5f)
            || position.x < Playfield.Left - 3.0f
            || position.x > Playfield.Right + 3.0f;
        if (leftScreen)
        {
            Destroy(gameObject);
        }
    }

    private void ResetPath()
    {
        pathTime = 0.0f;
        swoopAngle = 0.0f;
        swoopStartY = Playfield.Top - Random.Range(3.5f, 7.0f);
        Vector2 depth = pattern == FlightPattern.Hold ? holdDepthRange : hoverDepthRange;
        hoverY = Playfield.Top - Random.Range(depth.x, depth.y);
        hoverTimer = pattern == FlightPattern.Hold ? holdDuration : diveHoverTime;
        isDiving = false;
        isRetreating = false;
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
                if (transform.position.y <= swoopStartY)
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
        if (transform.position.y > hoverY)
        {
            return Vector3.down * speed;
        }

        Movement player = Movement.Instance;
        hoverTimer -= Time.deltaTime;
        if (hoverTimer <= 0.0f)
        {
            // Lock onto where the player is right now and commit to it.
            isDiving = true;
            Vector3 target = player ? player.transform.position : transform.position + Vector3.down;
            diveVelocity = (target - transform.position).normalized * speed * diveSpeedMultiplier;
            // Never dive upwards, even if the player has flown above the hover line.
            diveVelocity.y = Mathf.Min(diveVelocity.y, -speed * 0.5f);
            return diveVelocity;
        }

        // Hover: slide sideways to line up with the player, bobbing while it waits.
        float trackX = player ? Mathf.Clamp(player.transform.position.x - transform.position.x, -1.0f, 1.0f) : 0.0f;
        float bob = swayAmplitude * swayFrequency * 2.0f * Mathf.Cos(swayFrequency * 2.0f * pathTime);
        return new Vector3(trackX * speed * 0.4f, bob, 0.0f);
    }

    private Vector3 HoldVelocity()
    {
        if (isRetreating)
        {
            return Vector3.up * speed;
        }
        if (transform.position.y > hoverY)
        {
            // Ease into position so a heavy settles rather than stopping dead.
            float remaining = transform.position.y - hoverY;
            return Vector3.down * Mathf.Min(speed, remaining * 3.0f + 0.5f);
        }

        hoverTimer -= Time.deltaTime;
        if (hoverTimer <= 0.0f)
        {
            isRetreating = true;
        }
        // Slow, wide drift across the top of the screen while it fires.
        float drift = 3.0f * 0.5f * Mathf.Cos(0.5f * pathTime) * direction;
        return new Vector3(drift, 0.0f, 0.0f);
    }

    // Horizontal velocity of a gentle sine drift (derivative of A * sin(f * t)).
    private float Sway()
    {
        return swayAmplitude * swayFrequency * Mathf.Cos(swayFrequency * pathTime) * direction;
    }

    // Tilts the ship's nose towards its direction of travel so turns read clearly.
    private void Bank(Vector3 velocity)
    {
        // The sprite faces down, so measure the heading from straight down. Treating forward speed as at
        // least half cruising speed keeps hovering ships tilting into sideways slides and retreats level.
        float forward = Mathf.Max(-velocity.y, speed * 0.5f);
        float targetAngle = Mathf.Clamp(Mathf.Atan2(velocity.x, forward) * Mathf.Rad2Deg, -maxBankAngle, maxBankAngle);
        Quaternion targetRotation = Quaternion.Euler(0.0f, 0.0f, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bankSmoothing * Time.deltaTime);
    }
}

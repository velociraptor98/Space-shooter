using UnityEngine;

public enum FirePattern
{
    Aimed,   // A fan of `count` bullets centred on the player.
    Ring,    // `count` bullets evenly around a full circle.
    Spiral   // `count` arms rotating by `spinSpeed`, fired as a continuous stream.
}

// Fires an enemy's bullet pattern while it is on screen. A volley is `shotsPerVolley` shots spaced
// `shotGap` apart; volleys repeat every `volleyInterval`. Spirals ignore volleys and stream constantly.
public class BulletEmitter : MonoBehaviour
{
    [SerializeField] private FirePattern pattern = FirePattern.Aimed;
    [SerializeField] private BulletKind bullet = BulletKind.Orb;
    [SerializeField] private float bulletSpeed = 5.0f;
    [SerializeField] private float bulletAcceleration = 0.0f;
    [SerializeField] private int count = 1;
    [SerializeField] private float spreadAngle = 0.0f;
    [SerializeField] private int shotsPerVolley = 1;
    [SerializeField] private float shotGap = 0.1f;
    [SerializeField] private float volleyInterval = 2.0f;
    [SerializeField] private float spinSpeed = 60.0f;
    // Curves each bullet as it flies (degrees per second), for sweeping spiral arms.
    [SerializeField] private float bulletTurnRate = 0.0f;
    [SerializeField] private float firstVolleyDelay = 0.6f;
    // Where bullets leave the ship, relative to its centre in its own space (the sprite faces down).
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.0f, -0.8f);
    private float nextShot;
    private int shotsLeft;
    private float spiralAngle;

    private void OnEnable()
    {
        nextShot = Time.time + firstVolleyDelay;
        shotsLeft = shotsPerVolley;
        spiralAngle = Random.Range(0.0f, 360.0f);
    }

    private void Update()
    {
        if (Time.time < nextShot || !IsOnScreen())
        {
            return;
        }

        Vector2 origin = transform.position + transform.rotation * (Vector3)muzzleOffset;
        switch (pattern)
        {
            case FirePattern.Aimed:
                FireFan(origin, AimAngle(origin), spreadAngle);
                break;
            case FirePattern.Ring:
                FireFan(origin, Random.Range(0.0f, 360.0f), 360.0f);
                break;
            case FirePattern.Spiral:
                FireFan(origin, spiralAngle, 360.0f);
                spiralAngle += spinSpeed * shotGap;
                nextShot = Time.time + shotGap;
                return;
        }

        --shotsLeft;
        if (shotsLeft > 0)
        {
            nextShot = Time.time + shotGap;
        }
        else
        {
            shotsLeft = shotsPerVolley;
            nextShot = Time.time + volleyInterval;
        }
    }

    // Fires `count` bullets centred on `centerAngle` (degrees, 0 = right). A full 360 spread spaces
    // them evenly around the circle; anything less spans the arc edge to edge.
    private void FireFan(Vector2 origin, float centerAngle, float spread)
    {
        bool fullCircle = spread >= 360.0f;
        float step = count > 1 ? spread / (fullCircle ? count : count - 1) : 0.0f;
        float start = fullCircle ? centerAngle : centerAngle - spread * 0.5f;
        for (int i = 0; i < count; ++i)
        {
            float angle = (start + step * i) * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * bulletSpeed;
            BulletSystem.Fire(bullet, origin, velocity, bulletAcceleration, bulletTurnRate);
        }
    }

    private float AimAngle(Vector2 origin)
    {
        Movement player = Movement.Instance;
        Vector2 toPlayer = player ? (Vector2)player.transform.position - origin : Vector2.down;
        return Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
    }

    // Holds fire until the ship is properly in view, so bullets never arrive from off-screen.
    private bool IsOnScreen()
    {
        Camera cam = Camera.main;
        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        return viewport.x > 0.02f && viewport.x < 0.98f && viewport.y > 0.1f && viewport.y < 0.92f;
    }
}

using System.Collections.Generic;
using UnityEngine;

public enum BulletKind
{
    Orb,
    Pellet,
    Rice
}

// Runs every enemy bullet in one place. Bullets are pooled sprites moved in a single loop and tested
// against the player's small hitbox by distance, which stays cheap with hundreds on screen and gives
// the precise, circle-vs-circle hits a bullet hell needs.
public class BulletSystem : Singleton<BulletSystem>
{
    [System.Serializable]
    private struct BulletStyle
    {
        public Sprite sprite;
        // Collision radius in world units. Deliberately smaller than the sprite, as is genre convention.
        public float radius;
        // Rotate the sprite to face its direction of travel (for elongated bullets).
        public bool faceVelocity;
    }

    private class Bullet
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 position;
        public Vector2 velocity;
        public float acceleration;
        public float turnRate;
        public float radius;
        public bool faceVelocity;
    }

    private struct Rock
    {
        public Vector2 position;
        public float radius;
    }

    // Indexed by BulletKind.
    [SerializeField] private BulletStyle[] styles;
    [SerializeField] private int prewarm = 400;
    [SerializeField] private int sortingOrder = 4;
    [SerializeField] private float offscreenMargin = 1.0f;
    [SerializeField] private GameObject clearSparks;
    private readonly List<Bullet> active = new List<Bullet>();
    private readonly Stack<Bullet> pool = new Stack<Bullet>();
    private readonly List<Rock> rocks = new List<Rock>();
    private ParticleSystem clearSparkSystem;
    private int sparksPerBurst;
    private Rect bounds;
    // Where the player was last frame, so contact can be tested along the whole of this frame's movement.
    private Vector2 lastPlayerPosition;
    private bool hadPlayer;

    // velocity: world units per second. acceleration: speed change per second along the heading.
    // turnRate: degrees per second the heading curves by (for sweeping, spiralling streams).
    public static void Fire(BulletKind kind, Vector2 position, Vector2 velocity, float acceleration = 0.0f, float turnRate = 0.0f)
    {
        if (Instance)
        {
            Instance.Spawn(kind, position, velocity, acceleration, turnRate);
        }
    }

    // Removes every bullet on screen - used as a mercy clear when the player is hit.
    public static void Clear()
    {
        if (Instance)
        {
            Instance.ClearAll();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        for (int i = 0; i < prewarm; ++i)
        {
            pool.Push(CreateBullet());
        }
        if (clearSparks)
        {
            CreateClearSparks();
        }
    }

    private void CreateClearSparks()
    {
        clearSparkSystem = Instantiate(clearSparks, transform).GetComponent<ParticleSystem>();
        // Discard the burst it would play on waking, then keep it running with nothing emitting on its own.
        clearSparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = clearSparkSystem.main;
        main.loop = true;
        main.stopAction = ParticleSystemStopAction.None;
        // Sparks land all over the arena, including off screen, and must play out rather than wait to be seen.
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = Mathf.Max(main.maxParticles, 4000);
        ParticleSystem.EmissionModule emission = clearSparkSystem.emission;
        for (int i = 0; i < emission.burstCount; ++i)
        {
            sparksPerBurst += (int)emission.GetBurst(i).count.constantMax;
        }
        emission.enabled = false;
        clearSparkSystem.Play();
    }

    private Bullet CreateBullet()
    {
        var go = new GameObject("Bullet");
        go.transform.SetParent(transform, false);
        var bullet = new Bullet { transform = go.transform, renderer = go.AddComponent<SpriteRenderer>() };
        bullet.renderer.sortingOrder = sortingOrder;
        go.SetActive(false);
        return bullet;
    }

    private void Spawn(BulletKind kind, Vector2 position, Vector2 velocity, float acceleration, float turnRate)
    {
        BulletStyle style = styles[(int)kind];
        Bullet bullet = pool.Count > 0 ? pool.Pop() : CreateBullet();
        bullet.position = position;
        bullet.velocity = velocity;
        bullet.acceleration = acceleration;
        bullet.turnRate = turnRate;
        bullet.radius = style.radius;
        bullet.faceVelocity = style.faceVelocity;
        bullet.renderer.sprite = style.sprite;
        bullet.transform.SetPositionAndRotation(position, Rotation(bullet));
        bullet.transform.gameObject.SetActive(true);
        active.Add(bullet);
    }

    private void Update()
    {
        Rect view = Playfield.Bounds;
        bounds = new Rect(view.xMin - offscreenMargin, view.yMin - offscreenMargin,
                          view.width + offscreenMargin * 2.0f, view.height + offscreenMargin * 2.0f);

        Movement player = Movement.Instance;
        bool canHit = player && player.IsVulnerable;
        // Mid-roll, a bullet that would have hit sails through instead and counts as a dodge.
        bool canDodge = player && player.IsRolling;
        Vector2 playerPosition = player ? (Vector2)player.transform.position : Vector2.zero;
        Vector2 lastPlayer = hadPlayer ? lastPlayerPosition : playerPosition;
        lastPlayerPosition = playerPosition;
        hadPlayer = player;
        float dt = Time.deltaTime;

        rocks.Clear();
        IReadOnlyList<Asteroid> asteroids = Asteroid.Active;
        for (int i = 0; i < asteroids.Count; ++i)
        {
            rocks.Add(new Rock { position = asteroids[i].transform.position, radius = asteroids[i].Radius });
        }

        for (int i = active.Count - 1; i >= 0; --i)
        {
            Bullet bullet = active[i];
            if (bullet.turnRate != 0.0f)
            {
                bullet.velocity = Quaternion.Euler(0.0f, 0.0f, bullet.turnRate * dt) * bullet.velocity;
            }
            if (bullet.acceleration != 0.0f)
            {
                float speed = Mathf.Max(0.5f, bullet.velocity.magnitude + bullet.acceleration * dt);
                bullet.velocity = bullet.velocity.normalized * speed;
            }
            Vector2 previous = bullet.position;
            bullet.position += bullet.velocity * dt;
            bullet.transform.SetPositionAndRotation(bullet.position, Rotation(bullet));

            if (HitsAsteroid(bullet))
            {
                Recycle(i);
                continue;
            }
            // Before the bounds check: in a slow frame a bullet can cross the ship and leave the arena at once.
            if ((canHit || canDodge) && Touches(previous - lastPlayer, bullet.position - playerPosition, bullet.radius + player.HitboxRadius))
            {
                if (canHit)
                {
                    Recycle(i);
                    player.OnDamage();
                    // OnDamage clears the screen, so stop walking a list that has just been emptied.
                    return;
                }
                player.OnDodge();
            }
            if (!bounds.Contains(bullet.position))
            {
                Recycle(i);
            }
        }
    }

    // Whether a bullet came within `reach` of the player at any point this frame. Tested on the bullet's path
    // relative to the player (from/to), so a fast ship or bullet, or a slow frame, can't skip past a hit.
    private static bool Touches(Vector2 from, Vector2 to, float reach)
    {
        Vector2 path = to - from;
        float length = path.sqrMagnitude;
        float t = length > 0.0f ? Mathf.Clamp01(-Vector2.Dot(from, path) / length) : 0.0f;
        return (from + path * t).sqrMagnitude <= reach * reach;
    }

    // Asteroids soak up enemy fire, so they double as cover.
    private bool HitsAsteroid(Bullet bullet)
    {
        for (int i = 0; i < rocks.Count; ++i)
        {
            float reach = rocks[i].radius + bullet.radius;
            if ((bullet.position - rocks[i].position).sqrMagnitude <= reach * reach)
            {
                return true;
            }
        }
        return false;
    }

    private static Quaternion Rotation(Bullet bullet)
    {
        if (!bullet.faceVelocity)
        {
            return Quaternion.identity;
        }
        // Sprites are drawn pointing up.
        float angle = Mathf.Atan2(bullet.velocity.y, bullet.velocity.x) * Mathf.Rad2Deg - 90.0f;
        return Quaternion.Euler(0.0f, 0.0f, angle);
    }

    private void Recycle(int index)
    {
        Bullet bullet = active[index];
        bullet.transform.gameObject.SetActive(false);
        active[index] = active[active.Count - 1];
        active.RemoveAt(active.Count - 1);
        pool.Push(bullet);
    }

    private void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; --i)
        {
            if (clearSparkSystem && i % 4 == 0)
            {
                var emit = new ParticleSystem.EmitParams { position = active[i].position, applyShapeToPosition = true };
                clearSparkSystem.Emit(emit, sparksPerBurst);
            }
            Recycle(i);
        }
    }
}

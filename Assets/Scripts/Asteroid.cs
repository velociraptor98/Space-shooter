using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A drifting rock. It hurts whatever it hits - the player (unless they're mid-roll) and enemies alike -
// soaks up enemy bullets (see BulletSystem), and breaks into smaller pieces when shot apart.
public class Asteroid : MonoBehaviour
{
    // One is picked at random, so no two rocks in a field look alike.
    [SerializeField] private Sprite[] variants;
    [SerializeField] private int maxHealth = 4;
    // Damage dealt to an enemy that flies into it.
    [SerializeField] private int enemyDamage = 3;
    [SerializeField] private int scoreValue = 10;
    // The smaller rock this breaks into, and how many.
    [SerializeField] private GameObject fragment;
    [SerializeField] private int fragmentCount = 2;
    [SerializeField] private float fragmentSpeed = 3.0f;
    [SerializeField] private GameObject explosion;
    [SerializeField] private float explosionScale = 1.0f;
    [SerializeField] private GameObject hitSparks;
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float breakShake = 0.25f;
    private static readonly List<Asteroid> active = new List<Asteroid>();
    private Vector2 velocity;
    private int health;
    private bool isBroken;
    private SpriteRenderer body;
    private Material normalMaterial;
    private Coroutine flashing;

    // Every asteroid in play, for BulletSystem to test enemy bullets against.
    public static IReadOnlyList<Asteroid> Active => active;
    // Collision radius in world units.
    public float Radius { get; private set; }

    // Called by whatever spawns the rock, straight after instantiating it.
    public void Launch(Vector2 launchVelocity)
    {
        velocity = launchVelocity;
    }

    private void Awake()
    {
        health = maxHealth;
        body = GetComponent<SpriteRenderer>();
        normalMaterial = body.sharedMaterial;
        if (variants.Length > 0)
        {
            body.sprite = variants[Random.Range(0, variants.Length)];
        }
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        Radius = circle.radius * transform.lossyScale.x;
    }

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        // Rocks spawn just outside the arena, so only clear them once they're well clear of it.
        Rect bounds = Playfield.Bounds;
        Vector2 position = transform.position;
        if (position.x < bounds.xMin - 4.0f || position.x > bounds.xMax + 4.0f
            || position.y < bounds.yMin - 4.0f || position.y > bounds.yMax + 4.0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken)
        {
            return;
        }
        if (other.CompareTag("Bullet"))
        {
            PoolManager.Despawn(other.gameObject);
            TakeDamage(1, true);
        }
        else if (other.CompareTag("Player"))
        {
            Movement player = other.GetComponent<Movement>();
            if (player && player.IsVulnerable)
            {
                player.OnDamage();
                // The rock shatters on the hull.
                Break(false);
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy)
            {
                enemy.TakeDamage(enemyDamage, false);
                TakeDamage(1, false);
            }
        }
    }

    public void TakeDamage(int amount, bool byPlayer)
    {
        health -= amount;
        if (health <= 0)
        {
            Break(byPlayer);
            return;
        }
        if (flashMaterial)
        {
            if (flashing != null)
            {
                StopCoroutine(flashing);
            }
            flashing = StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        body.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(0.05f);
        body.sharedMaterial = normalMaterial;
        flashing = null;
    }

    private void Break(bool byPlayer)
    {
        isBroken = true;
        if (byPlayer && Movement.Instance)
        {
            Movement.Instance.AddScore(scoreValue);
        }
        if (fragment)
        {
            // Pieces fly apart evenly around the break, carrying some of the rock's own drift.
            float start = Random.Range(0.0f, 360.0f);
            for (int i = 0; i < fragmentCount; ++i)
            {
                float angle = (start + 360.0f * i / fragmentCount) * Mathf.Deg2Rad;
                Vector2 away = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                GameObject piece = Instantiate(fragment, (Vector2)transform.position + away * Radius * 0.5f, Quaternion.identity);
                piece.GetComponent<Asteroid>().Launch(velocity * 0.6f + away * fragmentSpeed);
            }
        }
        if (explosion)
        {
            GameObject blast = PoolManager.Spawn(explosion, transform.position, Quaternion.identity);
            blast.transform.localScale *= explosionScale;
        }
        if (hitSparks)
        {
            PoolManager.Spawn(hitSparks, transform.position, Quaternion.identity);
        }
        AudioSource source = GetComponent<AudioSource>();
        if (source)
        {
            SoundEffects.Play(source.clip, source.volume);
        }
        GameFeel.Impact(breakShake);
        Destroy(gameObject);
    }
}

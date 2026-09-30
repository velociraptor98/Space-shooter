using System.Collections;
using UnityEngine;

// An enemy's health and what happens when it is hit or destroyed. Flight lives in EnemyMovement and
// its bullet pattern in BulletEmitter; this switches both off when the ship goes down.
public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private GameObject hitSparks;
    // Swapped in for a frame when hit, so damage reads even on tough enemies.
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashTime = 0.05f;
    [SerializeField] private float deathShake = 0.3f;
    [SerializeField] private float deathFreeze = 0.04f;
    // The shared explosion, sized to the ship.
    [SerializeField] private GameObject deathExplosion;
    [SerializeField] private float explosionScale = 1.0f;
    private int health;
    private bool isDestroyed;
    private SpriteRenderer body;
    private Material normalMaterial;
    private Coroutine flashing;

    private void Awake()
    {
        health = maxHealth;
        body = GetComponent<SpriteRenderer>();
        normalMaterial = body.sharedMaterial;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed)
        {
            return;
        }
        if (other.CompareTag("Bullet"))
        {
            PoolManager.Despawn(other.gameObject);
            TakeDamage(1);
        }
        else if (other.CompareTag("Player"))
        {
            Movement player = other.GetComponent<Movement>();
            if (player && player.IsVulnerable)
            {
                player.OnDamage();
                Die(false);
            }
            else if (player && player.IsRolling)
            {
                player.OnDodge();
            }
        }
    }

    // byPlayer: whether the player gets the score if this finishes the ship off (not for asteroid hits).
    public void TakeDamage(int amount, bool byPlayer = true)
    {
        health -= amount;
        if (health <= 0)
        {
            Die(byPlayer);
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

    // Holds the ship where it is, guns silent (while the player's death plays out).
    public void Freeze()
    {
        GetComponent<EnemyMovement>().enabled = false;
        foreach (BulletEmitter emitter in GetComponents<BulletEmitter>())
        {
            emitter.enabled = false;
        }
        Rigidbody2D rigidbody = GetComponent<Rigidbody2D>();
        if (rigidbody)
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0.0f;
        }
    }

    // Blows the ship up without scoring, caught in the player's final blast. sortingBoost lifts its explosion
    // by that many sorting orders, to draw above the death sequence's blackout.
    public void Detonate(int sortingBoost)
    {
        if (!isDestroyed)
        {
            Die(false, sortingBoost);
        }
    }

    private IEnumerator Flash()
    {
        body.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(flashTime);
        body.sharedMaterial = normalMaterial;
        flashing = null;
    }

    private void Die(bool killedByPlayer, int sortingBoost = 0)
    {
        isDestroyed = true;
        body.sharedMaterial = normalMaterial;
        if (killedByPlayer && Movement.Instance)
        {
            Movement.Instance.AddScore(scoreValue);
        }

        GetComponent<EnemyMovement>().enabled = false;
        foreach (BulletEmitter emitter in GetComponents<BulletEmitter>())
        {
            emitter.enabled = false;
        }
        // Leave the exhaust trails behind to fade out on their own rather than vanishing with the ship.
        foreach (ParticleSystem exhaust in GetComponentsInChildren<ParticleSystem>())
        {
            exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            exhaust.transform.SetParent(null, true);
            Destroy(exhaust.gameObject, exhaust.main.startLifetime.constantMax);
        }

        AudioSource source = GetComponent<AudioSource>();
        if (source)
        {
            SoundEffects.Play(source.clip, source.volume);
        }
        if (deathExplosion)
        {
            GameObject explosion = PoolManager.Spawn(deathExplosion, transform.position, Quaternion.identity);
            explosion.transform.localScale *= explosionScale;
            DeathSequence.Lift(explosion, sortingBoost);
        }
        if (hitSparks)
        {
            DeathSequence.Lift(PoolManager.Spawn(hitSparks, transform.position, Quaternion.identity), sortingBoost);
        }
        GameFeel.Impact(deathShake, deathFreeze);
        Destroy(gameObject);
    }
}

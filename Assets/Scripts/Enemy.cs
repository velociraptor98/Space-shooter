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
    [SerializeField] private float deathAnimationLength = 2.8f;
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
            Destroy(other.gameObject);
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
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die(true);
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
        yield return new WaitForSeconds(flashTime);
        body.sharedMaterial = normalMaterial;
        flashing = null;
    }

    private void Die(bool killedByPlayer)
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
        Destroy(GetComponent<Collider2D>());
        foreach (ParticleSystem exhaust in GetComponentsInChildren<ParticleSystem>())
        {
            exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        GetComponent<Animator>().SetTrigger("OnEnemyDeath");
        GetComponent<AudioSource>().Play();
        if (hitSparks)
        {
            Instantiate(hitSparks, transform.position, Quaternion.identity);
        }
        GameFeel.Impact(deathShake, deathFreeze);
        Destroy(gameObject, deathAnimationLength);
    }
}

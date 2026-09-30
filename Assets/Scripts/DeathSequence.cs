using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The player's death, played as a finale. Everything stops; the camera pans to centre the stricken ship while
// the world fades to black around it, leaving only the ship and the enemies lit. The ship shudders, then
// blows, and a shockwave rolls out from it, setting off each enemy as it reaches them. Then the game-over menu.
public class DeathSequence : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraRig;
    [SerializeField] private CanvasGroup hud;
    [SerializeField] private GameOverMenu gameOverMenu;

    [Header("Blackout")]
    [SerializeField] private Sprite curtainSprite;
    [SerializeField] private Color curtainColor = new Color32(26, 28, 44, 255);
    // Above the world, below the ships, which are lifted by spotlightBoost for the sequence.
    [SerializeField] private int curtainOrder = 50;
    [SerializeField] private int spotlightBoost = 100;
    [SerializeField] private float panTime = 0.8f;
    [SerializeField] private float fadeTime = 0.8f;
    [SerializeField] private int fadeSteps = 6;

    [Header("Explosion")]
    // How long the ship shudders before it goes.
    [SerializeField] private float holdTime = 0.7f;
    [SerializeField] private float pixelsPerUnit = 17.0f;
    [SerializeField] private GameObject explosion;
    [SerializeField] private float explosionScale = 3.0f;
    [SerializeField] private GameObject sparks;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private float flashTime = 0.06f;
    [SerializeField] private Color flashColor = new Color32(244, 244, 244, 255);

    [Header("Shockwave")]
    [SerializeField] private Material ringMaterial;
    [SerializeField] private float ringSpeed = 16.0f;
    [SerializeField] private int ringParticles = 120;
    // Bursts a moment apart, so the wave is a thick band rather than a single line of pixels.
    [SerializeField] private int ringBands = 3;
    [SerializeField] private float bandGap = 0.04f;
    [SerializeField] private float ringParticleSize = 0.12f;
    [SerializeField] private Color ringColor = new Color32(115, 239, 247, 255);
    [SerializeField] private Color ringHotColor = new Color32(255, 205, 117, 255);
    // Pause on the empty screen before the game-over menu.
    [SerializeField] private float afterTime = 0.8f;

    private SpriteRenderer curtain;

    public void Play(Movement player)
    {
        StartCoroutine(Run(player));
    }

    // Raises every renderer under `target` by `boost` sorting orders.
    public static void Lift(GameObject target, int boost)
    {
        if (boost == 0 || target == null)
        {
            return;
        }
        foreach (Renderer part in target.GetComponentsInChildren<Renderer>(true))
        {
            part.sortingOrder += boost;
        }
    }

    private IEnumerator Run(Movement player)
    {
        int score = player.GetScore();
        var enemies = new List<Enemy>(FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        foreach (Enemy enemy in enemies)
        {
            enemy.Freeze();
            Lift(enemy.gameObject, spotlightBoost);
        }
        // Rocks stay where they are, hidden under the blackout.
        foreach (Asteroid asteroid in FindObjectsByType<Asteroid>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            asteroid.enabled = false;
        }
        Lift(player.gameObject, spotlightBoost);
        cameraRig.enabled = false;
        CreateCurtain();

        // Pan to centre the ship while the world fades out around it.
        Transform rig = cameraRig.transform;
        Vector3 panFrom = rig.position;
        Vector3 cameraOffset = Camera.main.transform.position - rig.position;
        Vector3 shipPosition = player.transform.position;
        Vector3 panTo = new Vector3(shipPosition.x - cameraOffset.x, shipPosition.y - cameraOffset.y, panFrom.z);
        float introTime = Mathf.Max(panTime, fadeTime);
        for (float t = 0.0f; t < introTime; t += Time.deltaTime)
        {
            float pan = Mathf.Clamp01(t / panTime);
            rig.position = Vector3.Lerp(panFrom, panTo, 1.0f - Mathf.Pow(1.0f - pan, 3.0f));
            float fade = Mathf.Floor(Mathf.Clamp01(t / fadeTime) * fadeSteps) / fadeSteps;
            SetCurtain(curtainColor, fade);
            if (hud)
            {
                hud.alpha = 1.0f - fade;
            }
            yield return null;
        }
        rig.position = panTo;
        SetCurtain(curtainColor, 1.0f);
        if (hud)
        {
            hud.alpha = 0.0f;
        }

        // The ship shudders harder and harder, a pixel at a time, as it breaks up.
        for (float t = 0.0f; t < holdTime; t += Time.deltaTime)
        {
            float strength = t / holdTime;
            Vector2 jitter = Random.value < strength ? Random.insideUnitCircle * (1.0f + strength * 2.0f) : Vector2.zero;
            player.transform.position = shipPosition + new Vector3(Mathf.Round(jitter.x), Mathf.Round(jitter.y), 0.0f) / pixelsPerUnit;
            GameFeel.Impact(0.02f);
            yield return null;
        }

        // It goes: a flash, the blast, and the shockwave.
        Vector2 origin = shipPosition;
        Destroy(player.gameObject);
        if (explosion)
        {
            GameObject blast = PoolManager.Spawn(explosion, origin, Quaternion.identity);
            blast.transform.localScale *= explosionScale;
            Lift(blast, curtainOrder + spotlightBoost);
        }
        if (sparks)
        {
            Lift(PoolManager.Spawn(sparks, origin, Quaternion.identity), curtainOrder + spotlightBoost);
        }
        SoundEffects.Play(explosionClip);
        GameFeel.Impact(1.0f, 0.15f);
        SetCurtain(flashColor, 1.0f);
        float flashUntil = Time.unscaledTime + flashTime;

        // The wave sets each enemy off as it reaches them, nearest first, and runs on until it clears the screen.
        enemies.RemoveAll(enemy => !enemy);
        enemies.Sort((a, b) => ((Vector2)a.transform.position - origin).sqrMagnitude.CompareTo(((Vector2)b.transform.position - origin).sqrMagnitude));
        float reach = Playfield.View.size.magnitude * 0.5f;
        foreach (Enemy enemy in enemies)
        {
            if (enemy)
            {
                reach = Mathf.Max(reach, Vector2.Distance(enemy.transform.position, origin));
            }
        }
        Shockwave(origin, reach / ringSpeed + bandGap * ringBands);
        int next = 0;
        for (float radius = 0.0f; radius < reach; radius += ringSpeed * Time.deltaTime)
        {
            if (Time.unscaledTime >= flashUntil)
            {
                SetCurtain(curtainColor, 1.0f);
            }
            while (next < enemies.Count && (!enemies[next] || Vector2.Distance(enemies[next].transform.position, origin) <= radius))
            {
                Detonate(enemies[next++]);
            }
            yield return null;
        }
        while (next < enemies.Count)
        {
            Detonate(enemies[next++]);
        }
        SetCurtain(curtainColor, 1.0f);
        yield return new WaitForSeconds(afterTime);
        gameOverMenu.Show(score);
    }

    // Enemies already shot down during the sequence are gone (null) and skipped.
    private void Detonate(Enemy enemy)
    {
        if (enemy)
        {
            enemy.Detonate(curtainOrder + spotlightBoost);
        }
    }

    // A full-screen sprite on the camera, drawn over the world but under the lifted ships.
    private void CreateCurtain()
    {
        Camera view = Camera.main;
        curtain = new GameObject("Death Curtain").AddComponent<SpriteRenderer>();
        curtain.transform.SetParent(view.transform, false);
        curtain.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
        curtain.sprite = curtainSprite;
        curtain.sortingOrder = curtainOrder;
        // Oversized, so screen shake never shows its edges.
        Vector2 cover = Playfield.View.size * 1.5f;
        Vector2 spriteSize = curtainSprite.bounds.size;
        curtain.transform.localScale = new Vector3(cover.x / spriteSize.x, cover.y / spriteSize.y, 1.0f);
        SetCurtain(curtainColor, 0.0f);
    }

    private void SetCurtain(Color color, float alpha)
    {
        color.a = alpha;
        curtain.color = color;
    }

    // A ring of pixels bursting out from `origin` at ringSpeed, so the wave front is exactly where enemies blow.
    private void Shockwave(Vector2 origin, float lifetime)
    {
        var go = new GameObject("Death Shockwave");
        go.transform.position = origin;
        var ring = go.AddComponent<ParticleSystem>();
        ring.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ring.main;
        main.duration = bandGap * ringBands + 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = ringSpeed;
        main.startSize = ringParticleSize;
        main.startColor = new ParticleSystem.MinMaxGradient(ringColor, ringHotColor);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = ringParticles * ringBands;

        ParticleSystem.EmissionModule emission = ring.emission;
        emission.rateOverTime = 0.0f;
        var bursts = new ParticleSystem.Burst[ringBands];
        for (int i = 0; i < ringBands; ++i)
        {
            bursts[i] = new ParticleSystem.Burst(i * bandGap, (short)ringParticles);
        }
        emission.SetBursts(bursts);

        // Evenly round the edge of a point-sized circle, flying straight out within the 2D plane.
        ParticleSystem.ShapeModule shape = ring.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.radiusThickness = 0.0f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;

        // Hold full strength, then thin out towards the end of its run.
        ParticleSystem.ColorOverLifetimeModule fade = ring.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) });
        fade.color = gradient;

        var shapeRenderer = go.GetComponent<ParticleSystemRenderer>();
        shapeRenderer.sharedMaterial = ringMaterial;
        shapeRenderer.sortingOrder = curtainOrder + spotlightBoost + 20;
        ring.Play();
        Destroy(go, lifetime + 1.0f);
    }
}

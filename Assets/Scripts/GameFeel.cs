using System.Collections;
using UnityEngine;

// Screen shake and hit-stop for impacts. Sits on the camera; anything can call GameFeel.Impact().
// The shake moves the camera in whole retro pixels so it reads as a chunky 16-bit jolt rather than
// a smooth wobble, and hit-stop freezes time for a few frames so big hits land with weight.
public class GameFeel : Singleton<GameFeel>
{
    [SerializeField] private float maxShake = 0.35f;
    [SerializeField] private float traumaDecay = 2.5f;
    [SerializeField] private float shakeFrequency = 30.0f;
    [SerializeField] private float pixelsPerUnit = 17.0f;
    private Vector3 restPosition;
    private float trauma;
    private float freezeUntil;
    private bool isFrozen;

    // shake: 0-1 trauma to add (shake strength grows with its square, so small hits stay subtle).
    // freeze: seconds of real time to hold the game still.
    public static void Impact(float shake, float freeze = 0.0f)
    {
        if (Instance == null)
        {
            return;
        }
        Instance.trauma = Mathf.Clamp01(Instance.trauma + shake);
        if (freeze > 0.0f)
        {
            Instance.Freeze(freeze);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        restPosition = transform.localPosition;
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            // Never leave the game frozen if the scene unloads mid hit-stop.
            Time.timeScale = 1.0f;
        }
        base.OnDestroy();
    }

    private void Freeze(float duration)
    {
        freezeUntil = Mathf.Max(freezeUntil, Time.unscaledTime + duration);
        if (!isFrozen)
        {
            StartCoroutine(HoldFreeze());
        }
    }

    private IEnumerator HoldFreeze()
    {
        isFrozen = true;
        Time.timeScale = 0.0f;
        while (Time.unscaledTime < freezeUntil)
        {
            yield return null;
        }
        Time.timeScale = 1.0f;
        isFrozen = false;
    }

    private void LateUpdate()
    {
        trauma = Mathf.Max(0.0f, trauma - traumaDecay * Time.unscaledDeltaTime);
        float strength = trauma * trauma * maxShake;
        float t = Time.unscaledTime * shakeFrequency;
        Vector2 offset = new Vector2(Mathf.PerlinNoise(t, 0.3f) * 2.0f - 1.0f, Mathf.PerlinNoise(0.7f, t) * 2.0f - 1.0f) * strength;
        offset.x = Mathf.Round(offset.x * pixelsPerUnit) / pixelsPerUnit;
        offset.y = Mathf.Round(offset.y * pixelsPerUnit) / pixelsPerUnit;
        transform.localPosition = restPosition + (Vector3)offset;
    }
}

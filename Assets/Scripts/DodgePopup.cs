using UnityEngine;

// The "DODGE!" callout that pops up where a barrel roll carried the ship through danger: it jumps up, hangs
// for a moment, then blinks out. Pooled by PoolManager.
[RequireComponent(typeof(SpriteRenderer))]
public class DodgePopup : MonoBehaviour
{
    [SerializeField] private float rise = 0.8f;
    [SerializeField] private float lifetime = 0.75f;
    // Starts blinking out after this fraction of its lifetime.
    [SerializeField] private float blinkFrom = 0.6f;
    [SerializeField] private float blinkInterval = 0.06f;
    [SerializeField] private float pixelsPerUnit = 17.0f;
    private SpriteRenderer spriteRenderer;
    private Vector3 start;
    private float age;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        start = transform.position;
        age = 0.0f;
        spriteRenderer.enabled = true;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / lifetime);
        // Snaps up fast and settles, moving in whole retro pixels.
        float height = rise * (1.0f - Mathf.Pow(1.0f - t, 3.0f));
        height = Mathf.Round(height * pixelsPerUnit) / pixelsPerUnit;
        transform.position = start + Vector3.up * height;
        spriteRenderer.enabled = t < blinkFrom || Mathf.FloorToInt(age / blinkInterval) % 2 == 0;
        if (age >= lifetime)
        {
            PoolManager.Despawn(gameObject);
        }
    }
}

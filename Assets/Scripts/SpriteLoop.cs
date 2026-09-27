using UnityEngine;

// Cycles a sprite through a few frames - blinking lights, a pulsing core - to keep ships looking alive.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 4.0f;
    private SpriteRenderer spriteRenderer;
    private float offset;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Start each ship at a random point in the loop so a formation doesn't blink in unison.
        offset = Random.value * 10.0f;
    }

    private void Update()
    {
        if (frames.Length > 0)
        {
            spriteRenderer.sprite = frames[(int)((Time.time + offset) * framesPerSecond) % frames.Length];
        }
    }
}

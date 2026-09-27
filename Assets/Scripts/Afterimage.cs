using UnityEngine;

// A fading copy of a sprite left behind by fast movement, such as the player's barrel roll.
// Pooled by PoolManager: the prefab is just a SpriteRenderer with this component.
[RequireComponent(typeof(SpriteRenderer))]
public class Afterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private float lifetime;
    private float age;

    // Leaves a snapshot of `source` exactly where it is now, tinted, fading out over `duration` seconds.
    public static void Spawn(GameObject prefab, SpriteRenderer source, Color tint, float duration)
    {
        GameObject go = PoolManager.Spawn(prefab, source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;
        var afterimage = go.GetComponent<Afterimage>();
        SpriteRenderer copy = afterimage.spriteRenderer;
        copy.sprite = source.sprite;
        copy.sortingLayerID = source.sortingLayerID;
        copy.sortingOrder = source.sortingOrder - 1;
        copy.color = tint;
        afterimage.startColor = tint;
        afterimage.lifetime = duration;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        age = 0.0f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        Color color = startColor;
        color.a *= 1.0f - age / lifetime;
        spriteRenderer.color = color;
        if (age >= lifetime)
        {
            PoolManager.Despawn(gameObject);
        }
    }
}

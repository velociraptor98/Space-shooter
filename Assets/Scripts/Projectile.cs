using UnityEngine;

// The player's shot: flies straight along its own facing, which is wherever the ship was aiming.
// Pooled by PoolManager: returned to the pool when it leaves the arena or hits something.
// Enemy fire is handled separately by BulletSystem.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10.0f;

    void Update()
    {
        transform.Translate(Vector3.up * Time.deltaTime * speed);
        Rect bounds = Playfield.Bounds;
        Vector2 position = transform.position;
        if (position.x < bounds.xMin - 1.0f || position.x > bounds.xMax + 1.0f
            || position.y < bounds.yMin - 1.0f || position.y > bounds.yMax + 1.0f)
        {
            PoolManager.Despawn(gameObject);
        }
    }
}

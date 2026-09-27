using UnityEngine;

// The shared explosion effect. Its Animator restarts the blast each time the object is switched on, so
// PoolManager can reuse it; it returns itself to the pool once the animation has played out.
public class Explosion : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.7f;

    private void OnEnable()
    {
        PoolManager.Despawn(gameObject, lifetime);
    }
}

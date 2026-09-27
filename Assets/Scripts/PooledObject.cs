using UnityEngine;

// Added by PoolManager to everything it creates: remembers which prefab the object belongs to, and returns
// it to the pool after a delay or, for particle effects set to stop with a Callback, when they finish.
public class PooledObject : MonoBehaviour
{
    public GameObject Prefab { get; set; }
    private float despawnAt = -1.0f;

    // Cleared on the way back into the pool rather than on the way out: the order OnEnable runs in across a
    // GameObject's components isn't defined, so resetting there could wipe a timer another component just set.
    private void OnDisable()
    {
        despawnAt = -1.0f;
    }

    public void DespawnAfter(float delay)
    {
        despawnAt = Time.time + delay;
    }

    private void Update()
    {
        if (despawnAt >= 0.0f && Time.time >= despawnAt)
        {
            PoolManager.Despawn(gameObject);
        }
    }

    private void OnParticleSystemStopped()
    {
        PoolManager.Despawn(gameObject);
    }
}

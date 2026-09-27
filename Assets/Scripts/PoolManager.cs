using System.Collections.Generic;
using UnityEngine;

// Reuses instances of frequently spawned prefabs - the player's shots, hit sparks, explosions, roll
// afterimages - instead of creating and destroying them, which avoids allocation and garbage-collection
// spikes in busy fights. Spawned objects are reactivated, so they reset themselves in OnEnable.
public class PoolManager : Singleton<PoolManager>
{
    [System.Serializable]
    private struct Warmup
    {
        public GameObject prefab;
        public int count;
    }

    // Instances created up front, so the first busy moments don't have to create them mid-fight.
    [SerializeField] private Warmup[] warmup;
    private readonly Dictionary<GameObject, Stack<PooledObject>> free = new Dictionary<GameObject, Stack<PooledObject>>();
    // An inactive parent new instances are created under, so they stay dormant (no Awake, no particles)
    // until spawned, without touching the prefab asset itself.
    private Transform staging;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        staging = new GameObject("Staging").transform;
        staging.SetParent(transform, false);
        staging.gameObject.SetActive(false);
        foreach (Warmup entry in warmup)
        {
            for (int i = 0; i < entry.count; ++i)
            {
                Release(Create(entry.prefab));
            }
        }
    }

    // Takes a free instance of `prefab` (or makes one), places it and switches it on.
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (Instance == null)
        {
            return Instantiate(prefab, position, rotation);
        }
        return Instance.Take(prefab, position, rotation);
    }

    // Switches an instance off and returns it to its pool. Objects that didn't come from a pool are destroyed.
    public static void Despawn(GameObject instance)
    {
        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null || Instance == null)
        {
            Destroy(instance);
            return;
        }
        // Already back in the pool (e.g. a shot that touched two things in the same physics step).
        if (!instance.activeSelf)
        {
            return;
        }
        Instance.Release(pooled);
    }

    // Returns an instance to its pool after `delay` seconds.
    public static void Despawn(GameObject instance, float delay)
    {
        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null || Instance == null)
        {
            Destroy(instance, delay);
            return;
        }
        pooled.DespawnAfter(delay);
    }

    private GameObject Take(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        PooledObject pooled = null;
        if (free.TryGetValue(prefab, out Stack<PooledObject> stack))
        {
            // Skip anything destroyed while pooled (e.g. with a scene change).
            while (stack.Count > 0 && pooled == null)
            {
                pooled = stack.Pop();
            }
        }
        if (pooled == null)
        {
            pooled = Create(prefab);
        }
        Transform t = pooled.transform;
        t.SetPositionAndRotation(position, rotation);
        // Callers sometimes rescale what they spawn (explosions sized to a ship), so start from the prefab's scale.
        t.localScale = prefab.transform.localScale;
        pooled.gameObject.SetActive(true);
        return pooled.gameObject;
    }

    private PooledObject Create(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, staging);
        instance.SetActive(false);
        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = instance.AddComponent<PooledObject>();
        }
        pooled.Prefab = prefab;
        return pooled;
    }

    private void Release(PooledObject pooled)
    {
        pooled.gameObject.SetActive(false);
        pooled.transform.SetParent(transform, false);
        if (!free.TryGetValue(pooled.Prefab, out Stack<PooledObject> stack))
        {
            stack = new Stack<PooledObject>();
            free.Add(pooled.Prefab, stack);
        }
        stack.Push(pooled);
    }
}

using UnityEngine;

// Base for scene managers that exactly one of should exist (pools, bullets, sound, screen shake). Gives the
// manager a static Instance for cheap global access and removes any accidental duplicate.
// Scene-scoped rather than DontDestroyOnLoad: restarting the game reloads the scene, and with it a fresh
// manager whose pools only ever hold that scene's objects.
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] A second instance on '{name}' was removed; there should only be one per scene.");
            Destroy(this);
            return;
        }
        Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

using UnityEngine;

// Plays one-shot sound effects (explosions, breaking rocks) through a single AudioSource. PlayOneShot lets
// sounds overlap without the throwaway GameObject that AudioSource.PlayClipAtPoint creates for every call.
[RequireComponent(typeof(AudioSource))]
public class SoundEffects : Singleton<SoundEffects>
{
    private AudioSource source;

    protected override void Awake()
    {
        base.Awake();
        source = GetComponent<AudioSource>();
    }

    public static void Play(AudioClip clip, float volume = 1.0f)
    {
        if (Instance && clip)
        {
            Instance.source.PlayOneShot(clip, volume);
        }
    }
}

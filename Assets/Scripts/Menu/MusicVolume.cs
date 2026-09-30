using UnityEngine;

// Scales a music AudioSource by the player's music volume, keeping the volume it was mixed at as the maximum.
[RequireComponent(typeof(AudioSource))]
public class MusicVolume : MonoBehaviour
{
    private AudioSource source;
    private float mixedVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        mixedVolume = source.volume;
        Apply();
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= Apply;
    }

    private void Apply()
    {
        source.volume = mixedVolume * GameSettings.Fraction(GameSettings.MusicVolume);
    }
}

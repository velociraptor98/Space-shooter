using System;
using UnityEngine;

// Player options and the best score, kept in PlayerPrefs so they survive between sessions. Volumes are whole
// steps (0..VolumeSteps) so the menu's segmented bars map onto them exactly.
public static class GameSettings
{
    public const int VolumeSteps = 10;
    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string ShakeKey = "ScreenShake";
    private const string BestKey = "BestScore";

    // Raised whenever an option changes, so live objects (music, fullscreen) can follow it.
    public static event Action Changed;

    public static int MasterVolume
    {
        get => PlayerPrefs.GetInt(MasterKey, 8);
        set => Set(MasterKey, Mathf.Clamp(value, 0, VolumeSteps));
    }

    public static int MusicVolume
    {
        get => PlayerPrefs.GetInt(MusicKey, 7);
        set => Set(MusicKey, Mathf.Clamp(value, 0, VolumeSteps));
    }

    public static bool ScreenShake
    {
        get => PlayerPrefs.GetInt(ShakeKey, 1) == 1;
        set => Set(ShakeKey, value ? 1 : 0);
    }

    public static bool Fullscreen
    {
        get => Screen.fullScreen;
        set
        {
            Screen.fullScreen = value;
            Changed?.Invoke();
        }
    }

    public static int BestScore => PlayerPrefs.GetInt(BestKey, 0);

    // Records a finished run's score, returning true if it's a new best.
    public static bool SubmitScore(int score)
    {
        if (score <= BestScore)
        {
            return false;
        }
        PlayerPrefs.SetInt(BestKey, score);
        PlayerPrefs.Save();
        return true;
    }

    public static float Fraction(int steps)
    {
        return steps / (float)VolumeSteps;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        AudioListener.volume = Fraction(MasterVolume);
    }

    private static void Set(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        Apply();
        Changed?.Invoke();
    }
}

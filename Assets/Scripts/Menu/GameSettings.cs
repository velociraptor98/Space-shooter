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

    // Read from PlayerPrefs once and kept here, as ScreenShake is checked every frame.
    private static bool loaded;
    private static int masterVolume;
    private static int musicVolume;
    private static bool screenShake;
    private static int bestScore;

    public static int MasterVolume
    {
        get
        {
            Load();
            return masterVolume;
        }
        set
        {
            masterVolume = Mathf.Clamp(value, 0, VolumeSteps);
            Set(MasterKey, masterVolume);
        }
    }

    public static int MusicVolume
    {
        get
        {
            Load();
            return musicVolume;
        }
        set
        {
            musicVolume = Mathf.Clamp(value, 0, VolumeSteps);
            Set(MusicKey, musicVolume);
        }
    }

    public static bool ScreenShake
    {
        get
        {
            Load();
            return screenShake;
        }
        set
        {
            screenShake = value;
            Set(ShakeKey, value ? 1 : 0);
        }
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

    public static int BestScore
    {
        get
        {
            Load();
            return bestScore;
        }
    }

    // Records a finished run's score, returning true if it's a new best.
    public static bool SubmitScore(int score)
    {
        if (score <= BestScore)
        {
            return false;
        }
        bestScore = score;
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

    private static void Load()
    {
        if (loaded)
        {
            return;
        }
        loaded = true;
        masterVolume = PlayerPrefs.GetInt(MasterKey, 8);
        musicVolume = PlayerPrefs.GetInt(MusicKey, 7);
        screenShake = PlayerPrefs.GetInt(ShakeKey, 1) == 1;
        bestScore = PlayerPrefs.GetInt(BestKey, 0);
    }

    private static void Set(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        Apply();
        Changed?.Invoke();
    }
}

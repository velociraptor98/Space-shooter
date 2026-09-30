using UnityEngine;
using UnityEngine.UI;

// The options screen, shared by the title and pause menus. Binds its controls to GameSettings: the volume
// sliders step through whole segments, the toggles read ON/OFF.
public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider musicVolume;
    [SerializeField] private Toggle screenShake;
    [SerializeField] private Text screenShakeValue;
    [SerializeField] private Toggle fullscreen;
    [SerializeField] private Text fullscreenValue;
    private MenuNavigator navigator;

    private void Awake()
    {
        navigator = GetComponentInParent<MenuNavigator>(true);
        masterVolume.maxValue = musicVolume.maxValue = GameSettings.VolumeSteps;
        masterVolume.onValueChanged.AddListener(value => Changed(() => GameSettings.MasterVolume = (int)value));
        musicVolume.onValueChanged.AddListener(value => Changed(() => GameSettings.MusicVolume = (int)value));
        screenShake.onValueChanged.AddListener(on => Changed(() => GameSettings.ScreenShake = on));
        fullscreen.onValueChanged.AddListener(on => Changed(() => GameSettings.Fullscreen = on));
#if UNITY_WEBGL && !UNITY_EDITOR
        // Browsers only allow fullscreen from their own control or a click handler, not a keypress in a menu.
        fullscreen.gameObject.SetActive(false);
#endif
    }

    private void OnEnable()
    {
        masterVolume.SetValueWithoutNotify(GameSettings.MasterVolume);
        musicVolume.SetValueWithoutNotify(GameSettings.MusicVolume);
        screenShake.SetIsOnWithoutNotify(GameSettings.ScreenShake);
        fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        UpdateLabels();
    }

    // Wired to the Back button.
    public void Back()
    {
        navigator.Back();
    }

    private void Changed(System.Action apply)
    {
        apply();
        navigator.PlayMove();
        // Labels follow the controls rather than the settings: the window only changes mode at the end of the
        // frame, so Screen.fullScreen still reads the old value here.
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        screenShakeValue.text = screenShake.isOn ? "ON" : "OFF";
        fullscreenValue.text = fullscreen.isOn ? "ON" : "OFF";
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

// The start screen. It opens on the logo and a blinking "PRESS ANY BUTTON"; the first press brings up the
// main menu, whose buttons call the methods below.
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private ScreenFader fader;
    [SerializeField] private GameObject pressAnyButton;
    [SerializeField] private MenuPanel mainPanel;
    [SerializeField] private MenuPanel optionsPanel;
    [SerializeField] private MenuPanel controlsPanel;
    [SerializeField] private MenuPanel creditsPanel;
    [SerializeField] private GameObject quitButton;
    [SerializeField] private Text bestScore;
    [SerializeField] private AudioClip startClip;
    [SerializeField] private string playScene = "Game";
    private IDisposable anyButton;
    private bool starting;

    private void Start()
    {
        mainPanel.gameObject.SetActive(false);
        optionsPanel.gameObject.SetActive(false);
        controlsPanel.gameObject.SetActive(false);
        creditsPanel.gameObject.SetActive(false);
        bestScore.text = "BEST " + GameSettings.BestScore.ToString("D6");
#if UNITY_WEBGL && !UNITY_EDITOR
        // There's nothing to quit to in a browser tab.
        quitButton.SetActive(false);
#endif
        anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => OpenMenu());
    }

    private void OnDestroy()
    {
        anyButton?.Dispose();
    }

    private void OpenMenu()
    {
        pressAnyButton.SetActive(false);
        navigator.PlayConfirm();
        navigator.OpenRoot(mainPanel);
    }

    public void Play()
    {
        if (starting)
        {
            return;
        }
        starting = true;
        navigator.Play(startClip);
        navigator.CloseAll();
        fader.LoadScene(playScene);
    }

    public void OpenOptions()
    {
        navigator.Open(optionsPanel);
    }

    public void OpenControls()
    {
        navigator.Open(controlsPanel);
    }

    public void OpenCredits()
    {
        navigator.Open(creditsPanel);
    }

    public void Quit()
    {
        Application.Quit();
    }
}

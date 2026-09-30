using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// Drives a canvas's menus as a stack of panels: opening a sub-menu pushes it, Back (the UI Cancel input or a
// Back button) returns to the one below. Also plays the menu blips, and keeps something selected so keyboard
// and gamepad navigation never gets lost after a stray mouse click.
[RequireComponent(typeof(AudioSource))]
public class MenuNavigator : MonoBehaviour
{
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip backClip;
    // Selection changes this soon after opening a panel are the menu's own, not the player's, so stay quiet.
    [SerializeField] private float quietTime = 0.05f;
    private readonly Stack<MenuPanel> stack = new Stack<MenuPanel>();
    private AudioSource source;
    private InputAction cancelAction;
    private float quietUntil;

    // Cancel was pressed on the bottom panel, which has nothing to go back to (the pause menu resumes on it).
    public event Action RootCancelled;

    public MenuPanel Current => stack.Count > 0 ? stack.Peek() : null;
    public int Depth => stack.Count;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        // Menu sounds keep playing while the pause menu has the rest of the game's audio paused.
        source.ignoreListenerPause = true;
    }

    private void Start()
    {
        var module = EventSystem.current ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        cancelAction = module && module.cancel ? module.cancel.action : null;
    }

    private void Update()
    {
        if (Current == null)
        {
            return;
        }
        if (cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            Back();
        }
        else if (EventSystem.current && EventSystem.current.currentSelectedGameObject == null)
        {
            Current.Select();
        }
    }

    // Replaces whatever is showing with a single root panel.
    public void OpenRoot(MenuPanel panel)
    {
        CloseAll();
        panel.ResetSelection();
        Push(panel);
    }

    public void Open(MenuPanel panel)
    {
        if (Current)
        {
            Current.Hide();
        }
        panel.ResetSelection();
        Push(panel);
    }

    public void Back()
    {
        if (stack.Count <= 1)
        {
            RootCancelled?.Invoke();
            return;
        }
        PlayBack();
        stack.Pop().Hide();
        Quiet();
        Current.Show();
    }

    public void CloseAll()
    {
        while (stack.Count > 0)
        {
            stack.Pop().Hide();
        }
        if (EventSystem.current)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void PlayMove()
    {
        if (Time.unscaledTime >= quietUntil)
        {
            Play(moveClip);
        }
    }

    public void PlayConfirm()
    {
        Play(confirmClip);
    }

    public void PlayBack()
    {
        Play(backClip);
    }

    public void Play(AudioClip clip)
    {
        if (clip)
        {
            source.PlayOneShot(clip);
        }
    }

    private void Push(MenuPanel panel)
    {
        stack.Push(panel);
        Quiet();
        panel.Show();
    }

    private void Quiet()
    {
        quietUntil = Time.unscaledTime + quietTime;
    }
}

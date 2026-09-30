using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pauses the run on the Pause input: freezes time and the game's audio, hands the mouse back from the
// crosshair, and turns off the ship's controls so clicking a menu button doesn't also fire.
// Runs before the MenuNavigator so an Escape that backs out of Options is seen at the Options depth, and
// doesn't also count as resuming from the pause panel underneath.
[DefaultExecutionOrder(-10)]
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private ScreenFader fader;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MenuPanel pausePanel;
    [SerializeField] private MenuPanel optionsPanel;
    [SerializeField] private GameObject backdrop;
    [SerializeField] private int menuScene = 0;
    private InputAction pauseAction;
    private InputActionMap playerControls;
    private bool paused;
    // Escape is both Pause and the menu's Cancel, so one press could resume and then re-pause in a frame.
    private int lastToggleFrame = -1;

    private void Awake()
    {
        pauseAction = InputSystem.actions.FindAction("Game/Pause", true);
        playerControls = InputSystem.actions.FindActionMap("Player", true);
        navigator.RootCancelled += Resume;
        pausePanel.gameObject.SetActive(false);
        optionsPanel.gameObject.SetActive(false);
        backdrop.SetActive(false);
    }

    private void OnDestroy()
    {
        navigator.RootCancelled -= Resume;
        if (paused)
        {
            // The input actions and audio listener outlive the scene, so never leave them paused.
            playerControls.Enable();
            AudioListener.pause = false;
        }
    }

    private void Update()
    {
        if (pauseAction.WasPressedThisFrame())
        {
            if (!paused)
            {
                Pause();
            }
            else if (navigator.Depth == 1)
            {
                Resume();
            }
        }
        if (paused)
        {
            // GameFeel's hit-stop restores the time scale when it ends; keep the game frozen regardless.
            Time.timeScale = 0.0f;
        }
    }

    public void Pause()
    {
        if (paused || gameManager.IsGameOver || Time.frameCount == lastToggleFrame)
        {
            return;
        }
        paused = true;
        lastToggleFrame = Time.frameCount;
        Time.timeScale = 0.0f;
        AudioListener.pause = true;
        playerControls.Disable();
        Cursor.visible = true;
        backdrop.SetActive(true);
        navigator.PlayConfirm();
        navigator.OpenRoot(pausePanel);
    }

    public void Resume()
    {
        if (!paused || Time.frameCount == lastToggleFrame)
        {
            return;
        }
        paused = false;
        lastToggleFrame = Time.frameCount;
        Time.timeScale = 1.0f;
        AudioListener.pause = false;
        playerControls.Enable();
        // The crosshair stands in for the cursor while the ship is flying.
        Cursor.visible = Movement.Instance == null;
        backdrop.SetActive(false);
        navigator.PlayBack();
        navigator.CloseAll();
    }

    public void OpenOptions()
    {
        navigator.Open(optionsPanel);
    }

    public void Restart()
    {
        fader.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        fader.LoadScene(menuScene);
    }
}

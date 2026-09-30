using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Runs a play session: the opening fly-in, the game-over state, and the quick Restart input once it's over.
public class GameManager : MonoBehaviour
{
    [SerializeField] private Movement player;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private UManager uiManager;
    [SerializeField] private float flyInTime = 1.6f;
    [SerializeField] private float readyTime = 2.2f;
    private InputAction restartAction;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        restartAction = InputSystem.actions.FindAction("Game/Restart", true);
    }

    // The run starts on its own: the ship flies in under a "GET READY" banner, then the waves begin.
    private IEnumerator Start()
    {
        player.PlayIntro(flyInTime);
        uiManager.ShowBanner("GET READY", readyTime);
        yield return new WaitForSeconds(readyTime);
        spawnManager.StartSpawn();
    }

    private void Update()
    {
        if (IsGameOver && restartAction.WasPressedThisFrame())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void GameOver()
    {
        IsGameOver = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool isGameOver = false;
    [SerializeField] private Movement player;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private UManager uiManager;
    [SerializeField] private float flyInTime = 1.6f;
    [SerializeField] private float readyTime = 2.2f;
    private InputAction restartAction;
    private InputAction quitAction;
    private void Awake()
    {
        restartAction = InputSystem.actions.FindAction("Game/Restart", true);
        quitAction = InputSystem.actions.FindAction("Game/Quit", true);
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
        if(isGameOver  && restartAction.WasPressedThisFrame())
        {
            // Scene - Game
            SceneManager.LoadScene(1);
        }
        if(quitAction.WasPressedThisFrame())
        {
            Application.Quit();
        }
    }
    public void GameOver()
    {
        isGameOver = true;
    }
}

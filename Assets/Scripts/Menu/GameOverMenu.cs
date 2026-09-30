using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shown once the ship is lost: the run's score against the best, then Retry or back to the title.
// Waits a moment first so the final explosion gets to play out.
public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private ScreenFader fader;
    [SerializeField] private MenuPanel panel;
    [SerializeField] private GameObject backdrop;
    [SerializeField] private Text score;
    [SerializeField] private Text best;
    [SerializeField] private GameObject newBest;
    [SerializeField] private float delay = 1.2f;
    [SerializeField] private int menuScene = 0;

    private void Awake()
    {
        panel.gameObject.SetActive(false);
    }

    public void Show(int finalScore)
    {
        StartCoroutine(ShowAfterDelay(finalScore));
    }

    private IEnumerator ShowAfterDelay(int finalScore)
    {
        bool isBest = GameSettings.SubmitScore(finalScore);
        yield return new WaitForSecondsRealtime(delay);
        score.text = "SCORE " + finalScore.ToString("D6");
        best.text = "BEST  " + GameSettings.BestScore.ToString("D6");
        newBest.SetActive(isBest);
        backdrop.SetActive(true);
        navigator.OpenRoot(panel);
    }

    public void Retry()
    {
        fader.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        fader.LoadScene(menuScene);
    }
}

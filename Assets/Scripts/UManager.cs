using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The in-game HUD: score, lives and centre-screen banners. Also hands the player's death to the DeathSequence.
public class UManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Image livesImage;
    // Indexed by lives remaining.
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private DeathSequence deathSequence;
    // Centre-screen callouts such as "GET READY" and wave warnings.
    [SerializeField] private Text bannerText;
    private Movement player;
    private GameManager gameManager;
    private Coroutine banner;

    private void Awake()
    {
        bannerText.enabled = false;
    }

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        player = FindAnyObjectByType<Movement>();
        UpdateText();
    }

    public void ShowBanner(string message, float duration)
    {
        if (banner != null)
        {
            StopCoroutine(banner);
        }
        banner = StartCoroutine(Banner(message, duration));
    }

    private IEnumerator Banner(string message, float duration)
    {
        bannerText.text = message;
        // Blink on and off like an arcade attract screen, then clear.
        for (float t = 0.0f; t < duration; t += 0.25f)
        {
            bannerText.enabled = !bannerText.enabled || t == 0.0f;
            yield return new WaitForSeconds(0.25f);
        }
        bannerText.enabled = false;
        banner = null;
    }

    public void UpdateText()
    {
        // The pixel font has capitals only; matches the game-over panel's "SCORE 000000".
        scoreText.text = "SCORE " + player.GetScore().ToString("D6");
    }

    public void UpdateLives(int currentLives)
    {
        livesImage.sprite = sprites[currentLives];
    }

    public void GameOver()
    {
        gameManager.GameOver();
        deathSequence.Play(player);
    }
}

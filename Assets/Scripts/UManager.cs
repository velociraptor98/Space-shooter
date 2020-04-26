using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    Text scoreText;
    GameObject player;
    [SerializeField]
    private Image livesImage;
    [SerializeField]
    private Sprite[] sprites;
    [SerializeField]
    private Text GameOverText;
    [SerializeField]
    private Text Reset_Text;
    private GameObject GameManager;
    private void Start()
    {
        GameManager = GameObject.Find("GameManager");
        player = GameObject.FindGameObjectWithTag("Player");
        GameOverText.enabled = false;
        Reset_Text.enabled = false;
        UpdateText();
    }
    public void UpdateText()
    {
        scoreText.text = "Score: " + player.GetComponent<Movement>().GetScore();
    }
    public void UpdateLives(int currentLives)
    {
        livesImage.sprite = sprites[currentLives];
    }
    public void GameOver()
    {
        GameOverText.enabled = true;
        Reset_Text.enabled = true;
        GameManager.GetComponent<GameManager>().GameOver();
        StartCoroutine(GameOverFlicker());
    }
     IEnumerator GameOverFlicker()
    {
        while(true)
        {
            GameOverText.enabled = false;
            yield return new WaitForSeconds(0.5f);
            GameOverText.enabled = true;
            yield return new WaitForSeconds(0.5f);
        }
    }
}

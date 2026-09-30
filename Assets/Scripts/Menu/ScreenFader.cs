using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A full-screen cover that fades each scene in from black and fades out before loading the next one, in a
// few hard steps rather than a smooth ramp, to suit the pixel art. It blocks clicks while it's covering.
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image cover;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private int steps = 4;
    private bool loading;

    private IEnumerator Start()
    {
        yield return Fade(1.0f, 0.0f);
    }

    public void LoadScene(string sceneName)
    {
        if (!loading)
        {
            StartCoroutine(FadeAndLoad(sceneName));
        }
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        loading = true;
        yield return Fade(cover.color.a, 1.0f);
        // Leaving from the pause menu must not carry the pause into the next scene.
        Time.timeScale = 1.0f;
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        cover.enabled = true;
        cover.raycastTarget = true;
        for (float t = 0.0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, Mathf.Floor(t / duration * steps) / steps));
            yield return null;
        }
        SetAlpha(to);
        cover.enabled = to > 0.0f;
        cover.raycastTarget = to > 0.0f;
    }

    private void SetAlpha(float alpha)
    {
        Color color = cover.color;
        color.a = alpha;
        cover.color = color;
    }
}

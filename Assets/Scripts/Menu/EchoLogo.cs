using UnityEngine;
using UnityEngine.UI;

// Brings the ECHO title to life: the logo bobs gently while its outline ripples outwards and fades, like a
// signal echoing off into space.
[RequireComponent(typeof(Image))]
public class EchoLogo : MonoBehaviour
{
    // The logo's outline, which the echoes are drawn with.
    [SerializeField] private Sprite ringSprite;
    [SerializeField] private int echoes = 2;
    [SerializeField] private float period = 2.4f;
    [SerializeField] private float growth = 0.3f;
    [SerializeField] private Color echoColor = new Color32(115, 239, 247, 200);
    [SerializeField] private float bobHeight = 2.0f;
    [SerializeField] private float bobSpeed = 1.6f;
    private RectTransform rect;
    private Vector2 home;
    private Image[] ghosts;

    private void Awake()
    {
        rect = (RectTransform)transform;
        home = rect.anchoredPosition;
        ghosts = new Image[echoes];
        for (int i = 0; i < echoes; ++i)
        {
            var ghost = new GameObject("Echo " + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            // Siblings placed just before the logo, so they draw behind it (children would draw on top).
            ghost.transform.SetParent(transform.parent, false);
            ghost.transform.SetSiblingIndex(transform.GetSiblingIndex());
            ghost.sprite = ringSprite;
            ghost.raycastTarget = false;
            ghost.rectTransform.anchorMin = rect.anchorMin;
            ghost.rectTransform.anchorMax = rect.anchorMax;
            ghost.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ghost.rectTransform.sizeDelta = rect.sizeDelta;
            ghosts[i] = ghost;
        }
    }

    private void Update()
    {
        // Whole-pixel steps keep the pixel art crisp as it moves.
        float bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight);
        rect.anchoredPosition = home + Vector2.up * bob;
        // The logo's centre, for ghosts that scale about it whatever the logo's own pivot.
        Vector2 center = rect.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, rect.sizeDelta);
        for (int i = 0; i < ghosts.Length; ++i)
        {
            float t = Mathf.Repeat(Time.unscaledTime / period + i / (float)ghosts.Length, 1.0f);
            ghosts[i].rectTransform.anchoredPosition = center;
            ghosts[i].rectTransform.localScale = Vector3.one * (1.0f + growth * t);
            Color color = echoColor;
            color.a *= (1.0f - t) * (1.0f - t);
            ghosts[i].color = color;
        }
    }
}

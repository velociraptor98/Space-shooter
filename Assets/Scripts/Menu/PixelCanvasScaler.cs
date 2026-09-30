using UnityEngine;
using UnityEngine.UI;

// Scales a pixel-art canvas by whole numbers only, like the Pixel Perfect Camera does for the world, so every
// UI texel covers the same number of screen pixels. The layout is designed for the reference resolution and
// gets the largest integer scale that still fits it on screen.
[RequireComponent(typeof(CanvasScaler))]
[ExecuteAlways]
public class PixelCanvasScaler : MonoBehaviour
{
    [SerializeField] private Vector2Int referenceResolution = new Vector2Int(640, 360);
    private CanvasScaler scaler;

    private void OnEnable()
    {
        scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private void Update()
    {
        float fit = Mathf.Min(Screen.width / (float)referenceResolution.x, Screen.height / (float)referenceResolution.y);
        scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(fit));
    }
}

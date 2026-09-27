using UnityEngine;
using UnityEngine.U2D;

// The visible play area in world units. Everything that spawns, despawns or keeps to the screen measures
// from here, so the view can be resized (via the Pixel Perfect Camera's reference resolution) in one place.
public static class Playfield
{
    private static Camera measuredCamera;
    private static Rect bounds;

    public static Rect Bounds
    {
        get
        {
            Camera cam = Camera.main;
            if (cam != measuredCamera)
            {
                Measure(cam);
            }
            return bounds;
        }
    }

    public static float Left => Bounds.xMin;
    public static float Right => Bounds.xMax;
    public static float Top => Bounds.yMax;
    public static float Bottom => Bounds.yMin;
    public static float CenterX => Bounds.center.x;

    // Measured once per camera, from its resting position: screen shake moves the camera, and the
    // playfield shouldn't shake with it.
    private static void Measure(Camera cam)
    {
        measuredCamera = cam;
        // The Pixel Perfect Camera's reference resolution defines the view exactly, even before the
        // camera has rendered and settled its own size and aspect.
        var pixelPerfect = cam.GetComponent<PixelPerfectCamera>();
        float halfHeight = pixelPerfect ? pixelPerfect.refResolutionY * 0.5f / pixelPerfect.assetsPPU : cam.orthographicSize;
        float halfWidth = pixelPerfect ? pixelPerfect.refResolutionX * 0.5f / pixelPerfect.assetsPPU : cam.orthographicSize * cam.aspect;
        Vector2 center = cam.transform.position;
        bounds = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2.0f, halfHeight * 2.0f);
    }
}

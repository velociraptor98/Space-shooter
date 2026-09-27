using UnityEngine;
using UnityEngine.U2D;

// The play area in world units. The arena (Bounds) is larger than the screen and the camera follows the
// player around it; the View is the part currently on screen. Everything that spawns, despawns or keeps
// within limits measures from here, so both can be resized in one place: the view via the Pixel Perfect
// Camera's reference resolution, the arena via CameraFollow's arena scale.
public static class Playfield
{
    private static Camera measuredCamera;
    private static Rect bounds;
    private static Vector2 viewSize;

    // The whole arena.
    public static Rect Bounds
    {
        get
        {
            Measure();
            return bounds;
        }
    }

    public static float Left => Bounds.xMin;
    public static float Right => Bounds.xMax;
    public static float Top => Bounds.yMax;
    public static float Bottom => Bounds.yMin;
    public static float CenterX => Bounds.center.x;

    // The part of the arena on screen right now.
    public static Rect View
    {
        get
        {
            Measure();
            Vector2 center = measuredCamera.transform.position;
            return new Rect(center - viewSize * 0.5f, viewSize);
        }
    }

    // Maps a screen position (such as the mouse) into the world. The view is fitted to the screen the way the
    // Pixel Perfect Camera fits it - the largest 16:9-style area, centred - and measured from the camera's
    // current position so aiming stays true as it follows the player and shakes. Done by hand because the
    // camera's own ScreenToWorldPoint uses its internal render-texture rect, which doesn't match the display.
    public static Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        Rect view = View;
        float aspect = view.width / view.height;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        Rect shown = screenWidth / screenHeight > aspect
            ? new Rect((screenWidth - screenHeight * aspect) * 0.5f, 0.0f, screenHeight * aspect, screenHeight)
            : new Rect(0.0f, (screenHeight - screenWidth / aspect) * 0.5f, screenWidth, screenWidth / aspect);
        Vector2 viewport = new Vector2((screenPosition.x - shown.x) / shown.width, (screenPosition.y - shown.y) / shown.height);
        return view.min + Vector2.Scale(viewport, view.size);
    }

    // Measured once per camera. The arena is centred where the camera starts, before it moves or shakes.
    private static void Measure()
    {
        Camera cam = Camera.main;
        if (cam == measuredCamera)
        {
            return;
        }
        measuredCamera = cam;
        // The Pixel Perfect Camera's reference resolution defines the view exactly, even before the
        // camera has rendered and settled its own size and aspect.
        var pixelPerfect = cam.GetComponent<PixelPerfectCamera>();
        viewSize = pixelPerfect
            ? new Vector2(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY) / pixelPerfect.assetsPPU
            : new Vector2(cam.orthographicSize * 2.0f * cam.aspect, cam.orthographicSize * 2.0f);
        var follow = cam.GetComponentInParent<CameraFollow>();
        Vector2 arenaSize = viewSize * (follow ? follow.ArenaScale : 1.0f);
        Vector2 center = follow ? (Vector2)follow.transform.position : (Vector2)cam.transform.position;
        bounds = new Rect(center - arenaSize * 0.5f, arenaSize);
    }
}

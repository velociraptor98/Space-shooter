using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Video;

// The studio sting shown at launch: plays the video, holds on its final logo for a beat, then fades to the
// title screen. Any button skips it. The video is streamed from StreamingAssets by URL, because web builds
// can't play imported VideoClips; if it can't be loaded at all, the game carries on to the title.
[RequireComponent(typeof(VideoPlayer))]
public class SplashScreen : MonoBehaviour
{
    [SerializeField] private string videoFile = "LittlePixelSting.mp4";
    [SerializeField] private ScreenFader fader;
    [SerializeField] private string titleScene = "MainMenu";
    [SerializeField] private float holdTime = 0.6f;
    [SerializeField] private float loadTimeout = 5.0f;
    private VideoPlayer video;
    private IDisposable anyButton;
    private bool finished;

    private IEnumerator Start()
    {
        video = GetComponent<VideoPlayer>();
        video.url = Path.Combine(Application.streamingAssetsPath, videoFile);
        video.loopPointReached += _ => finished = true;
        video.errorReceived += (_, message) =>
        {
            Debug.LogWarning("[Splash] " + message);
            finished = true;
        };
        anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => Leave());

        video.Prepare();
        for (float waited = 0.0f; !video.isPrepared && !finished && waited < loadTimeout; waited += Time.unscaledDeltaTime)
        {
            yield return null;
        }
        if (video.isPrepared && !finished)
        {
            video.Play();
            while (!finished)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(holdTime);
        }
        Leave();
    }

    private void OnDestroy()
    {
        anyButton?.Dispose();
    }

    private void Leave()
    {
        fader.LoadScene(titleScene);
    }
}

using UnityEngine;
using UnityEngine.UI;

// Flashes a graphic on and off, arcade style ("PRESS ANY BUTTON", "NEW BEST!"). Runs on unscaled time so it
// keeps blinking while the game is paused.
[RequireComponent(typeof(Graphic))]
public class Blink : MonoBehaviour
{
    [SerializeField] private float onTime = 0.6f;
    [SerializeField] private float offTime = 0.3f;
    private Graphic graphic;
    private float startTime;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
    }

    private void OnEnable()
    {
        startTime = Time.unscaledTime;
    }

    private void OnDisable()
    {
        graphic.enabled = true;
    }

    private void Update()
    {
        graphic.enabled = (Time.unscaledTime - startTime) % (onTime + offTime) < onTime;
    }
}

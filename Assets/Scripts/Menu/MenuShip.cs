using UnityEngine;

// The ship idling on the title screen: drifts in a slow figure of eight, snapped to the pixel grid.
public class MenuShip : MonoBehaviour
{
    [SerializeField] private Vector2 drift = new Vector2(0.6f, 0.3f);
    [SerializeField] private float speed = 0.7f;
    [SerializeField] private float pixelsPerUnit = 17.0f;
    private Vector3 home;

    private void Awake()
    {
        home = transform.position;
    }

    private void Update()
    {
        float t = Time.time * speed;
        Vector2 offset = new Vector2(Mathf.Sin(t) * drift.x, Mathf.Sin(t * 2.0f) * drift.y);
        offset.x = Mathf.Round(offset.x * pixelsPerUnit) / pixelsPerUnit;
        offset.y = Mathf.Round(offset.y * pixelsPerUnit) / pixelsPerUnit;
        transform.position = home + (Vector3)offset;
    }
}

using UnityEngine;

// Moves the camera rig after the player around an arena larger than the screen. It eases rather than
// locks on, leans a little towards where the player is aiming so they see more of what they're shooting
// at, and stops at the arena's edges. Screen shake (GameFeel) runs on the camera itself, beneath this rig.
public class CameraFollow : MonoBehaviour
{
    // The arena's size as a multiple of the screen's, in each direction.
    [SerializeField] private float arenaScale = 1.6f;
    [SerializeField] private float smoothTime = 0.25f;
    // How far towards the aim point the camera leans, as a fraction of the distance to it.
    [SerializeField] private float aimLead = 0.2f;
    private Vector3 velocity;

    public float ArenaScale => arenaScale;

    private void LateUpdate()
    {
        Movement player = Movement.Instance;
        if (player == null)
        {
            return;
        }
        Vector2 playerPosition = player.transform.position;
        Vector2 target = playerPosition + (player.AimPoint - playerPosition) * aimLead;

        // Keep the whole view inside the arena.
        Rect arena = Playfield.Bounds;
        Vector2 halfView = Playfield.View.size * 0.5f;
        target.x = Mathf.Clamp(target.x, arena.xMin + halfView.x, arena.xMax - halfView.x);
        target.y = Mathf.Clamp(target.y, arena.yMin + halfView.y, arena.yMax - halfView.y);

        Vector3 goal = new Vector3(target.x, target.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}

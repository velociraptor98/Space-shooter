using UnityEngine;

// The player's shot. Enemy fire is handled separately by BulletSystem.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10.0f;

    void Update()
    {
        transform.Translate(Vector3.up * Time.deltaTime * speed);
        if (transform.position.y >= Playfield.Top + 1.0f)
        {
            if (transform.parent)
            {
                Destroy(transform.parent.gameObject);
            }
            Destroy(this.gameObject);
        }
    }
}

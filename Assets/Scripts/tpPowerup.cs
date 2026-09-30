using UnityEngine;

// A power-up drifting down the arena. PowerID picks what it grants: 0 triple shot, 1 speed, 2 shield.
public class tpPowerup : MonoBehaviour
{
    [SerializeField] private float speed = 3.0f;
    [SerializeField] private int PowerID = 0;

    private void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        if (transform.position.y < Playfield.Bottom - 1.0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Movement player = other.GetComponent<Movement>();
        if (!player)
        {
            return;
        }
        switch (PowerID)
        {
            case 0:
                player.SetActive();
                break;
            case 1:
                player.SetSpeedActive();
                break;
            case 2:
                player.SetShieldActive();
                break;
        }
        Destroy(gameObject);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float speed = 10.0f;
    private bool isEnemyLaser = false;
    // Update is called once per frame
    void Update()
    {
        if(isEnemyLaser == false)
        {
            MoveUp();
        }
        else
        {
            speed = 20.0f;
            MoveDown();
            
        }
    }

    private void MoveUp()
    {
        transform.Translate(Vector3.up * Time.deltaTime * speed);
        if (transform.position.y >= 9.0f)
        {

            if (transform.parent)
            {
                Destroy(transform.parent.gameObject);
            }
            Destroy(this.gameObject);
        }
    }
    private void MoveDown()
    {
        transform.Translate(Vector3.down * Time.deltaTime * speed);
        if (transform.position.y < -9.0f)
        {

            if (transform.parent)
            {
                Destroy(transform.parent.gameObject);
            }
            Destroy(this.gameObject);
        }
    }
    public void SetEnemyLaser()
    {
        isEnemyLaser = true;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isEnemyLaser)
        {
            Movement player = collision.GetComponent<Movement>();
            if (player)
            {
                player.OnDamage();
            }
        }
    }
}

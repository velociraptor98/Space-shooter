using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float speed = 10.0f;
    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move()
    {
        transform.Translate(Vector3.up*Time.deltaTime*speed);
        if (transform.position.y >= 9.0f)
        {
            
            if (transform.parent)
            {
                Destroy(transform.parent.gameObject);
            }
            Destroy(this.gameObject);

            Debug.Log("In correct part");
        }
    }
}

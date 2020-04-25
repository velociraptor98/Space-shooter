using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tpPowerup : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float speed = 3.0f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move()
    {
        transform.Translate(Vector3.down*speed*Time.deltaTime);
        if (this.transform.position.y < -10.5f)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")==true)
        {
            GameObject player = GameObject.Find("Player");
            player.GetComponent<Movement>().SetActive();
            Destroy(this.gameObject);
        }
    }
}

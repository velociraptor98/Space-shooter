using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float rotSpeed = 3.0f;
    [SerializeField] GameObject explosion;
    private SpawnManager spawnManager;

    private void Start()
    {
        spawnManager = GameObject.Find("SpawnManager").GetComponent<SpawnManager>();
    }
    void Update()
    {
        transform.Rotate(Vector3.forward*rotSpeed*Time.deltaTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Bullet"))
        {
            if (explosion)
            {
                GameObject.Instantiate(explosion,this.transform.position,this.transform.rotation);
                Destroy(collision.gameObject);
                spawnManager.StartSpawn();
                Destroy(this.gameObject,0.2f);
            }
        }
    }
}

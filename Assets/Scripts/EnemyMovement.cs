using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 8.0f;
    private GameObject player;
    private Animator animator;
    // Update is called once per frame
    private AudioSource source;
    private void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        source = this.GetComponent<AudioSource>();
    }
    void Update()
    {
        Movement();
    }

    private void Movement()
    {
        transform.Translate(Vector3.down*speed*Time.deltaTime);
        if (this.transform.position.y < -9.51)
        {
            float xValue = Random.Range(3.07f, 8.22f);
            transform.position=new Vector3(xValue,7.91f,0.0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.CompareTag("Bullet") == true)
        {
            animator.SetTrigger("OnEnemyDeath");
            Destroy(other.gameObject);
            this.speed = 0.0f;
            Destroy(this.gameObject,2.8f);
            player.GetComponent<Movement>().AddScore();
            source.Play();
            
        }
        else if (other.transform.CompareTag("Player")==true)
        {
            animator.SetTrigger("OnEnemyDeath");
            this.speed = 0.0f;
            other.transform.GetComponent<Movement>().OnDamage();
            Destroy(this.gameObject,2.8f);
            source.Play();
        }
    }
}

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
    [SerializeField] private GameObject laser;
    private float fireRate = 3.0f;
    private float canFire = -1f;
    private void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        source = this.GetComponent<AudioSource>();
    }
    void Update()
    {
        CalculateMovement();
        if(Time.time > canFire)
        {
            fireRate = Random.Range(3f, 6f);
            canFire = Time.time + fireRate;
            GameObject enemyLaser = Instantiate(laser, transform.position, Quaternion.identity);
            Projectile[] projectile = enemyLaser.GetComponentsInChildren<Projectile>();
            for(int i =0;i<projectile.Length;++i)
            {
                projectile[i].SetEnemyLaser();
            }
        }
    }


    void CalculateMovement()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
        if (this.transform.position.y < -9.51)
        {
            float xValue = Random.Range(3.07f, 8.22f);
            transform.position = new Vector3(xValue, 7.91f, 0.0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.CompareTag("Bullet") == true)
        {
            animator.SetTrigger("OnEnemyDeath");
            Destroy(other.gameObject);
            this.speed = 0.0f;
            Destroy(gameObject.GetComponent<Collider2D>());
            Destroy(this.gameObject,2.8f);
            player.GetComponent<Movement>().AddScore();
            source.Play();
        }
        else if (other.transform.CompareTag("Player")==true)
        {
            animator.SetTrigger("OnEnemyDeath");
            this.speed = 0.0f;
            other.transform.GetComponent<Movement>().OnDamage();
            Destroy(gameObject.GetComponent<Collider2D>());
            Destroy(this.gameObject,2.8f);
            source.Play();
        }
    }
}

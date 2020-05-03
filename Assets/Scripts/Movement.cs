using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Movement : MonoBehaviour
{
    [SerializeField] private float playerSpeed = 20.0f;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject tripleProjectile;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private int life = 3;
    [SerializeField]private int score = 0;
    private float timeToNextBullet = -1.0f;
    [SerializeField]
    private bool isTripleActive = false;
    [SerializeField] private bool isSpeedActive = false;
    [SerializeField] private bool isShieldActive = false;
    [SerializeField] private GameObject shield;
    [SerializeField] private GameObject leftFire, rightFire;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip laserclip;
    [SerializeField] private AudioClip powerclip;
    private UManager UIManager;
    private GameObject spawn;
    // Start is called before the first frame update
    void Start()
    {
        spawn = GameObject.Find("SpawnManager");
        shield.SetActive(false);
        UIManager = GameObject.FindGameObjectWithTag("UI").GetComponent<UManager>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        if (Input.GetKey(KeyCode.Space) && Time.time>timeToNextBullet)
        {
            Fire();
        }
    }

    private void Move()
    {
        float MoveHorizontal = Input.GetAxisRaw("Horizontal");
        float MoveVertical = Input.GetAxisRaw("Vertical");
        transform.Translate(new Vector3(MoveHorizontal, MoveVertical, 0.0f) * Time.deltaTime * playerSpeed);
        if (transform.position.x <= -14.5f)
        {
            transform.position = new Vector3(27.0f, transform.position.y, 0.0f);
        }
        else if (transform.position.x >= 27.0f)
        {
            transform.position = new Vector3(-14.5f, transform.position.y, 0.0f);
        }

        if (transform.position.y >= 10.0f)
        {
            transform.position = new Vector3(transform.position.x, -10.0f, 0.0f);
        }
        else if (transform.position.y <= -10.0f)
        {
            transform.position = new Vector3(transform.position.x, 10.0f, 0.0f);
        }
    }

    private void Fire()
       {
           if (projectile || tripleProjectile)
           {
               if (isTripleActive == false)
               {
                   timeToNextBullet = Time.time + fireRate;
                   Instantiate(projectile, new Vector3(this.transform.position.x, transform.position.y + 0.4f, 0.0f), Quaternion.identity);
               }
               else if (isTripleActive == true)
               {
                   timeToNextBullet = Time.time + fireRate;
                   Instantiate(tripleProjectile, new Vector3(this.transform.position.x-1.4f, transform.position.y + 0.4f, 0.0f), Quaternion.identity);
               }
           }
        source.clip = laserclip;
        source.Play();
       }

    public void OnDamage()
    {
        if(isShieldActive)
        {
            isShieldActive = false;
            shield.SetActive(false); 
            return;
        }
        --life;
        if(life == 2)
        {
            leftFire.SetActive(true);
        }
        if(life == 1)
        {
            rightFire.SetActive(true);
        }
        UIManager.UpdateLives(life);
        if (life <= 0)
        {
            spawn.GetComponent<SpawnManager>().PlayerDead();
            Destroy(this.gameObject);
            UIManager.GameOver();
        }
    }

    public void SetActive()
    {
        source.clip = powerclip;
        source.Play();
        this.isTripleActive = true;
        StartCoroutine(DisableTriple());
    }
    public void SetSpeedActive()
    {
        source.clip = powerclip;
        source.Play();
        this.isSpeedActive = true;
        playerSpeed *= 1.5f;
        StartCoroutine(DisableSpeed());
    }
    public void SetShieldActive()
    {
        source.clip = powerclip;
        source.Play();
        this.isShieldActive = true;
        shield.SetActive(true);
    }
    private IEnumerator DisableSpeed()
    {
        yield return new WaitForSeconds(8.0f);
        isSpeedActive = false;
        playerSpeed /= 1.5f;
    }
    private IEnumerator DisableTriple()
    {
        yield return new WaitForSeconds(8.0f);
        isTripleActive = false;
    }
    public void AddScore()
    {
        score += 10;
        UIManager.UpdateText();

    }
    public int GetScore()
    {
        return score;
    }
    public int GetLife()
    {
        return life;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    // How quickly the ship reaches (and sheds) full speed, in units per second squared.
    [SerializeField] private float acceleration = 150.0f;
    // Sprites from level flight (index 0) to a full bank, used as the ship moves sideways.
    [SerializeField] private Sprite[] bankLeftFrames;
    [SerializeField] private Sprite[] bankRightFrames;
    [SerializeField] private GameObject hitSparks;
    // The only part of the ship bullets can hit, in world units. Far smaller than the sprite, as in any bullet hell.
    [SerializeField] private float hitboxRadius = 0.1f;
    // Shown while focusing so the player can thread gaps precisely.
    [SerializeField] private GameObject hitboxMarker;
    // Holding Focus slows the ship to this fraction of its speed.
    [SerializeField] private float focusSpeedMultiplier = 0.45f;
    [SerializeField] private float invulnerableTime = 2.0f;
    [SerializeField] private float blinkInterval = 0.08f;
    // How far inside the screen edge the ship's centre is kept.
    [SerializeField] private Vector2 screenMargin = new Vector2(0.7f, 0.9f);
    private UManager UIManager;
    private GameObject spawn;
    private InputAction moveAction;
    private InputAction fireAction;
    private InputAction focusAction;
    private SpriteRenderer body;
    private Vector2 velocity;
    private Rect playArea;
    private float invulnerableUntil;
    private bool controlsEnabled = true;

    // The live player ship, or null once it has been destroyed.
    public static Movement Instance { get; private set; }
    public float HitboxRadius => hitboxRadius;
    public bool IsVulnerable => controlsEnabled && Time.time >= invulnerableUntil;

    private void Awake()
    {
        Instance = this;
        Rect view = Playfield.Bounds;
        playArea = new Rect(view.xMin + screenMargin.x, view.yMin + screenMargin.y,
                            view.width - screenMargin.x * 2.0f, view.height - screenMargin.y * 2.0f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        spawn = GameObject.Find("SpawnManager");
        shield.SetActive(false);
        UIManager = GameObject.FindGameObjectWithTag("UI").GetComponent<UManager>();
        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        fireAction = InputSystem.actions.FindAction("Player/Fire", true);
        focusAction = InputSystem.actions.FindAction("Player/Focus", true);
        body = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!controlsEnabled)
        {
            return;
        }
        bool focusing = focusAction.IsPressed();
        if (hitboxMarker)
        {
            hitboxMarker.SetActive(focusing);
        }
        Move(focusing);
        Blink();
        if (fireAction.IsPressed() && Time.time>timeToNextBullet)
        {
            Fire();
        }
    }

    // Flies the ship up from below the screen into its starting spot, with controls locked until it arrives.
    public void PlayIntro(float duration)
    {
        StartCoroutine(FlyIn(duration));
    }

    private IEnumerator FlyIn(float duration)
    {
        controlsEnabled = false;
        Vector3 end = transform.position;
        Vector3 start = new Vector3(end.x, playArea.yMin - 4.0f, end.z);
        for (float t = 0.0f; t < duration; t += Time.deltaTime)
        {
            float eased = 1.0f - Mathf.Pow(1.0f - t / duration, 3.0f);
            transform.position = Vector3.LerpUnclamped(start, end, eased);
            yield return null;
        }
        transform.position = end;
        controlsEnabled = true;
    }

    private void Move(bool focusing)
    {
        // Ease towards the input velocity rather than snapping to it, so starts, stops and turns glide.
        float speed = playerSpeed * (focusing ? focusSpeedMultiplier : 1.0f);
        Vector2 targetVelocity = moveAction.ReadValue<Vector2>() * speed;
        velocity = Vector2.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        Vector2 position = (Vector2)transform.position + velocity * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, playArea.xMin, playArea.xMax);
        position.y = Mathf.Clamp(position.y, playArea.yMin, playArea.yMax);
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        Bank();
    }

    private void Bank()
    {
        float bank = Mathf.Clamp(velocity.x / playerSpeed, -1.0f, 1.0f);
        Sprite[] frames = bank < 0.0f ? bankLeftFrames : bankRightFrames;
        if (frames == null || frames.Length == 0)
        {
            return;
        }
        body.sprite = frames[Mathf.RoundToInt(Mathf.Abs(bank) * (frames.Length - 1))];
    }

    // Flicker the ship while it can't be hit, the classic signal for recovery time.
    private void Blink()
    {
        bool invulnerable = Time.time < invulnerableUntil;
        body.enabled = !invulnerable || Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
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
        if (!IsVulnerable)
        {
            return;
        }
        if (hitSparks)
        {
            Instantiate(hitSparks, transform.position, Quaternion.identity);
        }
        // Wipe the screen and grant a moment's grace, so one mistake doesn't cascade into several hits.
        BulletSystem.Clear();
        invulnerableUntil = Time.time + invulnerableTime;
        if(isShieldActive)
        {
            isShieldActive = false;
            shield.SetActive(false);
            GameFeel.Impact(0.3f);
            return;
        }
        --life;
        // Losing the ship hits hardest; any other hull damage still jolts the screen.
        GameFeel.Impact(life <= 0 ? 1.0f : 0.55f, life <= 0 ? 0.15f : 0.06f);
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
    public void AddScore(int points)
    {
        score += points;
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

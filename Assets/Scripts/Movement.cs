using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private float playerSpeed = 20.0f;
    [SerializeField] private GameObject projectile;
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
    // How quickly the ship's speed follows the stick: higher is snappier. Speed eases in and out
    // exponentially, so starts and stops glide rather than lurch.
    [SerializeField] private float responsiveness = 12.0f;
    // The ship's sprite, on a child that stays upright and swaps between pre-rotated frames instead of
    // being rotated itself (rotating pixel art on the fly makes its pixels crawl).
    [SerializeField] private SpriteRenderer body;
    // Frame i faces i steps anticlockwise from straight up, evenly round the circle.
    [SerializeField] private Sprite[] rotationFrames;
    // Children that look the same at any angle (shield, hitbox) and so are also kept upright.
    [SerializeField] private Transform[] keepUpright;
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
    // Roughly how long the ship takes to swing round to its aim point. Short, so aiming stays direct,
    // but eased so small mouse movements don't jitter the ship.
    [SerializeField] private float turnSmoothTime = 0.05f;
    // Where shots leave the ship, ahead of its centre along its facing.
    [SerializeField] private float noseOffset = 0.9f;
    // Angle between the three shots of the Triple Shot power-up.
    [SerializeField] private float tripleSpread = 12.0f;
    // Crosshair drawn at the aim point, replacing the mouse cursor during play.
    [SerializeField] private Transform reticle;
    // With a gamepad the crosshair floats this far ahead of the ship, along the right stick.
    [SerializeField] private float gamepadReticleDistance = 4.0f;
    // Barrel roll: a quick dodge in the direction of movement that bullets pass straight through.
    [SerializeField] private float rollDuration = 0.3f;
    // Speed at the start of the roll's dash, easing back to normal speed by its end.
    [SerializeField] private float rollSpeed = 48.0f;
    [SerializeField] private float rollCooldown = 0.6f;
    [SerializeField] private int rollAfterimages = 3;
    [SerializeField] private GameObject afterimagePrefab;
    [SerializeField] private Color afterimageTint = new Color(0.45f, 0.94f, 0.97f, 0.55f);
    // Tint while the ship's underside is showing, mid-roll.
    [SerializeField] private Color undersideTint = new Color(0.62f, 0.68f, 0.8f, 1.0f);
    private UManager UIManager;
    private GameObject spawn;
    private InputAction moveAction;
    private InputAction fireAction;
    private InputAction focusAction;
    private InputAction pointAction;
    private InputAction aimAction;
    private InputAction rollAction;
    private Vector2 aimPoint;
    private Vector2 lastPointer;
    private bool aimingWithStick;
    private Vector2 velocity;
    private float heading;
    private float turnVelocity;
    private Rect playArea;
    private float invulnerableUntil;
    private bool controlsEnabled = true;
    private float rollStartTime = -100.0f;
    private Vector2 rollDirection;
    private int afterimagesLeft;

    // The live player ship, or null once it has been destroyed.
    public static Movement Instance { get; private set; }
    public float HitboxRadius => hitboxRadius;
    public bool IsVulnerable => controlsEnabled && Time.time >= invulnerableUntil && !IsRolling;
    // Where the ship is aiming, in world space.
    public Vector2 AimPoint => aimPoint;
    public bool IsRolling => Time.time < rollStartTime + rollDuration;
    // 0 to 1 through the current roll.
    private float RollProgress => Mathf.Clamp01((Time.time - rollStartTime) / rollDuration);

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
        Cursor.visible = true;
        if (reticle)
        {
            reticle.gameObject.SetActive(false);
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
        pointAction = InputSystem.actions.FindAction("Player/Point", true);
        aimAction = InputSystem.actions.FindAction("Player/Aim", true);
        rollAction = InputSystem.actions.FindAction("Player/Roll", true);
        aimPoint = (Vector2)transform.position + Vector2.up * gamepadReticleDistance;
        // The crosshair takes over from the system cursor while the ship is alive.
        Cursor.visible = false;
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
        if (rollAction.WasPressedThisFrame() && Time.time >= rollStartTime + rollDuration + rollCooldown)
        {
            StartRoll();
        }
        Move(focusing);
        Aim();
        Blink();
        LeaveAfterimages();

        // No firing mid-roll: the dodge is a commitment.
        if (!IsRolling && fireAction.IsPressed() && Time.time>timeToNextBullet)
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

    // Dashes in the direction being steered (or rolls on the spot with no input), untouchable until it ends.
    private void StartRoll()
    {
        rollStartTime = Time.time;
        Vector2 input = moveAction.ReadValue<Vector2>();
        rollDirection = input.sqrMagnitude > 0.01f ? input.normalized : Vector2.zero;
        afterimagesLeft = rollAfterimages;
    }

    private void Move(bool focusing)
    {
        float speed = playerSpeed * (focusing ? focusSpeedMultiplier : 1.0f);
        Vector2 targetVelocity = moveAction.ReadValue<Vector2>() * speed;
        if (IsRolling && rollDirection != Vector2.zero)
        {
            // Burst out at roll speed and ease back to normal speed by the end of the roll.
            float eased = 1.0f - Mathf.Pow(1.0f - RollProgress, 2.0f);
            velocity = rollDirection * Mathf.Lerp(rollSpeed, playerSpeed, eased);
        }
        else
        {
            // Frame-rate independent exponential ease towards the target velocity.
            velocity = Vector2.Lerp(velocity, targetVelocity, 1.0f - Mathf.Exp(-responsiveness * Time.deltaTime));
        }
        Vector2 position = (Vector2)transform.position + velocity * Time.deltaTime;
        // Stop dead against the screen edge, so reversing away from it responds at once.
        if (position.x < playArea.xMin || position.x > playArea.xMax)
        {
            velocity.x = 0.0f;
        }
        if (position.y < playArea.yMin || position.y > playArea.yMax)
        {
            velocity.y = 0.0f;
        }
        position.x = Mathf.Clamp(position.x, playArea.xMin, playArea.xMax);
        position.y = Mathf.Clamp(position.y, playArea.yMin, playArea.yMax);
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    // Turns the ship towards the aim point: the mouse pointer, or the right stick's direction on a gamepad.
    // Whichever was used last wins, so either can be picked up at any time.
    private void Aim()
    {
        Vector2 stick = aimAction.ReadValue<Vector2>();
        Vector2 pointer = pointAction.ReadValue<Vector2>();
        if (stick.sqrMagnitude > 0.1f)
        {
            aimingWithStick = true;
            aimPoint = (Vector2)transform.position + stick.normalized * gamepadReticleDistance;
        }
        else if (pointer != lastPointer || !aimingWithStick)
        {
            aimingWithStick = false;
            aimPoint = Playfield.ScreenToWorld(pointer);
        }
        else
        {
            // Keep a gamepad crosshair at the same offset as the ship moves.
            aimPoint += velocity * Time.deltaTime;
        }
        lastPointer = pointer;
        // Keep the crosshair on screen even if the pointer strays into the borders or off the window.
        Rect view = Playfield.View;
        aimPoint = new Vector2(Mathf.Clamp(aimPoint.x, view.xMin, view.xMax), Mathf.Clamp(aimPoint.y, view.yMin, view.yMax));

        Vector2 toAim = aimPoint - (Vector2)transform.position;
        if (toAim.sqrMagnitude > 0.01f)
        {
            // The sprite faces up, so measure the heading from straight up.
            float target = Mathf.Atan2(toAim.y, toAim.x) * Mathf.Rad2Deg - 90.0f;
            heading = Mathf.SmoothDampAngle(heading, target, ref turnVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, 0.0f, heading);
        }
        if (reticle)
        {
            reticle.position = new Vector3(aimPoint.x, aimPoint.y, reticle.position.z);
        }
    }

    // The ship itself rotates (so shots, engines and damage fires follow its aim), but its sprite stays
    // upright and shows the pre-rotated frame nearest the current heading.
    private void LateUpdate()
    {
        if (IsRolling && rotationFrames != null && rotationFrames.Length > 0)
        {
            // Mid-roll the sprite turns with the ship and is squashed across its width, flipping through
            // edge-on to its darker underside and back - a full spin about its length. It's only for a
            // moment and moving fast, so rotating it directly doesn't show the pixel crawl.
            float spin = Mathf.Cos(RollProgress * Mathf.PI * 2.0f);
            body.sprite = rotationFrames[0];
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(spin, 1.0f, 1.0f);
            body.color = spin < 0.0f ? undersideTint : Color.white;
        }
        else
        {
            if (rotationFrames != null && rotationFrames.Length > 0)
            {
                float step = 360.0f / rotationFrames.Length;
                int frame = Mathf.RoundToInt(Mathf.Repeat(transform.eulerAngles.z, 360.0f) / step) % rotationFrames.Length;
                body.sprite = rotationFrames[frame];
            }
            body.transform.rotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;
            body.color = Color.white;
        }
        foreach (Transform child in keepUpright)
        {
            child.rotation = Quaternion.identity;
        }
    }

    // Drops fading copies of the ship at even points through a roll, streaking its path.
    private void LeaveAfterimages()
    {
        if (afterimagesLeft <= 0 || !IsRolling)
        {
            return;
        }
        float due = 1.0f - afterimagesLeft / (float)(rollAfterimages + 1);
        if (RollProgress >= due)
        {
            Afterimage.Spawn(afterimagePrefab, body, afterimageTint, 0.25f);
            --afterimagesLeft;
        }
    }

    // Flicker the ship while it can't be hit, the classic signal for recovery time.
    private void Blink()
    {
        bool invulnerable = Time.time < invulnerableUntil;
        body.enabled = !invulnerable || Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
    }

    // Shots leave the nose along the ship's facing; Triple Shot fans them out either side.
    private void Fire()
    {
        timeToNextBullet = Time.time + fireRate;
        Vector3 nose = transform.position + transform.up * noseOffset;
        if (isTripleActive)
        {
            for (int i = -1; i <= 1; ++i)
            {
                PoolManager.Spawn(projectile, nose, transform.rotation * Quaternion.Euler(0.0f, 0.0f, i * tripleSpread));
            }
        }
        else
        {
            PoolManager.Spawn(projectile, nose, transform.rotation);
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
            PoolManager.Spawn(hitSparks, transform.position, Quaternion.identity);
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

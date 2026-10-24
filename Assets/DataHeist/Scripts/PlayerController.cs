using UnityEngine;
using UnityEngine.InputSystem;

// Moves the player: automatic running, jumping, double jumping and sliding.
// It also picks which animation frames to show.
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;          // how fast the player runs to the right
    public float jumpForce = 15.5f;         // upward speed of a normal jump
    public float doubleJumpForce = 14f;     // upward speed of the double jump
    public float maxFallSpeed = 26f;

    [Header("Double jump")]
    public int coinsForDoubleJump = 30;     // coins needed in this run to unlock the double jump
    public bool canDoubleJump = false;      // becomes true after collecting 30 coins

    [Header("Jump help")]
    public float coyoteTime = 0.1f;         // you can still jump for a moment after running off an edge

    [Header("Slide")]
    public float slideTime = 0.6f;          // how long one slide lasts

    [Header("Ground check")]
    public Transform groundCheck;           // a point at the player's feet
    public LayerMask groundLayer;           // what counts as ground
    public LayerMask ceilingLayers;         // things the player must not stand up inside

    [Header("Animation")]
    public Sprite[] runFrames;
    public Sprite[] jumpFrames;
    public Sprite[] fallFrames;
    public Sprite[] slideFrames;
    public Sprite[] attackFrames;
    public float framesPerSecond = 12f;

    [Header("Sounds")]
    public AudioClip jumpSound;
    public AudioClip doubleJumpSound;
    public AudioClip slideSound;

    Rigidbody2D rb;
    BoxCollider2D box;
    SpriteRenderer spriteRenderer;
    PlayerAttack playerAttack;

    bool isGrounded = false;
    bool usedDoubleJump = false;
    bool isSliding = false;
    float slideTimer = 0f;
    float coyoteTimer = 0f;
    float animationTimer = 0f;

    Vector2 standingSize;                   // collider size when standing
    Vector2 standingOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAttack = GetComponent<PlayerAttack>();
        standingSize = box.size;
        standingOffset = box.offset;
    }

    void Update()
    {
        // Stop moving when the run is over
        if (GameManager.instance.isGameOver)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            return;
        }

        // Are we standing on the ground? (only when not moving upward)
        isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(0.5f, 0.1f), 0f, groundLayer) != null
                     && rb.linearVelocity.y <= 0.1f;
        if (isGrounded)
        {
            usedDoubleJump = false;         // landing gives the double jump back
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer = coyoteTimer - Time.deltaTime;
        }

        // Unlock the double jump after collecting enough coins in this run
        if (!canDoubleJump && GameManager.instance.coinCount >= coinsForDoubleJump)
        {
            canDoubleJump = true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // ---------- Jump and double jump (Space) ----------
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            if (coyoteTimer > 0f)          // on the ground, or just ran off an edge
            {
                Jump(jumpForce, jumpSound);
                coyoteTimer = 0f;
            }
            else if (canDoubleJump && !usedDoubleJump)
            {
                Jump(doubleJumpForce, doubleJumpSound);
                usedDoubleJump = true;
            }
        }

        // Letting go of Space early makes a smaller jump
        if (keyboard.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        // ---------- Slide (S or Down Arrow) ----------
        bool slidePressed = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
        if (slidePressed && isGrounded && !isSliding)
        {
            StartSlide();
        }

        if (isSliding)
        {
            slideTimer = slideTimer - Time.deltaTime;
            // Only stand up when the time is over AND nothing is above the player
            if (slideTimer <= 0f && CanStandUp())
            {
                StopSlide();
            }
        }

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (GameManager.instance.isGameOver)
        {
            return;
        }

        // Always run to the right. Keep the current up/down speed, but limit how fast we fall.
        float fallSpeed = Mathf.Max(rb.linearVelocity.y, -maxFallSpeed);
        rb.linearVelocity = new Vector2(moveSpeed, fallSpeed);
    }

    void Jump(float force, AudioClip sound)
    {
        if (isSliding)
        {
            if (!CanStandUp())
            {
                return;                     // something is above us, so we cannot jump
            }
            StopSlide();
        }
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        GameManager.instance.PlaySound(sound);
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideTime;
        // Make the collider half as tall so the player fits under barriers and bullets
        box.size = new Vector2(standingSize.x, standingSize.y * 0.45f);
        box.offset = new Vector2(standingOffset.x, box.size.y / 2f);
        GameManager.instance.PlaySound(slideSound);
    }

    void StopSlide()
    {
        isSliding = false;
        box.size = standingSize;
        box.offset = standingOffset;
    }

    // Checks the space above a sliding player. If a barrier or roof is there, we stay down.
    bool CanStandUp()
    {
        Vector2 checkCenter = (Vector2)transform.position + new Vector2(0f, standingSize.y * 0.75f);
        Vector2 checkSize = new Vector2(standingSize.x * 0.9f, standingSize.y * 0.5f);
        return Physics2D.OverlapBox(checkCenter, checkSize, 0f, ceilingLayers) == null;
    }

    // Shows the right animation for what the player is doing
    void UpdateAnimation()
    {
        Sprite[] frames = runFrames;
        if (playerAttack.IsAttacking())
        {
            frames = attackFrames;
        }
        else if (isSliding)
        {
            frames = slideFrames;
        }
        else if (!isGrounded && rb.linearVelocity.y > 0)
        {
            frames = jumpFrames;
        }
        else if (!isGrounded)
        {
            frames = fallFrames;
        }

        animationTimer = animationTimer + Time.deltaTime;
        int frameNumber = (int)(animationTimer * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frameNumber];
    }

    public bool IsSliding()
    {
        return isSliding;
    }
}

using UnityEngine;

// Keeps track of the player's health. Enemies, bullets and obstacles call TakeDamage().
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int playerHealth;
    public float damageCooldown = 1.25f;    // time after a hit when the player cannot be hurt again
    public float fallLimit = -9f;           // falling below this height ends the run
    public AudioClip hurtSound;

    float cooldownTimer = 0f;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        // Extra hearts bought in the Shop add to the starting health
        maxHealth = maxHealth + SaveManager.GetExtraHearts();
        playerHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Blink while the damage cooldown is running
        if (cooldownTimer > 0f)
        {
            cooldownTimer = cooldownTimer - Time.deltaTime;
            spriteRenderer.enabled = (int)(cooldownTimer * 10f) % 2 == 0;
        }
        else
        {
            spriteRenderer.enabled = true;
        }

        // Fell between the rooftops
        if (transform.position.y < fallLimit && !GameManager.instance.isGameOver)
        {
            playerHealth = 0;
            GameManager.instance.GameOver("You fell!");
        }
    }

    public void TakeDamage(int amount)
    {
        // No damage during the cooldown, so one collision only removes health once
        if (cooldownTimer > 0f || GameManager.instance.isGameOver)
        {
            return;
        }

        playerHealth = playerHealth - amount;
        cooldownTimer = damageCooldown;
        GameManager.instance.PlaySound(hurtSound);

        if (playerHealth <= 0)
        {
            playerHealth = 0;
            GameManager.instance.GameOver("Out of health!");
        }
    }
}

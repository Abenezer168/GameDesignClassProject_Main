using UnityEngine;
using UnityEngine.InputSystem;

// A simple melee attack: press J or click the left mouse button to hit enemies in front of the player.
public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 1.8f;        // how far in front of the player the attack reaches
    public float attackDuration = 0.25f;    // how long one attack can hit
    public float attackCooldown = 0.35f;    // time before the player can attack again
    public AudioClip attackSound;
    public AudioClip enemyDestroyedSound;

    float attackTimer = 0f;                 // > 0 while attacking
    float cooldownTimer = 0f;               // > 0 while waiting to attack again

    void Update()
    {
        if (GameManager.instance.isGameOver)
        {
            return;
        }

        attackTimer = attackTimer - Time.deltaTime;
        cooldownTimer = cooldownTimer - Time.deltaTime;

        // Did the player press J or the left mouse button?
        bool attackPressed = false;
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            attackPressed = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            attackPressed = true;
        }

        if (attackPressed && cooldownTimer <= 0f)
        {
            attackTimer = attackDuration;
            cooldownTimer = attackCooldown;
            GameManager.instance.PlaySound(attackSound);
        }

        // While the attack is active, destroy any enemy inside the attack area
        if (attackTimer > 0f)
        {
            HitEnemies();
        }
    }

    void HitEnemies()
    {
        // A box in front of the player, from the feet up to above the head
        Vector2 center = (Vector2)transform.position + new Vector2(0.3f + attackRange / 2f, 1.0f);
        Vector2 size = new Vector2(attackRange, 2.0f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Die();
                GameManager.instance.PlaySound(enemyDestroyedSound);
            }
        }
    }

    public bool IsAttacking()
    {
        return attackTimer > 0f;
    }
}

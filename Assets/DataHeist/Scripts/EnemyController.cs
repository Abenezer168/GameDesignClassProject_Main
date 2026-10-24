using UnityEngine;

// The two kinds of enemy in the game
public enum EnemyType
{
    ScoutDrone,     // flies back and forth a short distance
    Gunner          // stays in place and shoots at the player
}

// Controls scout drones and gunners. Touching either one hurts the player; attacking destroys it.
public class EnemyController : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.ScoutDrone;

    [Header("Scout drone")]
    public float patrolDistance = 1f;       // how far it flies left and right from where it starts
    public float patrolSpeed = 2f;
    public float bobHeight = 0.15f;         // small up and down movement

    [Header("Gunner")]
    public GameObject projectilePrefab;     // the bullet it fires
    public Transform firePoint;             // where bullets appear
    public float shootInterval = 1.45f;     // seconds between shots
    public float shootRange = 15f;          // starts shooting when the player is this close
    public float minShootDistance = 6f;     // stops shooting when the player is this close
    public AudioClip shootSound;

    Vector3 startPosition;
    float shootTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (enemyType == EnemyType.ScoutDrone)
        {
            // Fly left and right, and bob up and down
            float x = startPosition.x + Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
            float y = startPosition.y + Mathf.Sin(Time.time * 3f) * bobHeight;
            transform.position = new Vector3(x, y, 0f);
        }
        else
        {
            // The gunner only bobs a little and shoots when the player is in range
            transform.position = startPosition + new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.1f, 0f);

            Transform player = GameManager.instance.player;
            float distanceToPlayer = transform.position.x - player.position.x;
            if (distanceToPlayer > minShootDistance && distanceToPlayer < shootRange && !GameManager.instance.isGameOver)
            {
                shootTimer = shootTimer - Time.deltaTime;
                if (shootTimer <= 0f)
                {
                    Shoot();
                    shootTimer = shootInterval;
                }
            }
        }
    }

    void Shoot()
    {
        Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        GameManager.instance.PlaySound(shootSound);
    }

    // Touching an enemy hurts the player
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().TakeDamage(1);
        }
    }

    // Called by PlayerAttack when the player hits this enemy
    public void Die()
    {
        Destroy(gameObject);
    }
}

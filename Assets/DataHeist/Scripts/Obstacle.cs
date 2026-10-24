using UnityEngine;

// An obstacle such as a trash can (jump over it) or a barrier (slide under it).
// Touching it removes one health point. PlayerHealth's cooldown stops repeated damage.
public class Obstacle : MonoBehaviour
{
    public int damage = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().TakeDamage(damage);
        }
    }
}

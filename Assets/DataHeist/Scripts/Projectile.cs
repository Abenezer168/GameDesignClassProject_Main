using UnityEngine;

// A gunner's bullet. It flies to the left and hurts the player on contact.
public class Projectile : MonoBehaviour
{
    public float speed = 7.5f;
    public float lifeTime = 4f;             // removed after this many seconds so bullets don't pile up

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position = transform.position + Vector3.left * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().TakeDamage(1);
            Destroy(gameObject);
        }
    }
}

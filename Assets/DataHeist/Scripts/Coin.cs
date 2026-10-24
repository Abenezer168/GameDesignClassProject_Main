using UnityEngine;

// A coin (Data Byte). Touching it adds one coin to this run and removes the coin.
public class Coin : MonoBehaviour
{
    bool collected = false;                 // stops the same coin from being counted twice

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!collected && other.CompareTag("Player"))
        {
            collected = true;
            GameManager.instance.AddCoin();
            Destroy(gameObject);
        }
    }
}

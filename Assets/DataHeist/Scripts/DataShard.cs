using UnityEngine;

// The Data Shard: the main objective. The player must collect it before reaching the end of the level.
public class DataShard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.CollectDataShard();
            Destroy(gameObject);
        }
    }
}

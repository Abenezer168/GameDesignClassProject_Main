using UnityEngine;

// Plays a list of sprites in a loop, like a flip book. Used for coins, drones, gunners and the Data Shard.
public class SimpleAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 8f;

    SpriteRenderer spriteRenderer;
    float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (frames.Length == 0)
        {
            return;
        }
        timer = timer + Time.deltaTime;
        int frameNumber = (int)(timer * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frameNumber];
    }
}

using UnityEngine;

// A basic side-scrolling camera that follows the player.
public class CameraFollow : MonoBehaviour
{
    public Transform target;                // the player
    public float lookAhead = 4f;            // show more of the level in front of the player
    public float heightOffset = 2f;         // keep the player a little below the middle of the screen
    public float followSpeed = 4f;          // how quickly the camera follows up and down
    public float lowestY = -1f;             // the camera does not follow the player down into a gap

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = target.position.x + lookAhead;
        float targetY = Mathf.Max(target.position.y + heightOffset, lowestY);
        position.y = Mathf.Lerp(position.y, targetY, followSpeed * Time.deltaTime);
        transform.position = position;
    }
}

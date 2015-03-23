using UnityEngine;
using System.Collections;

/// <summary>
/// A base class for all world entities (items, players, NPCs), automatically
/// exposes useful elements like the GameManager, SpriteManager, and local
/// SpriteRenderer
/// </summary>
public abstract class Entity : MonoBehaviour
{
    public SpriteRenderer renderer;
    public Point position;
    
    protected void Start()
    {
        renderer = gameObject.AddComponent<SpriteRenderer>();

        position = new Point((int)transform.position.x, (int)transform.position.y);
    }

    // Smoothly move the object towards the target position
    protected IEnumerator SmoothMove(Point original, Point targetPoint, float moveDuration)
    {
        transform.position = (Vector2)original;

        float moveSpeed = targetPoint.Distance(original) / moveDuration;

        Vector2 target = new Vector2(targetPoint.x, targetPoint.y);

        // Find an indication of remaining distance to the target
        float remainingDistance = ((Vector2)transform.position - target).sqrMagnitude;

        while (remainingDistance > float.Epsilon)
        {
            // Move closer to the target position, based on time and move speed
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

            //Recalculate the remaining distance after moving
            remainingDistance = ((Vector2)transform.position - target).sqrMagnitude;

            //Return and loop until remainingDistance is close enough to zero to end the function
            yield return null;
        }

        // Just in case we didn't exactly hit zero, we should drop any remaining error
        transform.position = target;

        if (this is Effect)
            Destroy(this.gameObject);
    }
}

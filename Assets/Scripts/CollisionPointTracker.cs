using UnityEngine;

public class CollisionPointTracker : MonoBehaviour
{
    private Vector3 collisionPoint1;
    private Vector3 collisionPoint2;
    private bool hasCollision = false;

    public bool HasCollision()
    {
        return hasCollision;
    }

    public Vector3 GetCollisionPoint1()
    {
        return collisionPoint1;
    }

    public Vector3 GetCollisionPoint2()
    {
        return collisionPoint2;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount >= 2)
        {
            collisionPoint1 = collision.contacts[0].point;
            collisionPoint2 = collision.contacts[1].point;
            hasCollision = true;
        }
        else if (collision.contactCount == 1)
        {
            collisionPoint1 = collision.contacts[0].point;
            collisionPoint2 = collision.contacts[0].point;
            hasCollision = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        hasCollision = false;
    }
}

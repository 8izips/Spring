using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpringCollider : MonoBehaviour
{
    public enum CollisionType
    {
        Sphere,
        Capsule
    }
    public CollisionType collisionType;

    public float radius = 0.25f;
    public float height = 0.5f;

    public bool Check(float childLength, Vector3 headPosition, ref Vector3 tailPosition, float tailRadius, ref Vector3 hitNormal)
    {   
        switch (collisionType) {
            case CollisionType.Sphere:
                return CheckSphereCollision(headPosition, ref tailPosition, tailRadius, ref hitNormal);
            case CollisionType.Capsule:
                return CheckCapsuleCollision(headPosition, ref tailPosition, tailRadius, ref hitNormal);
        }

        return false;
    }

    bool CheckSphereCollision(Vector3 headPosition, ref Vector3 tailPosition, float tailRadius, ref Vector3 hitNormal)
    {
        var localHeadPosition = transform.InverseTransformPoint(headPosition);
        var localTailPosition = transform.InverseTransformPoint(tailPosition);
        var localTailRadius = transform.InverseTransformDirection(tailRadius, 0f, 0f).magnitude;
        var sphereLocalOrigin = Vector3.zero;
        var combinedRadius = radius + localTailRadius;

        if ((localTailPosition - sphereLocalOrigin).sqrMagnitude >= combinedRadius * combinedRadius)
            return false;

        bool hitted = false;
        var originToHead = localHeadPosition - sphereLocalOrigin;
        if (originToHead.sqrMagnitude <= radius * radius) {
            // The head is inside the sphere, so just try to push the tail out
            localTailPosition = sphereLocalOrigin + (localTailPosition - sphereLocalOrigin).normalized * combinedRadius;
            hitted = true;
        }
        else {
            var localHeadRadius = (localTailPosition - localHeadPosition).magnitude;
            var aToB = sphereLocalOrigin - localHeadPosition;
            var dSqr = aToB.sqrMagnitude;
            var d = Mathf.Sqrt(dSqr);
            if (d > 0f) {
                var radiusASqr = localHeadRadius * localHeadRadius;
                var radiusBSqr = combinedRadius * combinedRadius;

                // Assume a is at the origin and b is at (d, 0 0)
                var denominator = 0.5f / d;
                var subTerm = dSqr - radiusBSqr + radiusASqr;
                var x = subTerm * denominator;
                var squaredTerm = subTerm * subTerm;
                
                var intersectionUpVector = aToB / d;
                var intersecttionPosition = localHeadPosition + x * intersectionUpVector;
                var intersectionRadius = Mathf.Sqrt(4f * dSqr * radiusASqr - squaredTerm) * denominator;

                var newTailPosition = localTailPosition - Vector3.Dot(intersectionUpVector, localTailPosition - intersecttionPosition) * intersectionUpVector;
                var v = newTailPosition - intersecttionPosition;
                localTailPosition = intersecttionPosition + intersectionRadius * v.normalized;
            }
            hitted = true;
        }

        if (hitted) {
            tailPosition = transform.TransformPoint(localTailPosition);
            hitNormal = transform.TransformDirection(localTailPosition.normalized).normalized;
            return true;
        }

        return false;
    }

    bool CheckCapsuleCollision(Vector3 headPosition, ref Vector3 tailPosition, float tailRadius, ref Vector3 hitNormal)
    {
        var localHeadPosition = transform.InverseTransformPoint(headPosition);
        var localMoverPosition = transform.InverseTransformPoint(tailPosition);
        var localMoverRadius = transform.InverseTransformDirection(tailRadius, 0f, 0f).magnitude;

        var moverIsAboveTop = localMoverPosition.y >= height;
        var useSphereCheck = (localMoverPosition.y <= 0f) | moverIsAboveTop;
        if (useSphereCheck) {
            var sphereOrigin = new Vector3(0f, moverIsAboveTop ? height : 0f, 0f);
            var combinedRadius = localMoverRadius + radius;
            if ((localMoverPosition - sphereOrigin).sqrMagnitude >= combinedRadius * combinedRadius) {
                return false;
            }

            var originToHead = localHeadPosition - sphereOrigin;
            var isHeadEmbedded = originToHead.sqrMagnitude <= radius * radius;

            if (isHeadEmbedded) {
                // The head is inside the sphere, so just try to push the tail out
                var localHitNormal = (localMoverPosition - sphereOrigin).normalized;
                localMoverPosition = sphereOrigin + localHitNormal * combinedRadius;
                tailPosition = transform.TransformPoint(localMoverPosition);
                hitNormal = transform.TransformDirection(localHitNormal).normalized;
                return true;
            }

            var localHeadRadius = (localMoverPosition - localHeadPosition).magnitude;
            var aToB = sphereOrigin - localHeadPosition;
            var dSqr = aToB.sqrMagnitude;
            var d = Mathf.Sqrt(dSqr);
            if (d > 0f) {
                var radiusASqr = localHeadRadius * localHeadRadius;
                var radiusBSqr = combinedRadius * combinedRadius;

                // Assume a is at the origin and b is at (d, 0 0)
                var denominator = 0.5f / d;
                var subTerm = dSqr - radiusBSqr + radiusASqr;
                var x = subTerm * denominator;
                var squaredTerm = subTerm * subTerm;

                var intersectionUpVector = aToB / d;
                var intersecttionPosition = localHeadPosition + x * intersectionUpVector;
                var intersectionRadius = Mathf.Sqrt(4f * dSqr * radiusASqr - squaredTerm) * denominator;

                var newTailPosition = localMoverPosition - Vector3.Dot(intersectionUpVector, localMoverPosition - intersecttionPosition) * intersectionUpVector;
                var v = newTailPosition - intersecttionPosition;
                localMoverPosition = intersecttionPosition + intersectionRadius * v.normalized;                
                tailPosition = transform.TransformPoint(localMoverPosition);
                var localHitNormal = (localMoverPosition - sphereOrigin).normalized;
                hitNormal = transform.TransformDirection(localHitNormal).normalized;
            }

            return true;
        }
        else {
            var originToMover = new Vector2(localMoverPosition.x, localMoverPosition.z);
            var combinedRadius = radius + localMoverRadius;

            if (originToMover.sqrMagnitude <= combinedRadius * combinedRadius) {
                var normal = originToMover.normalized;
                originToMover = combinedRadius * normal;
                var newLocalMoverPosition = new Vector3(originToMover.x, localMoverPosition.y, originToMover.y);
                tailPosition = transform.TransformPoint(newLocalMoverPosition);
                hitNormal = transform.TransformDirection(new Vector3(normal.x, 0f, normal.y)).normalized;

                var originToHead = new Vector2(localHeadPosition.x, localHeadPosition.z);
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    public enum DrawGizmoMode
    {
        None,
        Selected,
        Always
    }
    public DrawGizmoMode drawGizmoMode = DrawGizmoMode.Selected;

    void OnDrawGizmosSelected()
    {
        if (drawGizmoMode == DrawGizmoMode.None)
            return;

        switch (collisionType) {
            case CollisionType.Sphere:
                DrawSphereGizmo();
                break;
            case CollisionType.Capsule:
                DrawCapsuleGizmo();
                break;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawGizmoMode == DrawGizmoMode.Always)
            OnDrawGizmosSelected();
    }

    void DrawSphereGizmo()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    void DrawCapsuleGizmo()
    {
        Gizmos.color = Color.green;

        var position = transform.position;
        var rotation = transform.rotation;

        // sphere origin
        Gizmos.DrawWireSphere(position, radius);

        // sphere terminal
        Gizmos.DrawWireSphere(position + rotation * new Vector3(0, height, 0), radius);
        
        // sideline x
        Vector3 startPosition = position + rotation * new Vector3(radius, 0, 0);
        Vector3 endPosition = position + rotation * new Vector3(radius, height, 0);
        Gizmos.DrawLine(startPosition, endPosition);
        
        // sideline -x
        startPosition = position + rotation * new Vector3(-radius, 0, 0);
        endPosition = position + rotation * new Vector3(-radius, height, 0);
        Gizmos.DrawLine(startPosition, endPosition);

        // sideline z
        startPosition = position + rotation * new Vector3(0, 0, radius);
        endPosition = position + rotation * new Vector3(0, height, radius);
        Gizmos.DrawLine(startPosition, endPosition);

        // sideline -z
        startPosition = position + rotation * new Vector3(0, 0, -radius);
        endPosition = position + rotation * new Vector3(0, height, -radius);
        Gizmos.DrawLine(startPosition, endPosition);
    }
#endif
}

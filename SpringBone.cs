using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CanEditMultipleObjects]
public class SpringBone : MonoBehaviour
{
    // Forces
    [Range(0f, 5000f)]
    public float stiffnessForce = 0.05f;
    [Range(0f, 1f)]
    public float dragForce = 0.05f;

    public Vector3 springForce = new Vector3(0.0f, -0.01f, 0.0f);
    public Vector3 _boneAxis = new Vector3(-1.0f, 0.0f, 0.0f);
    
    // Angle limits
    public Transform pivotNode;
    public float angularStiffness = 100f;
    
    Vector3 _currTipPos;
    Vector3 _prevTipPos;
    Quaternion _initialRotation;

    public SpringBone childNode;
    float _childDistance;

    // Side Link
    public float sideLinkForce = 10.0f;
    public SpringBone sideNodeLeft;
    float _sideDistanceLeft;

    public SpringBone sideNodeRight;
    float _sideDistanceRight;

    // Collision
    public float radius = 0.05f;
    [SerializeField]
    public SpringCollider[] colliders;

    bool _initialized = false;
    SpringController _controller;
    public void Init(SpringController controller)
    {
        if (_initialized)
            return;

        _controller = controller;

        var childPosition = childNode ? childNode.transform.position : transform.position + transform.right * -0.1f;
        var localChildPosition = transform.InverseTransformPoint(childPosition);
        _boneAxis = localChildPosition.normalized;

        _initialRotation = transform.localRotation;

        _childDistance = Vector3.Distance(transform.position, childPosition);
        _currTipPos = childPosition;
        _prevTipPos = childPosition;

        childNode?.Init(controller);
            
        if (sideNodeLeft != null)
            _sideDistanceLeft = Vector3.Distance(childPosition, sideNodeLeft.transform.position);
        if (sideNodeRight != null)
            _sideDistanceRight = Vector3.Distance(childPosition, sideNodeRight.transform.position);

        _initialized = true;
    }

    public void Solve(float elapsedTime, in Vector3 externalForce)
    {
        var baseWorldRotation = transform.parent.rotation * _initialRotation;
        var originPosition = transform.position + baseWorldRotation * _boneAxis * _childDistance;

        const float SpringConstant = 0.5f;
        var sqrDt = elapsedTime * elapsedTime;
        var accelerationMultiplier = SpringConstant * sqrDt;

        // Hooke's law: force to push us to equilibrium
        var force = stiffnessForce * (originPosition - _currTipPos);
        force += springForce + externalForce + _controller.gravity;
        force *= accelerationMultiplier;

        // Verlet
        var temp = _currTipPos;
        force += (1f - dragForce) * (_currTipPos - _prevTipPos);
        _currTipPos += force;
        _prevTipPos = temp;

        var headPosition = transform.position;
        var headToTail = _currTipPos - headPosition;
        var magnitude = headToTail.magnitude;
        const float MagnitudeThreshold = 0.001f;
        headToTail = (magnitude <= MagnitudeThreshold)
            ? transform.TransformDirection(_boneAxis)
            : headToTail / magnitude;
        _currTipPos = headPosition + _childDistance * headToTail;

        // Side Link
        if (sideNodeLeft != null)
            ApplySideLink(sideNodeLeft, _sideDistanceLeft, accelerationMultiplier);
        if (sideNodeRight != null)
            ApplySideLink(sideNodeRight, _sideDistanceRight, accelerationMultiplier);

        // Collision
        Vector3 targetPosition = _currTipPos;
        float scaledRadius = transform.TransformDirection(radius, 0, 0).magnitude;        
        Vector3 hitNormal = new Vector3(0, 0, 1);
        bool hitted = false;
        if (colliders != null) {
            for (int i = 0; i < colliders.Length; i++) {
                if (colliders[i] == null)
                    continue;

                hitted |= colliders[i].Check(_childDistance, headPosition, ref _currTipPos, scaledRadius, ref hitNormal);
            }
        }
        if (hitted) {
            var incidentVector = targetPosition - _prevTipPos;
            var reflectedVector = Vector3.Reflect(incidentVector, hitNormal);

            // friction
            var upwardComponent = Vector3.Dot(reflectedVector, hitNormal) * hitNormal;
            var lateralComponent = reflectedVector - upwardComponent;
            const float bounce = 0.0f;
            const float friction = 1.0f;
            var bounceVelocity = bounce * upwardComponent + (1f - friction) * lateralComponent;
            const float BounceThreshold = 0.0001f;
            if (bounceVelocity.sqrMagnitude > BounceThreshold) {
                var distanceTraveled = (_currTipPos - _prevTipPos).magnitude;
                _prevTipPos = _currTipPos - bounceVelocity;
                _currTipPos += Mathf.Max(0f, bounceVelocity.magnitude - distanceTraveled) * bounceVelocity.normalized;
            }
            else {
                _prevTipPos = _currTipPos;
            }
        }

        childNode?.Solve(elapsedTime, in externalForce);
    }

    void ApplySideLink(SpringBone sideNode, float sideDistance, float acceleration)
    {
        Vector3 linkDirection = _currTipPos - sideNode.transform.position;
        // Hooke's Law
        float curSideDistance = linkDirection.magnitude;
        float distanceDiff = curSideDistance - sideDistance;
        Vector3 movement = acceleration * distanceDiff * linkDirection.normalized;
                
        _currTipPos -= movement * sideLinkForce;
    }

    public void ApplyRotation(float elapsedTime)
    {
        if (float.IsNaN(_currTipPos.x)
            | float.IsNaN(_currTipPos.y)
            | float.IsNaN(_currTipPos.z)) {
            var baseWorldRotation = transform.parent.rotation * _initialRotation;
            _currTipPos = transform.position + baseWorldRotation * _boneAxis * _childDistance;
            _prevTipPos = _currTipPos;
        }

        transform.localRotation = ComputeRotation(_currTipPos);

        childNode?.ApplyRotation(elapsedTime);
    }

    Quaternion ComputeRotation(Vector3 tipPosition)
    {
        var baseWorldRotation = transform.parent.rotation * _initialRotation;
        var worldBoneVector = tipPosition - transform.position;
        var localBoneVector = Quaternion.Inverse(baseWorldRotation) * worldBoneVector;
        localBoneVector.Normalize();

        var aimRotation = Quaternion.FromToRotation(_boneAxis, localBoneVector);
        var outputRotation = _initialRotation * aimRotation;

        return outputRotation;
    }

    public void DeployChildrenRecursive()
    {
        if (transform.childCount == 0)
            return;
        var children = GetComponentsInChildren<Transform>();
        if (children == null)
            return;

        for (int i = 0; i < children.Length; i++) {
            if (children[i] == transform)
                continue;
            if (children[i].parent != transform)
                continue;

            var bone = children[i].GetComponent<SpringBone>();
            if (bone == null)
                bone = children[i].AddComponent<SpringBone>();

            childNode = bone;
            childNode.DeployChildrenRecursive();
        }
    }

    public void CopySettingRecursive()
    {
        if (childNode == null)
            return;

        childNode.stiffnessForce = stiffnessForce;
        childNode.dragForce = dragForce;
        childNode.springForce = springForce;

        childNode.sideLinkForce = sideLinkForce;

        childNode.radius = radius;

        childNode.CopySettingRecursive();
    }

    public void SetLinkRecursive()
    {
        if (childNode == null)
            return;

        if (sideNodeLeft) {
            childNode.sideNodeLeft = sideNodeLeft.childNode;
            if (sideNodeLeft.childNode)
                sideNodeLeft.childNode.sideNodeRight = childNode;
        }
        else {
            childNode.sideNodeLeft = null;
        }
            
        if (sideNodeRight) {
            childNode.sideNodeRight = sideNodeRight.childNode;
            if (sideNodeRight.childNode)
                sideNodeRight.childNode.sideNodeLeft = childNode;
        }   
        else {
            childNode.sideNodeRight = null;
        }

        childNode.SetLinkRecursive();
    }

    public void ClearLinkRecursive()
    {
        sideNodeLeft = null;
        sideNodeRight = null;

        childNode?.ClearLinkRecursive();
    }

    public void SetCollisionRecursive()
    {
        if (childNode == null)
            return;

        if (colliders != null) {
            childNode.colliders = colliders;
            childNode.SetCollisionRecursive();
        }   
    }
}

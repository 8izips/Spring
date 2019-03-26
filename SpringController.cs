using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todo List
// 1. angle limitation
// 2. sample parameter set
public class SpringController : MonoBehaviour
{   
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public Vector3 externalForce;

    [SerializeField]
    SpringBone[] _rootBones;

    void Start()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
    }

    void Init()
    {
        if (_rootBones == null)
            return;

        for (int i = 0; i < _rootBones.Length; i++) {
            if (_rootBones[i])
                _rootBones[i].Init(this);
        }   
    }

    void LateUpdate()
    {
        if (_rootBones == null)
            return;

        float elapsedTime = Time.deltaTime;

        for (int i = 0; i < _rootBones.Length; i++)
            _rootBones[i]?.Solve(elapsedTime, in externalForce);
        for (int i = 0; i < _rootBones.Length; i++)
            _rootBones[i]?.ApplyRotation(elapsedTime);
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

        for (int i = 0; i < _rootBones.Length; i++) {
            if (_rootBones[i])
                DrawGizmoRecursive(_rootBones[i]);
        }   
    }

    private void OnDrawGizmos()
    {
        if (drawGizmoMode == DrawGizmoMode.Always)
            OnDrawGizmosSelected();
    }

    void DrawGizmoRecursive(SpringBone bone)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(bone.transform.position, bone.radius);

        if (bone.childNode != null)
            DrawGizmoRecursive(bone.childNode);
    }
#endif
}

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
    public SpringBone[] GetRootBones()
    {
        return _rootBones;
    }

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
    public void ScanRootBones()
    {
        Transform[] transforms = transform.GetComponentsInChildren<Transform>();
        if (transforms == null)
            return;

        var rootBones = new List<SpringBone>();
        for (int i = 0; i < transforms.Length; i++) {
            var bone = transforms[i].gameObject.GetComponent<SpringBone>();
            if (bone != null) {
                var parentBone = transforms[i].parent.gameObject.GetComponent<SpringBone>();
                if (parentBone == null) {
                    rootBones.Add(bone);
                }
            }
        }

        _rootBones = rootBones.ToArray();
    }

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

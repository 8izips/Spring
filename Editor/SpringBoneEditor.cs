using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpringBone)), CanEditMultipleObjects]
public class SpringBoneEditor : Editor
{
    SpringBone _instance;
    void OnEnable()
    {
        _instance = (SpringBone)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Base Parameter
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Base Parameter", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _instance.stiffnessForce = EditorGUILayout.FloatField("Stiffness", _instance.stiffnessForce);
        _instance.dragForce = EditorGUILayout.FloatField("Drag Force", _instance.dragForce);
        _instance.springForce = EditorGUILayout.Vector3Field("Spring Force", _instance.springForce);
        _instance.childNode = (SpringBone)EditorGUILayout.ObjectField("Child Node", _instance.childNode, typeof(SpringBone), true);
        if (EditorGUI.EndChangeCheck()) {
            foreach (var subObject in targets) {
                var subBone = subObject as SpringBone;

                subBone.stiffnessForce = _instance.stiffnessForce;
                subBone.dragForce = _instance.dragForce;
                subBone.springForce = _instance.springForce;

                EditorUtility.SetDirty(subObject);
            }
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Children"))
            _instance.DeployChildrenRecursive();
        if (GUILayout.Button("Apply Children")) {
            if (Selection.objects.Length > 1) {
                for (int i = 0; i < Selection.objects.Length - 1; i++) {
                    var boneObject = Selection.objects[i] as GameObject;
                    var bone = boneObject.GetComponent<SpringBone>();
                    bone?.CopySettingRecursive();
                }
            }
            else {
                _instance.CopySettingRecursive();
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Side Link
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Links", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _instance.sideLinkForce = EditorGUILayout.FloatField("Side Link", _instance.sideLinkForce);
        if (EditorGUI.EndChangeCheck()) {
            foreach (var subObject in targets) {
                var subBone = subObject as SpringBone;

                subBone.sideLinkForce = _instance.sideLinkForce;

                EditorUtility.SetDirty(subObject);
            }
        }
        _instance.sideNodeLeft = (SpringBone)EditorGUILayout.ObjectField("Left Link Target", _instance.sideNodeLeft, typeof(SpringBone), true);
        _instance.sideNodeRight = (SpringBone)EditorGUILayout.ObjectField("Right Link Target", _instance.sideNodeRight, typeof(SpringBone), true);
        EditorGUILayout.Vector3Field("movement", _instance.DebugMovement);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Children")) {
            if (Selection.objects.Length > 1) {
                for (int i = 0; i < Selection.objects.Length - 1; i++) {
                    var leftObject = Selection.objects[i] as GameObject;
                    SpringBone leftBone = leftObject.GetComponent<SpringBone>();
                    var rightObject = Selection.objects[i + 1] as GameObject;
                    SpringBone rightBone = rightObject.GetComponent<SpringBone>();
                    if (leftBone == null || rightBone == null)
                        continue;

                    leftBone.sideNodeRight = rightBone;
                    rightBone.sideNodeLeft = leftBone;
                    leftBone.SetLinkRecursive();
                }
            }
            else {
                _instance.SetLinkRecursive();
            }
        }
        if (GUILayout.Button("Clear")) {
            if (Selection.objects.Length > 1) {
                for (int i = 0; i < Selection.objects.Length; i++) {
                    var leftObject = Selection.objects[i] as GameObject;
                    SpringBone bone = leftObject.GetComponent<SpringBone>();
                    bone?.ClearLinkRecursive();
                }
            }
            else {
                _instance.ClearLinkRecursive();
            }         
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Collision
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel);        
        EditorGUI.BeginChangeCheck();
        EditorGUI.indentLevel++;
        _instance.radius = EditorGUILayout.FloatField("Collision Radius", _instance.radius);
        var colliders = serializedObject.FindProperty("colliders");
        if (colliders != null)
            EditorGUILayout.PropertyField(colliders, true);
        EditorGUI.indentLevel--;
        if (EditorGUI.EndChangeCheck()) {
            foreach (var subObject in targets) {
                var subBone = subObject as SpringBone;

                subBone.radius = _instance.radius;
                subBone.colliders = _instance.colliders;

                EditorUtility.SetDirty(subObject);
            }
        }
        if (GUILayout.Button("Apply Children")) {
            if (Selection.objects.Length > 1) {
                for (int i = 0; i < Selection.objects.Length - 1; i++) {
                    var boneObject = Selection.objects[i] as GameObject;
                    var bone = boneObject.GetComponent<SpringBone>();
                    bone?.SetCollisionRecursive();
                }
            }
            else {
                _instance.SetCollisionRecursive();
            }
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}

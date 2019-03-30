using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpringController))]
public class SpringControllerEditor : Editor
{
    SpringController _instance;
    SerializedProperty _bonesProperty;
    void OnEnable()
    {
        _instance = (SpringController)target;
        _bonesProperty = serializedObject.FindProperty("_rootBones");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Force", EditorStyles.boldLabel);
        _instance.gravity = EditorGUILayout.Vector3Field("Gravity", _instance.gravity);
        _instance.externalForce = EditorGUILayout.Vector3Field("External Force", _instance.externalForce);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        //EditorGUILayout.LabelField("Root Bones", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_bonesProperty, true);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan")) {
            _instance.ScanRootBones();
        }
        if (GUILayout.Button("Select")) {
            Selection.objects = (Object[])_instance.GetRootBones();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        _instance.drawGizmoMode = (SpringController.DrawGizmoMode)EditorGUILayout.EnumPopup("Draw Gizmo Mode", _instance.drawGizmoMode);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}

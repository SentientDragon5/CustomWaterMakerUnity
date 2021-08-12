using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum WaterSimpleMode { View, Move, Rotate, Extend };

[CustomEditor(typeof(WaterMakerSimple)), CanEditMultipleObjects]
public class WaterMakerSimpleEditor : Editor
{
    SerializedProperty material;

    void OnEnable()
    {
        material = serializedObject.FindProperty("material");
    }
    public override void OnInspectorGUI()
    {
        WaterMakerSimple maker = (WaterMakerSimple)target;


        GUILayout.Label("Mode ", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("View", GUILayout.MinWidth(320), GUILayout.Width(80)))
        {
            maker.mode = WaterSimpleMode.View;
        }
        if (GUILayout.Button("Move", GUILayout.MinWidth(320), GUILayout.Width(80)))
        {
            maker.mode = WaterSimpleMode.Move;
        }
        if (GUILayout.Button("Rotate", GUILayout.MinWidth(320), GUILayout.Width(80)))
        {
            maker.mode = WaterSimpleMode.Rotate;
        }
        if (GUILayout.Button("Extend", GUILayout.MinWidth(320), GUILayout.Width(80)))
        {
            maker.mode = WaterSimpleMode.Extend;
        }
        GUILayout.EndHorizontal();

        serializedObject.Update();
        //EditorGUILayout.PropertyField(material);
        if (GUILayout.Button("Bake", GUILayout.MinWidth(320), GUILayout.Width(320)))
        {
            maker.Bake();
        }

        serializedObject.ApplyModifiedProperties();

        DrawDefaultInspector();
    }
    protected virtual void OnSceneGUI()
    {
        WaterMakerSimple maker = (WaterMakerSimple)target;

        for (int i = 0; i < maker.vertices.Count; i++)
        {
            if (maker.mode == WaterSimpleMode.Move)
            {
                Vector3 edit = maker.vertices[i].positionWorld;
                edit = Handles.PositionHandle(edit, Quaternion.identity);
                maker.vertices[i].position = edit - maker.transform.position;
            }
            if (maker.mode == WaterSimpleMode.Rotate)
            {
                Quaternion edit = maker.vertices[i].rotation;
                edit = Handles.RotationHandle(edit, maker.vertices[i].positionWorld);
                maker.vertices[i].rotation = edit;
            }
            if (maker.mode == WaterSimpleMode.Extend)
            {
                WaterVertex vert = maker.vertices[i];
                Transform parent = maker.transform;
                if (SceneView.currentDrawingSceneView != null)
                {
                    Vector3 pos = vert.position + parent.position + vert.rotation * Vector3.forward * 5f;

                    float dist = Vector3.Distance(pos, SceneView.currentDrawingSceneView.pivot);
                    float size = 0.01f * dist;


                    Handles.color = Color.green;
                    Handles.DrawSolidDisc(pos, SceneView.currentDrawingSceneView.rotation * Vector3.forward, size);
                    if(Handles.Button(pos, SceneView.currentDrawingSceneView.rotation.normalized, 1.25f * size, 1.25f * size, Handles.CircleHandleCap))
                    {
                        WaterVertex newVert = new WaterVertex(pos - parent.position, vert.rotation, parent);
                        maker.vertices.Add(newVert);
                        vert.to.Add(maker.vertices.IndexOf(newVert));
                    }
                }
            }
            Handles.color = Color.blue;
            Handles.Label(maker.vertices[i].positionWorld, new GUIContent(i.ToString()));
        }
    }
}
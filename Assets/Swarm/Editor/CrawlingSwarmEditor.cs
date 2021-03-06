// Swarm - Special renderer that draws a swarm of swirling/crawling lines.
// https://github.com/keijiro/Swarm

using UnityEngine;
using UnityEditor;

namespace Swarm
{
    // Custom inspector for CrawlingSwarm
    [CustomEditor(typeof(CrawlingSwarm)), CanEditMultipleObjects]
    public class CrawlingSwarmEditor : Editor
    {
        SerializedProperty _instanceCount;
        SerializedProperty _template;
        SerializedProperty _radius;

        SerializedProperty _volume;
        SerializedProperty _speed;
        SerializedProperty _noiseFrequency;
        SerializedProperty _noiseSpread;
        SerializedProperty _noiseMotion;

        SerializedProperty _material;
        SerializedProperty _gradient;

        static class Labels
        {
            public static GUIContent frequency = new GUIContent("Frequency");
            public static GUIContent spread = new GUIContent("Spread");
            public static GUIContent motion = new GUIContent("Motion");
        }

        void OnEnable()
        {
            _instanceCount = serializedObject.FindProperty("_instanceCount");
            _template = serializedObject.FindProperty("_template");
            _radius = serializedObject.FindProperty("_radius");

            _volume = serializedObject.FindProperty("_volume");
            _speed = serializedObject.FindProperty("_speed");
            _noiseFrequency = serializedObject.FindProperty("_noiseFrequency");
            _noiseSpread = serializedObject.FindProperty("_noiseSpread");
            _noiseMotion = serializedObject.FindProperty("_noiseMotion");

            _material = serializedObject.FindProperty("_material");
            _gradient = serializedObject.FindProperty("_gradient");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_instanceCount);
            EditorGUILayout.PropertyField(_template);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_radius);
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(_speed);
            EditorGUILayout.PropertyField(_volume);
            EditorGUILayout.LabelField("Noise Field");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_noiseFrequency, Labels.frequency);
            EditorGUILayout.PropertyField(_noiseSpread, Labels.spread);
            EditorGUILayout.PropertyField(_noiseMotion, Labels.motion);
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(_material);
            EditorGUILayout.PropertyField(_gradient);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

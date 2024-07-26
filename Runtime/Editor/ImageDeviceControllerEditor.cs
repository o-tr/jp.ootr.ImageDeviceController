using jp.ootr.common;
using UnityEditor;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(ImageDeviceController))]
    public class ImageDeviceControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty devices;

        private void OnEnable()
        {
            devices = serializedObject.FindProperty("devices");
        }

        public override void OnInspectorGUI()
        {
            var script = (ImageDeviceController)target;

            EditorGUILayout.LabelField("ImageDeviceController", EditorStyle.UiTitle);

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(devices, new GUIContent("Device List"), true);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Zip Load Delay Frames");
            script.zlDelayFrames = EditorGUILayout.IntSlider(script.zlDelayFrames, 0, 100);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Base64 Decode Part Size");
            script.zlPartLength = EditorGUILayout.IntSlider(script.zlPartLength, 1024, 1024000);

            EditorUtility.SetDirty(script);
        }
    }
}

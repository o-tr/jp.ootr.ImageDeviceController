using jp.ootr.common;
using UnityEditor;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(ImageDeviceController))]
    public class ImageDeviceControllerEditor : UnityEditor.Editor
    {
        private bool _debug;
        private SerializedProperty _devices;
        private SerializedProperty _zlDelayFrames;
        private SerializedProperty _zlPartLength;

        private void OnEnable()
        {
            _devices = serializedObject.FindProperty("devices");
            _zlDelayFrames = serializedObject.FindProperty("zlDelayFrames");
            _zlPartLength = serializedObject.FindProperty("zlPartLength");
        }

        public override void OnInspectorGUI()
        {
            _debug = EditorGUILayout.ToggleLeft("Debug", _debug);
            if (_debug)
            {
                base.OnInspectorGUI();
                return;
            }

            var script = (ImageDeviceController)target;

            EditorGUILayout.LabelField("ImageDeviceController", EditorStyle.UiTitle);

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_devices, new GUIContent("Device List"), true);
            for (var i = _devices.arraySize - 1; i >= 0; i--)
                if (_devices.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    _devices.DeleteArrayElementAtIndex(i);
            if (EditorGUI.EndChangeCheck()) UpdateDevices(script);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_zlDelayFrames, new GUIContent("Zip Load Delay Frames"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_zlPartLength, new GUIContent("Base64 Decode Part Size"));

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateDevices(ImageDeviceController script)
        {
            foreach (var device in script.devices)
            {
                var so = new SerializedObject(device);
                so.FindProperty("controller").objectReferenceValue = script;
                var property = so.FindProperty("devices");
                property.arraySize = script.devices.Length;
                for (var i = 0; i < script.devices.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = script.devices[i];

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(device);
            }

            EditorUtility.SetDirty(script);
        }
    }
}
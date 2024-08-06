using System.Linq;
using jp.ootr.common;
using UnityEditor;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(ImageDeviceController))]
    public class ImageDeviceControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty devices;
        protected bool Debug;

        private void OnEnable()
        {
            devices = serializedObject.FindProperty("devices");
        }

        public override void OnInspectorGUI()
        {
            Debug = EditorGUILayout.ToggleLeft("Debug", Debug);
            if (Debug)
            {
                base.OnInspectorGUI();
                return;
            }
            EditorGUI.BeginChangeCheck();
            var script = (ImageDeviceController)target;

            EditorGUILayout.LabelField("ImageDeviceController", EditorStyle.UiTitle);

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(devices, new GUIContent("Device List"), true);
            script.devices = script.devices.Where((v)=>v != null).ToArray();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Zip Load Delay Frames");
            script.zlDelayFrames = EditorGUILayout.IntSlider(script.zlDelayFrames, 0, 100);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Base64 Decode Part Size");
            script.zlPartLength = EditorGUILayout.IntSlider(script.zlPartLength, 1024, 1024000);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var device in script.devices)
                {
                    var so = new SerializedObject(device);
                    so.FindProperty("controller").objectReferenceValue = script;
                    var property = so.FindProperty("devices");
                    property.arraySize = script.devices.Length;
                    for (var i = 0; i < script.devices.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = script.devices[i];
                    }
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(device);
                }
                EditorUtility.SetDirty(script);
            }
        }
    }
}

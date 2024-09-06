#if UNITY_EDITOR
using jp.ootr.common;
using jp.ootr.ImageDeviceController.CommonDevice.PickupDetector;
using UnityEditor;
using UnityEngine;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(PickupDetector))]
    public class PickupDetectorEditor : UnityEditor.Editor
    {
        private SerializedProperty _commonDevice;

        private void OnEnable()
        {
            _commonDevice = serializedObject.FindProperty("commonDevice");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("PickupDetector", EditorStyle.UiTitle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target Device");
            serializedObject.Update();
            EditorGUILayout.PropertyField(_commonDevice, new GUIContent("Common Device"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif

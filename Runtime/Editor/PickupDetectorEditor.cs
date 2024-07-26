using jp.ootr.common;
using jp.ootr.ImageDeviceController.CommonDevice.PickupDetector;
using UnityEditor;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(PickupDetector))]
    public class PickupDetectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var script = (PickupDetector)target;

            EditorGUILayout.LabelField("PickupDetector", EditorStyle.UiTitle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target Device");
            script.commonDevice =
                (CommonDevice.CommonDevice)EditorGUILayout.ObjectField(script.commonDevice, typeof(CommonDevice.CommonDevice), true);
        }
    }
}

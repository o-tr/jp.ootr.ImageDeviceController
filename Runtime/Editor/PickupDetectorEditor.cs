#if UNITY_EDITOR
using jp.ootr.common.Editor;
using jp.ootr.ImageDeviceController.CommonDevice.PickupDetector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(PickupDetector))]
    public class PickupDetectorEditor : BaseEditor
    {
        protected override VisualElement GetLayout()
        {
            var root = new VisualElement();
            root.AddToClassList("container");
            
            root.Add(GetCommonDevice());
            
            return root;
        }
        
        private VisualElement GetCommonDevice()
        {
            var commonDevice = new ObjectField
            {
                objectType = typeof(CommonDevice.CommonDevice),
                bindingPath = "commonDevice",
                label = "Target Device"
            };
            
            return commonDevice;
        }

        protected override string GetScriptName()
        {
            return "Pickup Detector";
        }
    }
}
#endif

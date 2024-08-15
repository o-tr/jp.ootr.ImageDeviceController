using UdonSharp;

namespace jp.ootr.ImageDeviceController
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ImageDeviceController : DeviceController
    {
        public override string GetClassName()
        {
            return "jp.ootr.ImageDeviceController.ImageDeviceController";
        }
        
        public override string GetDisplayName()
        {
            return "Image Device Controller";
        }
    }
}
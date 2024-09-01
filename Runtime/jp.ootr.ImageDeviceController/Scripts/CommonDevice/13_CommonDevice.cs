namespace jp.ootr.ImageDeviceController.CommonDevice
{
    public class CommonDevice : LogicLoadImage
    {
        public override string GetClassName()
        {
            return "jp.ootr.ImageManager.CommonDevice.CommonDevice";
        }

        public override string GetDisplayName()
        {
            return "Common Device";
        }

        public override void OnPickup()
        {
        }

        public override void OnDrop()
        {
        }
    }
}
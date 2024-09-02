namespace jp.ootr.ImageDeviceController
{
    public static class DeviceUtils
    {
        public static CommonDevice.CommonDevice FindByUuid(this CommonDevice.CommonDevice[] devices, string uuid)
        {
            foreach (var device in devices)
                if (device.GetDeviceUuid() == uuid)
                    return device;

            return null;
        }
    }
}

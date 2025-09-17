using JetBrains.Annotations;

namespace jp.ootr.ImageDeviceController
{
    public static class DeviceUtils
    {
        [CanBeNull]
        public static CommonDevice.CommonDevice FindByUuid(
            [CanBeNull][ItemCanBeNull] this CommonDevice.CommonDevice[] devices, string uuid)
        {
            if (devices == null) return null;
            foreach (var device in devices)
            {
                if (device == null) continue;
                if (device.GetDeviceUuid() == uuid)
                    return device;
            }

            return null;
        }
    }
}

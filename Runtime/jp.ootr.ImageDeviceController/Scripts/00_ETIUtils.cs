using JetBrains.Annotations;
using VRC.SDK3.StringLoading;

namespace jp.ootr.ImageDeviceController
{
    public static class EIAUtils
    {
        public static bool IsValidEIA([CanBeNull] this IVRCStringDownload result)
        {
            if (result == null) return false;
            return result.ResultBytes[0] == 0x45 &&
                   result.ResultBytes[1] == 0x49 &&
                   result.ResultBytes[2] == 0x41 &&
                   result.ResultBytes[3] == 0x5E &&
                   result.ResultBytes[4] == 0x7B;
        }
    }
}

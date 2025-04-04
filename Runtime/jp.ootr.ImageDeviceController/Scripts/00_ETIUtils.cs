using JetBrains.Annotations;
using VRC.SDK3.StringLoading;

namespace jp.ootr.ImageDeviceController
{
    public static class ETIUtils
    {
        public static bool IsValidETI([CanBeNull] this IVRCStringDownload result)
        {
            if (result == null) return false;
            return result.Result.Substring(0, 5) == "ETI@{";
        }
    }
}

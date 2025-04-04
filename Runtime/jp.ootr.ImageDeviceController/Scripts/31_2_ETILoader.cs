using jp.ootr.UdonBase64RLE;
using UnityEngine;
using VRC.SDK3.StringLoading;

namespace jp.ootr.ImageDeviceController
{
    public class ETILoader : ZipLoader {
        [SerializeField] private UdonBase64CSVRLE base64Rle;
        protected void OnETILoadSuccess(IVRCStringDownload result)
        {
            
        }
    }
}

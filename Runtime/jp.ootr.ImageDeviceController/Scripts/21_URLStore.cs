using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using static jp.ootr.common.ArrayUtils;
using static jp.ootr.common.Network;

namespace jp.ootr.ImageDeviceController
{
    public class URLStore : CacheController
    {
        [SerializeField] public VRCUrl[] usUrls = new VRCUrl[0];
        [SerializeField] public string[] usUrlStrings = new string[0];
        [UdonSynced] private URLStoreSyncAction _usSyncAction = URLStoreSyncAction.None;
        [UdonSynced] private VRCUrl[] _usSyncUrl = new VRCUrl[0];
        
        private readonly string[] _urlStorePrefixes = new[] { "URLStore" };

        public VRCUrl UsGetUrl(string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs)) url = tmpUrl;

            if (!usUrlStrings.Has(url, out var urlIndex)) return null;
            return usUrls[urlIndex];
        }

        public void UsAddUrl(VRCUrl url)
        {
            if (usUrlStrings.Has(url.ToString())) return;
            _usSyncAction = URLStoreSyncAction.AddUrl;
            _usSyncUrl = new[] { url };
            Sync();
        }

        public void UsAddUrlLocal(VRCUrl url)
        {
            if (usUrlStrings.Has(url.ToString())) return;
            usUrls = usUrls.Append(url);
            usUrlStrings = usUrlStrings.Append(url.ToString());
        }

        public bool UsHasUrl(string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs)) url = tmpUrl;

            return usUrlStrings.Has(url, out var tmp);
        }

        public override void _OnDeserialization()
        {
            switch (_usSyncAction)
            {
                case URLStoreSyncAction.AddUrl:
                    if (usUrlStrings.Has(_usSyncUrl[0].ToString())) return;
                    ConsoleDebug($"url added to store: {_usSyncUrl[0]}", _urlStorePrefixes);
                    usUrls = usUrls.Append(_usSyncUrl[0]);
                    usUrlStrings = usUrlStrings.Append(_usSyncUrl[0].ToString());
                    break;
                case URLStoreSyncAction.SyncAll:
                    ConsoleDebug($"urls synced: {_usSyncUrl.Length}", _urlStorePrefixes);
                    usUrls = _usSyncUrl;
                    usUrlStrings = _usSyncUrl.ToStrings();
                    break;
                case URLStoreSyncAction.None:
                default:
                    break;
            }

            _usSyncAction = URLStoreSyncAction.None;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            _usSyncAction = URLStoreSyncAction.SyncAll;
            _usSyncUrl = usUrls;
            Sync();
        }
    }
}
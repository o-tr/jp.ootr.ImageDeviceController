using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using static jp.ootr.common.ArrayUtils;
using static jp.ootr.common.Network;

namespace jp.ootr.ImageDeviceController
{
    public class URLStore : CacheController
    {
        [ItemCanBeNull][SerializeField] protected VRCUrl[] usUrls = new VRCUrl[0];
        [ItemCanBeNull][SerializeField] protected string[] usUrlStrings = new string[0];

        private readonly string[] _urlStorePrefixes = { "URLStore" };
        [UdonSynced] private URLStoreSyncAction _usSyncAction = URLStoreSyncAction.None;
        [ItemCanBeNull][UdonSynced] private VRCUrl[] _usSyncUrl = new VRCUrl[0];

        [CanBeNull]
        public VRCUrl UsGetUrl([CanBeNull]string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs)) url = tmpUrl;

            if (!usUrlStrings.Has(url, out var urlIndex)) return null;
            return usUrls[urlIndex];
        }

        public void UsAddUrl([CanBeNull]VRCUrl url)
        {
            if (url == null || usUrlStrings.Has(url.ToString())) return;
            _usSyncAction = URLStoreSyncAction.AddUrl;
            _usSyncUrl = new[] { url };
            Sync();
        }

        public void UsAddUrlLocal([CanBeNull]VRCUrl url)
        {
            if (url == null || usUrlStrings.Has(url.ToString())) return;
            usUrls = usUrls.Append(url);
            usUrlStrings = usUrlStrings.Append(url.ToString());
        }

        public bool UsHasUrl([CanBeNull]string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs)) url = tmpUrl;

            return usUrlStrings.Has(url, out var tmp);
        }

        public override void _OnDeserialization()
        {
            if (_usSyncUrl.Length < 1 || _usSyncUrl[0] == null)
            {
                _usSyncAction = URLStoreSyncAction.None;
                return;
            }

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

        public override void OnPlayerJoined([NotNull]VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            _usSyncAction = URLStoreSyncAction.SyncAll;
            _usSyncUrl = usUrls;
            Sync();
        }
    }
}

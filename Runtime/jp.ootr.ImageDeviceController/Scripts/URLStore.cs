using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using static jp.ootr.common.ArrayUtils;
using static jp.ootr.common.Network;

namespace jp.ootr.ImageDeviceController
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class URLStore : CacheController
    {
        [UdonSynced] protected URLStoreSyncAction UsSyncAction = URLStoreSyncAction.None;
        [UdonSynced] protected VRCUrl[] UsSyncUrl = new VRCUrl[0];
        
        [SerializeField] public VRCUrl[] usUrls = new VRCUrl[0];
        [SerializeField] public string[] usUrlStrings = new string[0];

        public VRCUrl UsGetUrl(string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs))
            {
                url = tmpUrl;
            }

            if (!usUrlStrings.Has(url, out var urlIndex)) return null;
            return usUrls[urlIndex];
        }

        public void UsAddUrl(VRCUrl url)
        {
            if (usUrlStrings.Has(url.ToString())) return;
            UsSyncAction = URLStoreSyncAction.AddUrl;
            UsSyncUrl = new[] { url };
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
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs))
            {
                url = tmpUrl;
            }

            return usUrlStrings.Has(url, out var tmp);
        }

        public override void _OnDeserialization()
        {
            switch (UsSyncAction)
            {
                case URLStoreSyncAction.AddUrl:
                    if (usUrlStrings.Has(UsSyncUrl[0].ToString())) return;
                    ConsoleDebug($"URLStore: url added to store: {UsSyncUrl[0]}");
                    usUrls = usUrls.Append(UsSyncUrl[0]);
                    usUrlStrings = usUrlStrings.Append(UsSyncUrl[0].ToString());
                    break;
                case URLStoreSyncAction.SyncAll:
                    ConsoleDebug($"URLStore: urls synced: {UsSyncUrl.Length}");
                    usUrls = UsSyncUrl;
                    usUrlStrings = UsSyncUrl.ToStrings();
                    break;
            }

            UsSyncAction = URLStoreSyncAction.None;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            UsSyncAction = URLStoreSyncAction.SyncAll;
            UsSyncUrl = usUrls;
            Sync();
        }
    }
}
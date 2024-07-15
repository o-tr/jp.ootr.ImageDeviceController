using UdonSharp;
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
        protected VRCUrl[] UsUrls = new VRCUrl[0];
        protected string[] UsUrlStrings = new string[0];

        public VRCUrl UsGetUrl(string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs))
            {
                url = tmpUrl;
            }

            if (!UsUrlStrings.Has(url, out var urlIndex)) return null;
            return UsUrls[urlIndex];
        }

        public void UsAddUrl(VRCUrl url)
        {
            if (UsUrlStrings.Has(url.ToString())) return;
            UsSyncAction = URLStoreSyncAction.AddUrl;
            UsSyncUrl = new[] { url };
            Sync();
        }

        public void UsAddUrlLocal(VRCUrl url)
        {
            if (UsUrlStrings.Has(url.ToString())) return;
            UsUrls = UsUrls.Append(url);
            UsUrlStrings = UsUrlStrings.Append(url.ToString());
        }

        public bool UsHasUrl(string url)
        {
            if (UrlUtil.GetUrlAndArgs(url, out var tmpUrl, out var voidArgs))
            {
                url = tmpUrl;
            }

            return UsUrlStrings.Has(url, out var tmp);
        }

        public override void _OnDeserialization()
        {
            switch (UsSyncAction)
            {
                case URLStoreSyncAction.AddUrl:
                    if (UsUrlStrings.Has(UsSyncUrl[0].ToString())) return;
                    ConsoleDebug($"URLStore: url added to store: {UsSyncUrl[0]}");
                    UsUrls = UsUrls.Append(UsSyncUrl[0]);
                    UsUrlStrings = UsUrlStrings.Append(UsSyncUrl[0].ToString());
                    break;
                case URLStoreSyncAction.SyncAll:
                    ConsoleDebug($"URLStore: urls synced: {UsSyncUrl.Length}");
                    UsUrls = UsSyncUrl;
                    UsUrlStrings = UsSyncUrl.ToStrings();
                    break;
            }

            UsSyncAction = URLStoreSyncAction.None;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) return;
            UsSyncAction = URLStoreSyncAction.SyncAll;
            UsSyncUrl = UsUrls;
            Sync();
        }
    }
}
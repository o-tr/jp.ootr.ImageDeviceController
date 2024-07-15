namespace jp.ootr.ImageDeviceController
{
    public interface IControlledDevice
    {
        string GetName();
        void InitController(DeviceController controller, int index, IControlledDevice[] devices);
        void ShowScreenName();
        void OnFilesLoadSuccess(string url, string[] files);
        void OnFilesLoadFailed(LoadError error);
    }
}
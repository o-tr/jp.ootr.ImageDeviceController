#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using jp.ootr.common;
using jp.ootr.common.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDKBase.Editor.BuildPipeline;
using Console = jp.ootr.common.Console;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(CommonDevice.CommonDevice))]
    public class CommonDeviceEditor : BaseEditor
    {
        [SerializeField] private StyleSheet commonStyle;

        public override void OnEnable()
        {
            base.OnEnable();
            Root.styleSheets.Add(commonStyle);
        }

        protected override VisualElement GetLayout()
        {
            var container = new VisualElement();
            container.AddToClassList("container");
            var script = (CommonDevice.CommonDevice)target;
            SetController(script);

            container.Add(GetDeviceUuid(script));
            container.Add(ShowDeviceName());
            container.Add(GetContentTk());

            var imguiContainer = new IMGUIContainer(OnInspectorGUIInternal);
            Root.Add(imguiContainer);

            container.Add(GetOther(script));
            return container;
        }

        protected virtual VisualElement GetContentTk()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnInspectorGUIInternal()
        {
            EditorGUILayout.Space();
            ShowContent();
            EditorGUILayout.Space();
        }

        protected virtual void ShowContent()
        {
        }

        private void SetController(CommonDevice.CommonDevice script)
        {
            serializedObject.Update();
            if (CommonDeviceUtils.UpdateDeviceControl(script, FindObjectsOfType<ImageDeviceController>()))
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var helpBox =
                new HelpBox(
                    "Please assign this device to ImageDeviceController\nこのデバイスをImageDeviceControllerの管理対象に追加してください",
                    HelpBoxMessageType.Error);
            InfoBlock.Add(helpBox);
            serializedObject.ApplyModifiedProperties();
        }

        protected override string GetScriptName()
        {
            return "CommonDevice";
        }

        private VisualElement GetDeviceUuid(CommonDevice.CommonDevice script)
        {
            if (script.deviceUuid.IsNullOrEmpty()) CommonDeviceUtils.GenerateUuid(serializedObject);
            var row = new VisualElement();
            row.AddToClassList("row");
            var label = new Label("Device UUID");
            row.Add(label);
            var uuid = new Label(script.deviceUuid);
            row.Add(uuid);
            return row;
        }

        private VisualElement ShowDeviceName()
        {
            return new TextField("Device Name")
            {
                bindingPath = nameof(CommonDevice.CommonDevice.deviceName)
            };
        }

        private VisualElement GetOther(CommonDevice.CommonDevice script)
        {
            var foldout = new Foldout
            {
                text = "Other",
                value = false
            };

            if (script.splashImage != null)
            {
                var texture = new ObjectField("Splash Image")
                {
                    bindingPath = nameof(CommonDevice.CommonDevice.splashImageTexture),
                    objectType = typeof(Texture2D)
                };
                foldout.Add(texture);

                texture.RegisterValueChangedCallback(evt =>
                {
                    var newTexture = (Texture2D)evt.newValue;
                    var splashImageProp = serializedObject.FindProperty(nameof(CommonDevice.CommonDevice.splashImage));
                    var splashImage = (RawImage)splashImageProp.objectReferenceValue;
                    var soImage = new SerializedObject(splashImage);
                    soImage.Update();
                    soImage.FindProperty("m_Texture").objectReferenceValue = newTexture;
                    soImage.ApplyModifiedProperties();
                    var slashImageFitterProp =
                        serializedObject.FindProperty(nameof(CommonDevice.CommonDevice.splashImageFitter));
                    var splashImageFitter = (AspectRatioFitter)slashImageFitterProp.objectReferenceValue;
                    var soImageFitter = new SerializedObject(splashImageFitter);
                    soImageFitter.Update();
                    soImageFitter.FindProperty("m_AspectRatio").floatValue =
                        newTexture.width / (float)newTexture.height;
                    soImageFitter.ApplyModifiedProperties();
                });
            }

            return foldout;
        }
    }


    [InitializeOnLoad]
    public class PlayModeNotifier
    {
        static PlayModeNotifier()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            var scripts = ComponentUtils.GetAllComponents<CommonDevice.CommonDevice>();
            CommonDeviceUtils.ValidateUuids(scripts.ToArray());
            CommonDeviceUtils.UpdateSplashImages(scripts.ToArray());
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var scripts = ComponentUtils.GetAllComponents<CommonDevice.CommonDevice>();
                CommonDeviceUtils.ValidateUuids(scripts.ToArray());
                CommonDeviceUtils.UpdateSplashImages(scripts.ToArray());
            }
        }
    }

    public class SetObjectReferences : UnityEditor.Editor, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 10;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            return CommonDeviceUtils.SetupDevices();
        }
    }

    public static class CommonDeviceUtils
    {
        public static bool SetupDevices()
        {
            var scripts = ComponentUtils.GetAllComponents<CommonDevice.CommonDevice>();
            var controllers = ComponentUtils.GetAllComponents<ImageDeviceController>().ToArray();
            if (!SetEachReference(scripts, controllers) || !ValidateDuplicateReferencedDevices(controllers))
                return false;

            ValidateUuids(scripts.ToArray());
            UpdateSplashImages(scripts.ToArray());
            return true;
        }

        public static void UpdateSplashImages(CommonDevice.CommonDevice[] scripts)
        {
            foreach (var script in scripts)
            {
                if (script.splashImage == null) continue;
                var texture = script.splashImageTexture;
                var splashImageProp =
                    new SerializedObject(script).FindProperty(nameof(CommonDevice.CommonDevice.splashImage));
                var splashImage = (RawImage)splashImageProp.objectReferenceValue;
                var soImage = new SerializedObject(splashImage);
                soImage.Update();
                soImage.FindProperty("m_Texture").objectReferenceValue = texture;
                soImage.ApplyModifiedProperties();
                var slashImageFitterProp =
                    new SerializedObject(script).FindProperty(nameof(CommonDevice.CommonDevice.splashImageFitter));
                var splashImageFitter = (AspectRatioFitter)slashImageFitterProp.objectReferenceValue;
                var soImageFitter = new SerializedObject(splashImageFitter);
                soImageFitter.Update();
                soImageFitter.FindProperty("m_AspectRatio").floatValue = texture.width / (float)texture.height;
                soImageFitter.ApplyModifiedProperties();
            }
        }

        public static void ValidateUuids(CommonDevice.CommonDevice[] scripts)
        {
            var usedUuids = new List<string>();
            foreach (var script in scripts)
            {
                var uuid = script.deviceUuid;
                if (uuid.IsNullOrEmpty()) uuid = Guid.NewGuid().ToString();

                while (usedUuids.Contains(uuid))
                {
                    Console.Warn(
                        $"Device {script.name}({script.GetInstanceID()})'s UUID is regenerated because it is duplicated",
                        "jp.ootr.ImageDeviceController");
                    uuid = Guid.NewGuid().ToString();
                }

                usedUuids.Add(uuid);
                if (script.deviceUuid == uuid) continue;
                var so = new SerializedObject(script);
                so.Update();
                so.FindProperty(nameof(CommonDevice.CommonDevice.deviceUuid)).stringValue = uuid;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(script);
            }
        }

        public static void GenerateUuid(SerializedObject so)
        {
            var uuid = Guid.NewGuid().ToString();
            so.FindProperty(nameof(CommonDevice.CommonDevice.deviceUuid)).stringValue = uuid;
        }

        public static bool ValidateDuplicateReferencedDevices(ImageDeviceController[] controllers)
        {
            var usedDevices = new List<CommonDevice.CommonDevice>();
            var flag = true;
            foreach (var controller in controllers)
            foreach (var device in controller.devices)
            {
                if (usedDevices.Contains(device))
                {
                    Console.Error(
                        $"Device {device.name}({device.GetInstanceID()}) is referenced by multiple ImageDeviceControllers",
                        "jp.ootr.ImageDeviceController");
                    flag = false;
                }

                usedDevices.Add(device);
            }

            return flag;
        }

        public static bool SetEachReference(List<CommonDevice.CommonDevice> scripts,
            ImageDeviceController[] controllers)
        {
            foreach (var script in scripts)
                if (!UpdateDeviceControl(script, controllers))
                {
                    Console.Error(
                        $"Device {script.name}({script.GetInstanceID()}) is not assigned to any ImageDeviceController",
                        "jp.ootr.ImageDeviceController");
                    return false;
                }

            return true;
        }

        public static bool UpdateDeviceControl(CommonDevice.CommonDevice device, ImageDeviceController[] controllers)
        {
            if (device.controller)
            {
                if (device.controller.devices.Has(device))
                {
                    device.devices = device.controller.devices;
                    return true;
                }

                device.controller = null;
            }

            foreach (var controller in controllers)
            {
                if (!controller.devices.Has(device)) continue;
                device.controller = controller;
                device.devices = controller.devices;
                return true;
            }

            return false;
        }
    }
}
#endif

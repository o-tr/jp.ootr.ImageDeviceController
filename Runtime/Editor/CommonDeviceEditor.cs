using System;
using System.Collections.Generic;
using Cinemachine.Editor;
using jp.ootr.common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase.Editor.BuildPipeline;
using Console = jp.ootr.common.Console;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(CommonDevice.CommonDevice))]
    public class CommonDeviceEditor : UnityEditor.Editor
    {
        private bool _debug;

        private SerializedProperty _deviceName;
        private bool _foldoutState;

        public virtual void OnEnable()
        {
            _deviceName = serializedObject.FindProperty("deviceName");
        }

        public override void OnInspectorGUI()
        {
            _debug = EditorGUILayout.ToggleLeft("Debug", _debug);
            if (_debug)
            {
                base.OnInspectorGUI();
                return;
            }

            ShowScriptName();
            var script = (CommonDevice.CommonDevice)target;
            var so = new SerializedObject(script);

            SetController(script);
            if (script.deviceUuid.IsNullOrEmpty()) CommonDeviceUtils.GenerateUuid(so);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Device UUID", script.deviceUuid);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_deviceName);

            so.ApplyModifiedProperties();
            EditorGUILayout.Space();
            ShowContent();
            EditorGUILayout.Space();

            _foldoutState = EditorGUILayout.Foldout(_foldoutState, "Other", true);

            if (!_foldoutState) return;
            EditorGUI.BeginChangeCheck();
            if (script.splashImage != null)
            {
                var texture = (Texture)EditorGUILayout.ObjectField("Splash Image",
                    script.splashImage.texture, typeof(Texture), false);
                if (texture != script.splashImage.texture)
                {
                    var splashImageProp = serializedObject.FindProperty("splashImage");
                    var splashImage = (RawImage)splashImageProp.objectReferenceValue;
                    var soImage = new SerializedObject(splashImage);
                    soImage.FindProperty("m_Texture").objectReferenceValue = texture;
                    soImage.ApplyModifiedProperties();
                    var slashImageFitterProp = serializedObject.FindProperty("splashImageFitter");
                    var splashImageFitter = (AspectRatioFitter)slashImageFitterProp.objectReferenceValue;
                    var soImageFitter = new SerializedObject(splashImageFitter);
                    soImageFitter.FindProperty("m_AspectRatio").floatValue =
                        texture.width / (float)texture.height;
                    soImageFitter.ApplyModifiedProperties();
                }
            }
        }

        protected virtual void ShowContent()
        {
        }

        private void SetController(CommonDevice.CommonDevice script)
        {
            if (CommonDeviceUtils.UpdateDeviceControl(script, FindObjectsOfType<ImageDeviceController>())) return;

            EditorGUILayout.Space();
            var content =
                new GUIContent(
                    "Please assign this device to ImageDeviceController\n\nこのデバイスをImageDeviceControllerの管理対象に追加してください");
            content.image = EditorGUIUtility.IconContent("console.erroricon").image;
            EditorGUILayout.HelpBox(content);
        }

        protected virtual void ShowScriptName()
        {
            EditorGUILayout.LabelField("CommonDevice", EditorStyle.UiTitle);
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
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var scripts = ComponentUtils.GetAllComponents<CommonDevice.CommonDevice>();
                CommonDeviceUtils.ValidateUuids(scripts.ToArray());
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
            return true;
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
                so.FindProperty("deviceUuid").stringValue = uuid;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(script);
            }
        }

        public static void GenerateUuid(SerializedObject so)
        {
            var uuid = Guid.NewGuid().ToString();
            so.FindProperty("deviceUuid").stringValue = uuid;
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
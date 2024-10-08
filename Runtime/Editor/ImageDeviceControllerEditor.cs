﻿#if UNITY_EDITOR
using System.Linq;
using jp.ootr.common;
using jp.ootr.common.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase.Editor.BuildPipeline;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(ImageDeviceController))]
    public class ImageDeviceControllerEditor : BaseEditor
    {
        private SerializedProperty _devices;
        private SerializedProperty _vlLoadTimeout;
        private SerializedProperty _zlDelayFrames;
        private SerializedProperty _zlPartLength;

        public override void OnEnable()
        {
            base.OnEnable();
            _devices = serializedObject.FindProperty(nameof(ImageDeviceController.devices));
            _zlDelayFrames = serializedObject.FindProperty(nameof(ImageDeviceController.zlDelayFrames));
            _zlPartLength = serializedObject.FindProperty(nameof(ImageDeviceController.zlPartLength));
            _vlLoadTimeout = serializedObject.FindProperty(nameof(ImageDeviceController.vlLoadTimeout));
        }

        protected override string GetScriptName()
        {
            return "Image Device Controller";
        }

        protected override VisualElement GetLayout()
        {
            var root = new VisualElement();
            root.AddToClassList("container");

            root.Add(GetDeviceList());

            root.Add(GetZipLoadDelayFrames());

            root.Add(GetBase64DecodePartSize());

            root.Add(GetVideoFrameLoadTimeout());

            return root;
        }

        private VisualElement GetDeviceList()
        {
            var devices = new PropertyField(_devices);
            devices.RegisterCallback<ChangeEvent<Object>>(evt => { UpdateDevices((ImageDeviceController)target); });
            return devices;
        }

        private VisualElement GetZipLoadDelayFrames()
        {
            var zlDelayFrames = new PropertyField(_zlDelayFrames);
            return zlDelayFrames;
        }

        private VisualElement GetBase64DecodePartSize()
        {
            var zlPartLength = new PropertyField(_zlPartLength);
            return zlPartLength;
        }

        private VisualElement GetVideoFrameLoadTimeout()
        {
            var vlLoadTimeout = new PropertyField(_vlLoadTimeout);
            return vlLoadTimeout;
        }

        private void UpdateDevices(ImageDeviceController script)
        {
            foreach (var device in script.devices)
            {
                if (device == null) continue;
                var so = new SerializedObject(device);
                so.Update();
                so.FindProperty(nameof(CommonDevice.CommonDevice.controller)).objectReferenceValue = script;
                var property = so.FindProperty(nameof(CommonDevice.CommonDevice.devices));
                property.arraySize = script.devices.Length;
                for (var i = 0; i < script.devices.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = script.devices[i];

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(device);
            }

            EditorUtility.SetDirty(script);
        }
    }

    [InitializeOnLoad]
    public class PlayModeNotifier__ImageDeviceController
    {
        static PlayModeNotifier__ImageDeviceController()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var scripts = ComponentUtils.GetAllComponents<ImageDeviceController>();
                foreach (var script in scripts) ImageDeviceControllerUtils.ValidateDeviceList(script);
            }
        }
    }

    public class SetObjectReference__ImageDeviceController : UnityEditor.Editor, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 9;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            var scripts = ComponentUtils.GetAllComponents<ImageDeviceController>();
            foreach (var script in scripts) ImageDeviceControllerUtils.ValidateDeviceList(script);
            return true;
        }
    }

    public static class ImageDeviceControllerUtils
    {
        public static void ValidateDeviceList(ImageDeviceController script)
        {
            if (script.devices == null || script.devices.Length == 0)
            {
                script.devices = new CommonDevice.CommonDevice[0];
                return;
            }

            //filter script devices
            var devices = script.devices.Where(d => d != null).ToArray();
            var so = new SerializedObject(script);
            so.Update();
            var property = so.FindProperty(nameof(ImageDeviceController.devices));
            property.arraySize = devices.Length;
            for (var i = 0; i < devices.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = devices[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(script);
        }
    }
}
#endif

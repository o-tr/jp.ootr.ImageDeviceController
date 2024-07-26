﻿using System.Collections.Generic;
using jp.ootr.common;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;

namespace jp.ootr.ImageDeviceController.Editor
{
    [CustomEditor(typeof(CommonDevice.CommonDevice))]
    public class CommonDeviceEditor : UnityEditor.Editor
    {
        protected bool Debug;
        public override void OnInspectorGUI()
        {
            Debug = EditorGUILayout.ToggleLeft("Debug", Debug);
            if (Debug)
            {
                base.OnInspectorGUI();
                return;
            }
            ShowScriptName();
            var script = (CommonDevice.CommonDevice)target;
            
            SetController(script);
            
            if (script.deviceUuid.IsNullOrEmpty())
            {
                script.deviceUuid = System.Guid.NewGuid().ToString();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Device UUID",script.deviceUuid);
            
        }
        
        private void SetController(CommonDevice.CommonDevice script)
        {
            if (CommonDeviceUtils.UpdateDeviceControl(script, FindObjectsOfType<ImageDeviceController>()))
            {
                return;
            }
            EditorGUILayout.Space();
            GUIContent content = new GUIContent("Please assign this device to ImageDeviceController\n\nこのデバイスをImageDeviceControllerの管理対象に追加してください");
            content.image = EditorGUIUtility.IconContent("console.erroricon").image;
            EditorGUILayout.HelpBox(content);
        }
        
        public virtual void ShowScriptName()
        {
            EditorGUILayout.LabelField("CommonDevice", EditorStyle.UiTitle);
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
            var scripts = GetAllScripts<CommonDevice.CommonDevice>();
            var controllers = GetAllScripts<ImageDeviceController>().ToArray();
            if (!SetEachReference(scripts, controllers) || !ValidateDuplicateReferencedDevices(controllers))
            {
                return false;
            }
            ValidateUuids(scripts.ToArray());
            return true;
        }
        
        public static void ValidateUuids(CommonDevice.CommonDevice[] scripts)
        {
            var usedUuids = new List<string>();
            foreach (var script in scripts)
            {
                if (script.deviceUuid.IsNullOrEmpty())
                {
                    script.deviceUuid = System.Guid.NewGuid().ToString();
                }
                if (usedUuids.Contains(script.deviceUuid))
                {
                    Console.Warn($"Device {script.name}({script.GetInstanceID()})'s UUID is regenerated because it is duplicated","jp.ootr.ImageDeviceController");
                    script.deviceUuid = System.Guid.NewGuid().ToString();
                }
                usedUuids.Add(script.deviceUuid);
            }
        }
        
        public static bool ValidateDuplicateReferencedDevices(ImageDeviceController[] controllers)
        {
            var usedDevices = new List<CommonDevice.CommonDevice>();
            var flag = true;
            foreach (var controller in controllers)
            {
                foreach (var device in controller.devices)
                {
                    if (usedDevices.Contains(device))
                    {
                        Console.Error($"Device {device.name}({device.GetInstanceID()}) is referenced by multiple ImageDeviceControllers","jp.ootr.ImageDeviceController");
                        flag = false;
                    }
                    usedDevices.Add(device);
                }
            }
            return flag;
        }
        
        public static bool SetEachReference(List<CommonDevice.CommonDevice> scripts, ImageDeviceController[] controllers)
        {
            foreach (var script in scripts)
            {
                if (!UpdateDeviceControl(script, controllers))
                {
                    Console.Error($"Device {script.name}({script.GetInstanceID()}) is not assigned to any ImageDeviceController","jp.ootr.ImageDeviceController");
                    return false;
                }
            }
            return true;
        }
        
        public static bool UpdateDeviceControl(CommonDevice.CommonDevice device, ImageDeviceController[] controllers)
        {
            if (device.controller)
            {
                if (device.controller.devices.Has(device))
                {
                    return true;
                }
                device.controller = null;
            }
            foreach (var controller in controllers)
            {
                if (!controller.devices.Has(device)) continue;
                device.controller = controller;
                return true;
            }
            return false;
        }
        
        public static List<T> GetAllScripts<T>()
        {
            var controllers = new List<T>();
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                controllers.AddRange(rootObject.GetComponentsInChildren<T>());
            }
            return controllers;
        }
    }
}
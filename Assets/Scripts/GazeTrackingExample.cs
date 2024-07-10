// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using MagicLeap.Android;
using MagicLeap.Examples;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public class GazeTrackingExample : MonoBehaviour
{
    [SerializeField, Tooltip("UserInterface for displaying issues")]
    private UserInterface userInterface;

    [SerializeField]
    private GazeVisualizer visualizer;

    private List<InputDevice> InputDeviceList = new();
    private InputDevice eyeTracking;
    private Camera mainCamera;

    private bool permissionGranted;

    private void Awake()
    {
        Permissions.RequestPermission(Permissions.EyeTracking, OnPermissionGranted, OnPermissionDenied);
    }

    void Update()
    {
        if (!permissionGranted)
            return;

        if (!eyeTracking.isValid)
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, InputDeviceList);
            eyeTracking = InputDeviceList.FirstOrDefault();

            if (!eyeTracking.isValid)
            {
                userInterface.AddIssue("Unable to acquire eye tracking device. Have permissions been granted?");
                return;
            }
        }
        
        bool hasData = eyeTracking.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
        hasData &= eyeTracking.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out Vector3 position);
        hasData &= eyeTracking.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out Quaternion rotation);

        if(isTracked && hasData)
        {
            transform.SetLocalPositionAndRotation(position + (rotation * Vector3.forward), rotation);
        }

        var ray = new Ray(mainCamera.transform.position, visualizer.gameObject.transform.position - mainCamera.transform.position);
        if(Physics.Raycast(ray, out RaycastHit info))
        {
            visualizer.Show(info.transform.position);
        }
        else
        {
            visualizer.Hide();
        }
    }

    void OnPermissionGranted(string permission) 
    {
        permissionGranted = true;
        mainCamera = Camera.main;
    }

    void OnPermissionDenied(string permission)
    {
        userInterface.AddIssue($"{permission} denied, example won't function");
    }

    private void Reset()
    {
        userInterface = GameObject.Find("UserInterface").GetComponent<UserInterface>();
    }
}

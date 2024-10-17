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
using System.Text;
using MagicLeap.OpenXR.Features.EyeTracker;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.NativeTypes;

public class GazeTrackingExample : MonoBehaviour
{
    public enum GazeTrackingExampleGazeType { EyeGazeExt, EyeGazeML, Fixation }
    
    [SerializeField, Tooltip("UserInterface for displaying issues")]
    private UserInterface userInterface;

    [SerializeField] 
    private Transform fixationPointTransform;
    
    [SerializeField] 
    private MeshRenderer[] sphereRenderers;

    [SerializeField] 
    private Material blueMaterial;
    
    [SerializeField] 
    private Material redMaterial;

    [SerializeField] 
    private Text statusText;

    [SerializeField] 
    private float movementThreshold = 0.01f;

    private List<InputDevice> InputDeviceList = new();
    private InputDevice eyeTrackingDevice;
    private Camera mainCamera;
    private MagicLeapEyeTrackerFeature eyeTrackerFeature;

    private bool eyeTrackPermission;
    private bool pupilSizePermission;
    private bool isInitialized;
    private bool isPupilTrackingEnabled;
    private bool isDeviceVerified;
    private GazeTrackingExampleGazeType currentGazeType;
    private PupilData[] currentPupilData;
    private GeometricData[] currentGeometricData;
    private StringBuilder statusStringBuilder;
    private EyeTrackerData eyeTrackerData;

    private void Awake()
    {
        Permissions.RequestPermissions(new string[] { Permissions.EyeTracking, Permissions.PupilSize }, OnPermissionGranted, OnPermissionDenied, OnPermissionDenied);

        mainCamera = Camera.main;
        isPupilTrackingEnabled = true;

        statusStringBuilder = new StringBuilder();
    }

    private void Update()
    {
        if (!ArePermissionsGranted())
            return;

        if (!isInitialized)
        {
            Initialize();
            return;
        }

        if (IsEyeTrackingDeviceValid())
        {
            ShowEyeTrackingVisualization();

            if (isPupilTrackingEnabled)
            {
                DisplayPupilSizeOutput();
            }
            else
            {
                statusText.text = "";
            }
        }
        else
        {
            statusText.text = "";
        }
    }

    public void SelectGazeType(int gazeTypeNum)
    {
        currentGazeType = (GazeTrackingExampleGazeType)gazeTypeNum;
        isDeviceVerified = false;
    }

    public void TogglePupilSizeTracking(bool isOn)
    {
        isPupilTrackingEnabled = isOn;
    }
    
    private void Initialize()
    {
        eyeTrackerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapEyeTrackerFeature>();
        eyeTrackerFeature.CreateEyeTracker();
        isInitialized = true;
    }

    private void OnPermissionGranted(string permission)
    {
        if (permission == Permissions.EyeTracking)
            eyeTrackPermission = true;

        if (permission == Permissions.PupilSize)
            pupilSizePermission = true;
    }

    private void OnPermissionDenied(string permission)
    {
        if (permission == Permissions.EyeTracking || permission == Permissions.PupilSize)
            userInterface.AddIssue($"{permission} denied, example won't function");
    }

    private void Reset()
    {
        userInterface = GameObject.Find("UserInterface").GetComponent<UserInterface>();
    }
    
    private bool ArePermissionsGranted()
    {
        if (!eyeTrackPermission)
        {
            userInterface.AddIssue($"waiting on permission {Permissions.EyeTracking} to be granted");
        }

        if (!pupilSizePermission)
        {
            userInterface.AddIssue($"waiting on permission {Permissions.PupilSize} to be granted");
        }

        return eyeTrackPermission && pupilSizePermission;
    }
    
    private bool IsEyeTrackingDeviceValid()
    {
        if (currentGazeType == GazeTrackingExampleGazeType.EyeGazeExt && (!eyeTrackingDevice.isValid || !isDeviceVerified))
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, InputDeviceList);

            eyeTrackingDevice = InputDeviceList.Find(device => device.name == "Eye Tracking OpenXR");

            if (eyeTrackingDevice == null || !eyeTrackingDevice.isValid)
            {
                userInterface.AddIssue("Unable to acquire eye tracking device. Have permissions been granted?");
                return false;
            }
        }
        else if ((currentGazeType == GazeTrackingExampleGazeType.EyeGazeML ||
                  currentGazeType == GazeTrackingExampleGazeType.Fixation) &&
                 (!eyeTrackingDevice.isValid || !isDeviceVerified))
        {
            eyeTrackerData = eyeTrackerFeature.GetEyeTrackerData();

            if (eyeTrackerData.PosesData.Result != XrResult.Success)
            {
                userInterface.AddIssue("Unable to acquire eye tracking device. Have permissions been granted?");
                return false;
            }
        }

        isDeviceVerified = true;
        return true;
    }
    
    private void ShowEyeTrackingVisualization()
    {
        bool hasData;
        Vector3 gazePosition;
        Quaternion gazeRotation;
        Vector3 offsetFromFace;

        if (currentGazeType == GazeTrackingExampleGazeType.EyeGazeExt)
        {
            hasData = eyeTrackingDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) & 
                      eyeTrackingDevice.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out gazePosition) &
                      eyeTrackingDevice.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out gazeRotation);

            offsetFromFace = gazeRotation * Vector3.forward;
        }
        else if (currentGazeType == GazeTrackingExampleGazeType.EyeGazeML)
        {
            eyeTrackerData = eyeTrackerFeature.GetEyeTrackerData();

            hasData = eyeTrackerData.PosesData.Result == XrResult.Success;

            gazePosition = eyeTrackerData.PosesData.GazePose.Pose.position;
            gazeRotation = eyeTrackerData.PosesData.GazePose.Pose.rotation;
            
            offsetFromFace = gazeRotation * Vector3.forward;
        }
        else if (currentGazeType == GazeTrackingExampleGazeType.Fixation)
        {
            eyeTrackerData = eyeTrackerFeature.GetEyeTrackerData();
            
            hasData = eyeTrackerData.PosesData.Result == XrResult.Success;

            gazePosition = eyeTrackerData.PosesData.FixationPose.Pose.position;
            gazeRotation = eyeTrackerData.PosesData.FixationPose.Pose.rotation;
            
            offsetFromFace = Vector3.zero;
        }
        else
        {
            return;
        }

        Vector3 targetGazePosition = gazePosition + offsetFromFace;

        if(hasData)
        {
            if (Vector3.Distance(fixationPointTransform.position, targetGazePosition) >= movementThreshold)
            {
                fixationPointTransform.SetLocalPositionAndRotation(targetGazePosition, gazeRotation);
            }
        }

        var ray = new Ray(mainCamera.transform.position, fixationPointTransform.position - mainCamera.transform.position);
        if(Physics.Raycast(ray, out RaycastHit info))
        {
            foreach (var sphere in sphereRenderers)
            {
                if (info.transform.gameObject == sphere.gameObject)
                {
                    sphere.sharedMaterial = redMaterial;
                }
                else
                {
                    sphere.sharedMaterial = blueMaterial;
                }
            }
        }
        else
        {
            foreach (var sphere in sphereRenderers)
            {
                sphere.sharedMaterial = blueMaterial;
            }
        }
    }

    private void DisplayPupilSizeOutput()
    {
        currentPupilData = eyeTrackerData.PupilData;
        currentGeometricData = eyeTrackerData.GeometricData;

        float leftPupilDiameter = 0;
        float rightPupilDiameter = 0;
        float leftEyeOpenness = 0;
        float rightEyeOpenness = 0;

        foreach (var pupilData in currentPupilData)
        {
            if (pupilData.Eye == Eye.Left)
            {
                leftPupilDiameter = pupilData.PupilDiameter;
            }
            else if (pupilData.Eye == Eye.Right)
            {
                rightPupilDiameter = pupilData.PupilDiameter;
            }
        }

        foreach (var geometricData in currentGeometricData)
        {
            if (geometricData.Eye == Eye.Left)
            {
                leftEyeOpenness = geometricData.EyeOpenness;
            }
            else if (geometricData.Eye == Eye.Right)
            {
                rightEyeOpenness = geometricData.EyeOpenness;
            }
        }

        statusStringBuilder.Clear();
        
        statusStringBuilder.AppendLine("Pupil Data:");
        statusStringBuilder.AppendLine();
        statusStringBuilder.AppendLine($"Left Eye Diameter: { leftPupilDiameter }");
        statusStringBuilder.AppendLine($"Right Eye Diameter: { rightPupilDiameter}");
        statusStringBuilder.AppendLine();
        statusStringBuilder.AppendLine("Eye Openness:");
        statusStringBuilder.AppendLine();
        statusStringBuilder.AppendLine($"Left Eye Openness: { leftEyeOpenness }");
        statusStringBuilder.AppendLine($"Right Eye Openness: { rightEyeOpenness }");

        statusText.text = statusStringBuilder.ToString(); 
    }
}

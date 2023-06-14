// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2021-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using InputDevice = UnityEngine.XR.InputDevice;

namespace MagicLeap.Examples
{
    /// <summary>
    /// Behaviour that moves object to fixation point position.
    /// </summary>
    public class EyeTrackingExample : MonoBehaviour
    {
        [SerializeField, Tooltip("Left Eye Statistic Panel")]
        private Text leftEyeTextStatic;
        [SerializeField, Tooltip("Right Eye Statistic Panel")]
        private Text rightEyeTextStatic;
        [SerializeField, Tooltip("Both Eyes Statistic Panel")]
        private Text bothEyesTextStatic;
        [SerializeField, Tooltip("Fixation Point marker")]
        private Transform eyesFixationPoint;

        // Used to get ml inputs.
        private MagicLeapInputs mlInputs;

        // Used to get eyes action data.
        private MagicLeapInputs.EyesActions eyesActions;

        // Used to get other eye data
        private InputDevice eyesDevice;

        // Was EyeTracking permission granted by user
        private bool permissionGranted = false;
        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        private void Awake()
        {
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        }

        private void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();

            MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);
        }

        private void OnDestroy()
        {
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            mlInputs.Disable();
            mlInputs.Dispose();

            InputSubsystem.Extensions.MLEyes.StopTracking();
        }

        private void Update()
        {
            if (!permissionGranted)
            {
                return;
            }

            if (!eyesDevice.isValid)
            {
                this.eyesDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
                return;
            }

            // Eye data provided by the engine for all XR devices.
            // Used here only to update the status text. The 
            // left/right eye centers are moved to their respective positions &
            // orientations using InputSystem's TrackedPoseDriver component.
            var eyes = eyesActions.Data.ReadValue<UnityEngine.InputSystem.XR.Eyes>();

            // Manually set fixation point marker so we can apply rotation, since UnityXREyes
            // does not provide it
            eyesFixationPoint.position = eyes.fixationPoint;
            eyesFixationPoint.rotation = Quaternion.LookRotation(eyes.fixationPoint - Camera.main.transform.position);

            // Eye data specific to Magic Leap
            InputSubsystem.Extensions.TryGetEyeTrackingState(eyesDevice, out var trackingState);

            var leftEyeForwardGaze = eyes.leftEyeRotation * Vector3.forward;

            string leftEyeText =
                $"Center:\n({eyes.leftEyePosition.x:F2}, {eyes.leftEyePosition.y:F2}, {eyes.leftEyePosition.z:F2})\n" +
                $"Gaze:\n({leftEyeForwardGaze.x:F2}, {leftEyeForwardGaze.y:F2}, {leftEyeForwardGaze.z:F2})\n" +
                $"Confidence:\n{trackingState.LeftCenterConfidence:F2}\n" +
                $"Openness:\n{eyes.leftEyeOpenAmount:F2}";

            leftEyeTextStatic.text = leftEyeText;

            var rightEyeForwardGaze = eyes.rightEyeRotation * Vector3.forward;

            string rightEyeText =
                $"Center:\n({eyes.rightEyePosition.x:F2}, {eyes.rightEyePosition.y:F2}, {eyes.rightEyePosition.z:F2})\n" +
                $"Gaze:\n({rightEyeForwardGaze.x:F2}, {rightEyeForwardGaze.y:F2}, {rightEyeForwardGaze.z:F2})\n" +
                $"Confidence:\n{trackingState.RightCenterConfidence:F2}\n" +
                $"Openness:\n{eyes.rightEyeOpenAmount:F2}";

            rightEyeTextStatic.text = rightEyeText;

            string bothEyesText =
                $"Fixation Point:\n({eyes.fixationPoint.x:F2}, {eyes.fixationPoint.y:F2}, {eyes.fixationPoint.z:F2})\n" +
                $"Confidence:\n{trackingState.FixationConfidence:F2}";

            bothEyesTextStatic.text = $"{bothEyesText}";

            if (trackingState.RightBlink || trackingState.LeftBlink)
            {
                Debug.Log($"Eye Tracking Blink Registered Right Eye Blink: {trackingState.RightBlink} Left Eye Blink: {trackingState.LeftBlink}");
            }
        }

        private void OnPermissionDenied(string permission)
        {
            MLPluginLog.Error($"{permission} denied, example won't function.");
        }

        private void OnPermissionGranted(string permission)
        {
            InputSubsystem.Extensions.MLEyes.StartTracking();
            eyesActions = new MagicLeapInputs.EyesActions(mlInputs);
            permissionGranted = true;
        }
    }
}


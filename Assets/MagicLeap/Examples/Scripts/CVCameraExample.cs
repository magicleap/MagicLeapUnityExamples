// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;


namespace MagicLeap.Examples
{
    /// <summary>
    /// This class handles video recording and loading based on controller
    /// input.
    /// </summary>
    public class CVCameraExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("Refrence to the Raw Video Capture Visualizer gameobject for YUV frames")]
        private CameraCaptureVisualizer cameraCaptureVisualizer = null;

        [SerializeField, Tooltip("Desired width for the camera capture")]
        private int captureWidth = 1280;

        [SerializeField, Tooltip("Desired height for the camera capture")]
        private int captureHeight = 720;

        private bool isCameraConnected;
        private MLCamera.StreamCapability selectedCapability;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        private MLCamera colorCamera;
        private bool cameraDeviceAvailable = false;
        private bool isCapturing;
        private string poseText;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        /// <summary>
        /// Using Awake so that Permissions is set before PermissionRequester Start.
        /// </summary>
        void Awake()
        {
            if (_statusText == null)
            {
                Debug.LogError("Error: CVCameraExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (cameraCaptureVisualizer == null)
            {
                Debug.LogError("Error: CVCameraExample._rawVideoCaptureVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            isCapturing = false;

            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            MLPermissions.RequestPermission(MLPermission.Camera, permissionCallbacks);
        }

        /// <summary>
        /// Stop the camera, unregister callbacks, and stop input and permissions APIs.
        /// </summary>
        void OnDisable()
        {
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            controllerActions.Bumper.performed -= OnButtonDown;
            mlInputs.Dispose();

            if (colorCamera != null && isCameraConnected)
            {
                DisableMLCamera();
            }
        }

        /// <summary>
        /// Display permission error if necessary or update status text.
        /// </summary>
        private void Update()
        {
            UpdateStatusText();
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);
            _statusText.text += $"\nCamera Available: {cameraDeviceAvailable}";
            if (isCameraConnected)
            {
                _statusText.text += $"\nStream width: {selectedCapability.Width} \nStream height{selectedCapability.Height}";
            }
            if (!string.IsNullOrEmpty(poseText))
            {
                _statusText.text += poseText;
            }
        }

        /// <summary>
        /// Captures a still image using the device's camera and returns
        /// the data path where it is saved.
        /// </summary>
        /// <param name="fileName">The name of the file to be saved to.</param>
        private void StartVideoCapture()
        {
            MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.RGBA_8888;
            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig();
            captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS;
            captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
            captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create(selectedCapability, outputFormat);
            MLResult result = colorCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);
            if (result.IsOk)
            {
                result = colorCamera.PreCaptureAEAWB();
                result = colorCamera.CaptureVideoStart();
                if (!result.IsOk)
                {
                    Debug.LogError("Failed to start video capture!");
                }
                else
                {
                    cameraCaptureVisualizer.DisplayCapture(outputFormat, false);
                }
            }

            isCapturing = result.IsOk;
        }

        private void StopVideoCapture()
        {
            if (isCapturing)
            {
                colorCamera.CaptureVideoStop();
                cameraCaptureVisualizer.HideRenderer();
            }

            isCapturing = false;
        }

        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        private IEnumerator EnableMLCamera()
        {
            if (colorCamera != null)
            {
                yield return null;
            }

            while (!cameraDeviceAvailable)
            {
                MLResult result = MLCamera.GetDeviceAvailabilityStatus(MLCamera.Identifier.CV, out cameraDeviceAvailable);
                if (!(result.IsOk && cameraDeviceAvailable))
                {
                    // Wait until camera device is available
                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("Camera device available");
            yield return new WaitForSeconds(1.0f);

            MLCamera.ConnectContext context = MLCamera.ConnectContext.Create();
            context.EnableVideoStabilization = true;
            context.CamId = MLCamera.Identifier.CV;

            colorCamera = MLCamera.CreateAndConnect(context);
            if (colorCamera != null)
            {
                Debug.Log("Camera device connected");
                isCameraConnected = true;
                MLCamera.StreamCapability[] streamCapabilities = MLCamera.GetImageStreamCapabilitiesForCamera(colorCamera, MLCamera.CaptureType.Video);
                if (streamCapabilities == null || streamCapabilities.Length <= 0)
                {
                    Debug.LogError("Camera device unable to received stream caps.");
                    yield break;
                }
                
                if (!MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, captureWidth, captureHeight,
                    MLCamera.CaptureType.Video, out selectedCapability))
                {
                    Debug.LogError("Camera device unable to fit stream caps to chosen options.");
                    yield break;
                }
                    
                Debug.Log("Camera device received stream caps");
                colorCamera.OnRawVideoFrameAvailable += OnCaptureRawVideoFrameAvailable;
                controllerActions.Bumper.performed += OnButtonDown;
            }
        }

        /// <summary>
        /// Disconnects the MLCamera if it was ever created or connected.
        /// </summary>
        private void DisableMLCamera()
        {
            if (colorCamera != null)
            {
                colorCamera.OnRawVideoFrameAvailable -= OnCaptureRawVideoFrameAvailable;
                colorCamera.Disconnect();
                // Explicitly set to false here as the disconnect was attempted.
                isCameraConnected = false;
                colorCamera = null;
            }
        }

        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(InputAction.CallbackContext obj)
        {
            if (!isCapturing)
            {
                StartVideoCapture();
            }
            else
            {
                StopVideoCapture();
            }
        }

        /// <summary>
        /// Handles the event of a new image getting captured.
        /// </summary>
        /// <param name="imageData">The raw data of the image.</param>
        private void OnCaptureRawVideoFrameAvailable(MLCamera.CameraOutput capturedFrame, MLCamera.ResultExtras resultExtras)
        {
            cameraCaptureVisualizer.OnCaptureDataReceived(resultExtras, capturedFrame);
#if UNITY_ANDROID
            if (MLCVCamera.GetFramePose(resultExtras.VCamTimestamp, out Matrix4x4 cameraTransform).IsOk)
            {
                poseText = $"Cam Pose: {cameraTransform.GetPosition()}; {cameraTransform.rotation}";
            }
            else
            {
                poseText = String.Empty;
            }
#endif
        }

        private void OnPermissionDenied(string permission)
        {
#if UNITY_ANDROID
            MLPluginLog.Error($"{permission} denied, example won't function.");
#endif
        }

        private void OnPermissionGranted(string permission)
        {
            StartCoroutine(EnableMLCamera());
        }
    }
}


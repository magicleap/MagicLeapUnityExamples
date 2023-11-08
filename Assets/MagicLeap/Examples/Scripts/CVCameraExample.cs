// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;


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

        private StringBuilder statusText = new();
        private Coroutine enableCameraCoroutine;
        
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
            
            controllerActions.Bumper.performed += OnButtonDown;

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
            if (colorCamera != null && isCameraConnected)
            {
                DisableMLCamera();
            }
            controllerActions.Bumper.performed -= OnButtonDown;
            mlInputs.Dispose();

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
            statusText.Clear();
            statusText.AppendLine($"<color=#B7B7B8><b>Controller Data</b></color>\nStatus: {ControllerStatus.Text}");
            statusText.AppendLine($"\nCamera Available: {cameraDeviceAvailable}");
            if (isCameraConnected)
            {
                statusText.AppendLine($"\nStream width: {selectedCapability.Width} \nStream height{selectedCapability.Height}");
            }
            else
            {
                statusText.AppendLine("Camera Disconnected");
            }
            if (!string.IsNullOrEmpty(poseText))
            {
                statusText.AppendLine(poseText);
            }

            if (colorCamera is {IsPaused: true})
            {
                statusText.AppendLine("Waiting for camera to resume");
            }

            _statusText.text = statusText.ToString();
        }

        private void CheckAndStopPreviousCoroutine()
        {
            if (enableCameraCoroutine == null) return;
            StopCoroutine(enableCameraCoroutine);
            enableCameraCoroutine = null;
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                colorCamera.OnRawVideoFrameAvailable -= OnCaptureRawVideoFrameAvailable;
            }
            else
            {
                colorCamera.OnRawVideoFrameAvailable += OnCaptureRawVideoFrameAvailable;
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
            if (colorCamera is { IsPaused: true } || !isCameraConnected)
            {
                return;
            }
            
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
        private void OnCaptureRawVideoFrameAvailable(MLCamera.CameraOutput capturedFrame, MLCamera.ResultExtras resultExtras, MLCamera.Metadata metadataHandle)
        {
            if (colorCamera is { IsPaused: true })
            {
                return;
            }
            cameraCaptureVisualizer.OnCaptureDataReceived(resultExtras, capturedFrame);

            if (MLCVCamera.GetFramePose(resultExtras.VCamTimestamp, out Matrix4x4 cameraTransform).IsOk)
            {
                poseText = $"Cam Pose: {cameraTransform.GetPosition()}; {cameraTransform.rotation}";
            }
            else
            {
                poseText = String.Empty;
            }
        }

        private void OnPermissionDenied(string permission)
        {
            MLPluginLog.Error($"{permission} denied, example won't function.");
        }

        private void OnPermissionGranted(string permission)
        {
            enableCameraCoroutine = StartCoroutine(EnableMLCamera());
        }
    }
}


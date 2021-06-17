// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using System;
using System.Text;

namespace MagicLeap
{
    /// <summary>
    /// This class handles video recording and loading based on controller
    /// input.
    /// </summary>
    public class RawVideoCapturePreviewExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        [Space, SerializeField, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("Refrence to the Privilege requester Prefab")]
        private MLPrivilegeRequesterBehavior _privilegeRequester = null;

        [SerializeField, Tooltip("Refrence to the Raw Video Capture Visualizer gameobject")]
        private RawVideoCaptureVisualizer _rawVideoCaptureVisualizer = null;

        private string _intrinsicValuesText = null;

        // Is the camera currently recording
        private bool _isCapturing = false;

        private bool _isCameraConnected = false;

        private float _captureStartTime = 0.0f;

        private bool _hasStarted = false;

        #pragma warning disable 414
        private bool _appPaused = false;
        #pragma warning restore 414

        #if PLATFORM_LUMIN
        private event Action<MLCamera.ResultExtras, MLCamera.YUVFrameInfo, MLCamera.FrameMetadata> OnRawVideoDataReceived = null;
        #endif

        #pragma warning disable 414
        private event Action OnRawVideoCaptureStarted = null;

        private event Action OnRawVideoCaptureEnded = null;
        #pragma warning restore 414

        // Using Awake so that Privileges is set before MLPrivilegeRequesterBehavior Start
        void Awake()
        {
            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: VideoCamptureExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_privilegeRequester == null)
            {
                Debug.LogError("Error: RawVideoCapturePreviewExample._privilegeRequester is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: RawVideoCapturePreviewExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            // Before enabling the Camera, the scene must wait until the privileges have been granted.
            _privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;
            #endif

            OnRawVideoCaptureStarted += _rawVideoCaptureVisualizer.OnCaptureStarted;
            OnRawVideoCaptureEnded += _rawVideoCaptureVisualizer.OnCaptureEnded;

            #if PLATFORM_LUMIN
            MLCamera.OnCameraConnected += OnCameraConnected;
            MLCamera.OnCameraDisconnected += OnCameraDisconnected;

            MLCamera.OnCameraCaptureStarted += OnCameraCaptureStarted;
            MLCamera.OnCameraCaptureCompleted +=OnCameraCaptureCompleted;

            OnRawVideoDataReceived += _rawVideoCaptureVisualizer.OnRawCaptureDataReceived;
            #endif
        }

        #if PLATFORM_LUMIN
        private void OnCameraConnected(MLResult result)
        {
            if (result.IsOk)
            {
                MLCamera.OnRawVideoFrameAvailableYUV += OnRawCaptureDataReceived;
                _isCameraConnected = true;
            }
            else
            {
                Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to connect camera. Error Code: {0}", MLCamera.GetErrorCode().ToString());
            }
        }

        private void OnCameraDisconnected(MLResult result)
        {
            _isCameraConnected = false;
            MLCamera.OnRawVideoFrameAvailableYUV -= OnRawCaptureDataReceived;
        }

        private void OnCameraCaptureStarted(MLResult result, string pathName)
        {
            if (result.IsOk)
            {
                _isCapturing = true;
                _captureStartTime = Time.time;
                OnRawVideoCaptureStarted.Invoke();
                SetupCameraIntrinsics();
            }
            else
            {
                Debug.LogError($"Error: RawVideoCapturePreviewExample failed to start raw video capture. Reason: {MLCamera.GetErrorCode()}");
            }
        }

        private void OnCameraCaptureCompleted(MLResult result)
        {
            if (result.IsOk)
            {
                OnRawVideoCaptureEnded.Invoke();

                _isCapturing = false;
                _captureStartTime = 0;
            }
            else
            {
                Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to end video capture. Error Code: {0}", MLCamera.GetErrorCode().ToString());
            }
        }
        #endif

        void Update()
        {
            UpdateStatusText();
        }

        /// <summary>
        /// Stop the camera, unregister callbacks, and stop input and privileges APIs.
        /// </summary>
        void OnDisable()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= OnButtonDown;
            #endif

            if (_isCameraConnected)
            {
                DisableMLCamera();
            }
        }

        /// <summary>
        /// Cannot make the assumption that a privilege is still granted after
        /// returning from pause. Return the application to the state where it
        /// requests privileges needed and clear out the list of already granted
        /// privileges. Also, disable the camera and unregister callbacks.
        /// </summary>
        void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _appPaused = true;

                #if PLATFORM_LUMIN
                if (_isCameraConnected)
                {
                    MLResult result = MLCamera.ApplicationPause(_appPaused);
                    if (!result.IsOk)
                    {
                        Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to pause MLCamera, disabling script. Reason: {0}", result);
                        enabled = false;
                        return;
                    }

                    if (_isCapturing)
                    {
                        OnRawVideoCaptureEnded.Invoke();
                    }

                    _isCapturing = false;
                    _captureStartTime = 0;
                    _isCameraConnected = false;
                }

                MLInput.OnControllerButtonDown -= OnButtonDown;
                #endif
            }
        }

        void OnDestroy()
        {
            if (_privilegeRequester != null)
            {
                #if PLATFORM_LUMIN
                _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
                #endif
            }

            OnRawVideoCaptureStarted -= _rawVideoCaptureVisualizer.OnCaptureStarted;
            OnRawVideoCaptureEnded -= _rawVideoCaptureVisualizer.OnCaptureEnded;

            #if PLATFORM_LUMIN
            OnRawVideoDataReceived -= _rawVideoCaptureVisualizer.OnRawCaptureDataReceived;
            MLCamera.OnCameraCaptureStarted -= OnCameraCaptureStarted;
            MLCamera.OnCameraCaptureCompleted -= OnCameraCaptureCompleted;

            MLCamera.OnCameraConnected -= OnCameraConnected;
            MLCamera.OnCameraDisconnected -= OnCameraDisconnected;
            MLCamera.OnRawVideoFrameAvailableYUV -= OnRawCaptureDataReceived;
            #endif
        }

        /// <summary>
        /// Start capturing video.
        /// </summary>
        public void StartCapture()
        {
            #if PLATFORM_LUMIN
            if (!_isCapturing && _isCameraConnected)
            {
                MLCamera.StartRawVideoCaptureAsync();
            }
            else
            {
                Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to start raw video capture.");
            }
            #endif
        }

        /// <summary>
        /// Stop capturing video.
        /// </summary>
        public void EndCapture()
        {
            if (_isCapturing)
            {
                #if PLATFORM_LUMIN
                MLCamera.StopVideoCaptureAsync();
                #endif
            }
            else
            {
                Debug.LogError("Error: RawVideoCapturePreviewExample failed to end video capture because the camera is not recording.");
            }
        }

        /// <summary>
        /// Update Status Tabin Ui.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data </b></color>\nStatus: {0}\n", ControllerStatus.Text);

            _statusText.text += "\n<color=#dbfb76><b>VideoData</b></color>:\n";

            _statusText.text += "Mode: Raw Video Capture\n";

            _statusText.text += _intrinsicValuesText;
        }

        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        private void EnableMLCamera()
        {
            #if PLATFORM_LUMIN
            MLCamera.ConnectAsync();
            #endif
        }

        /// <summary>
        /// Disconnects the MLCamera if it was ever created or connected.
        /// Also stops any video recording if active.
        /// </summary>
        private void DisableMLCamera()
        {
            #if PLATFORM_LUMIN
            MLCamera.DisconnectAsync();
            #endif
        }

        /// <summary>
        /// Enable the camera and callbacks. Called once privileges have been granted.
        /// </summary>
        private void EnableCapture()
        {
            if (!_hasStarted)
            {
                EnableMLCamera();
                #if PLATFORM_LUMIN
                MLInput.OnControllerButtonDown += OnButtonDown;
                #endif
                _hasStarted = true;
            }
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        private void HandlePrivilegesDone(MLResult result)
        {
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to get all requested privileges, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }

            Debug.Log("Succeeded in requesting all privileges");

            // Called here because it needs privileges to be granted first on resume by MLPrivilegeRequesterBehavior.
            if (_appPaused)
            {
                _appPaused = false;

                result = MLCamera.ApplicationPause(_appPaused);
                if (!result.IsOk)
                {
                    Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to resume MLCamera, disabling script. Reason: {0}", result);
                    enabled = false;
                    return;
                }

                _isCameraConnected = true;

                MLInput.OnControllerButtonDown += OnButtonDown;
            }
            else
            {
                EnableCapture();
            }
        }

        /// <summary>
        /// Handles the event for raw capture data recieved, and forwards it to any listeners.
        /// </summary>
        /// <param name="extras">Contains timestamp to use with GetFramePose, also forwarded to listeners.</param>
        /// <param name="frameData">Forwarded to listeners.</param>
        /// <param name="frameMetadata">Forwarded to listeners.</param>
        private void OnRawCaptureDataReceived(MLCamera.ResultExtras extras, MLCamera.YUVFrameInfo frameData, MLCamera.FrameMetadata frameMetadata)
        {
            OnRawVideoDataReceived?.Invoke(extras, frameData, frameMetadata);
        }
        #endif

        /// <summary>
        /// Handles the event for button down. Starts or stops recording.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId) && MLInput.Controller.Button.Bumper == button)
            {
                if (!_isCapturing)
                {
                    StartCapture();
                }
                else
                {
                    EndCapture();
                }
            }
        }

        /// <summary>
        /// Setup the text field for camera intrinsic values.
        /// Precondition: MLCamera must be successfully started.
        /// </summary>
        void SetupCameraIntrinsics()
        {
            #if PLATFORM_LUMIN
            MLCVCamera.IntrinsicCalibrationParameters parameters;
            MLResult result = MLCVCamera.GetIntrinsicCalibrationParameters(out parameters);
            if (result.IsOk)
            {
                _intrinsicValuesText = CalibrationParametersToString(parameters);
            }
            else
            {
                Debug.LogErrorFormat("Error: RawVideoCapturePreviewExample failed to GetIntrinsicCalibrationParameters. Reason: {0}", result);
            }
            #endif
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Convert camera calibration parameters to a string.
        /// </summary>
        /// <param name="parameters">The camera calibration values to pull from.</param>
        static string CalibrationParametersToString(MLCVCamera.IntrinsicCalibrationParameters parameters)
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("\n <color=#dbfb76><b>Camera  Intrinsic Values:</b></color>")
                .AppendFormat("\n   Width: {0}", parameters.Width)
                .AppendFormat("\n   Height: {0}", parameters.Height)
                .AppendFormat("\n   Focal Length: {0}", parameters.FocalLength)
                .AppendFormat("\n   Principal Point: {0}", parameters.PrincipalPoint)
                .AppendFormat("\n   FOV: {0}", parameters.FOV)
                .AppendFormat("\n   Distortion Coeff:");
            for (int i = 0; i < parameters.Distortion.Length; ++i)
            {
                b.AppendFormat("\n   [{0}]: {1}", i, parameters.Distortion[i]);
            }
            return b.ToString();
        }
        #endif

    }
}

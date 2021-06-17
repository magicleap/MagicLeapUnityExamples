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
    public class VideoCaptureExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The maximum amount of time the camera can be recording for (in seconds.)")]
        private float _maxRecordingTime = 10.0f;

        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        [Space, SerializeField, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("Refrence to the Privilege requester Prefab")]
        private MLPrivilegeRequesterBehavior _privilegeRequester = null;

        [SerializeField, Tooltip("Refrence to the Video Capture Visualizer gameobject")]
        private VideoCaptureVisualizer _videoCaptureVisualizer = null;

        private const string _validFileFormat = ".mp4";

        private const float _minRecordingTime = 1.0f;

        // Is the camera currently recording
        private bool _isCapturing = false;

        // The file path to the active capture
        private string _captureFilePath;

        private bool _isCameraConnected = false;

        private float _captureStartTime = 0.0f;

        private bool _hasStarted = false;

        #pragma warning disable 414
        private bool _appPaused = false;
        #pragma warning restore 414

        #pragma warning disable 414
        private event Action OnVideoCaptureStarted = null;

        private event Action<string> OnVideoCaptureEnded = null;
        #pragma warning restore 414

        /// <summary>
        /// Validate that _maxRecordingTime is not less than minimum possible.
        /// </summary>
        void OnValidate()
        {
            if (_maxRecordingTime < _minRecordingTime)
            {
                Debug.LogWarning(string.Format("You can not have a MaxRecordingTime less than {0}, setting back to minimum allowed!", _minRecordingTime));
                _maxRecordingTime = _minRecordingTime;
            }
        }

        // Using Awake so that Privileges is set before MLPrivilegeRequesterBehavior Start
        void Awake()
        {
            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: VideoCamptureExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if(_privilegeRequester == null)
            {
                Debug.LogError("Error: VideoCaptureExample._privilegeRequester is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: VideoCaptureExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            // Before enabling the Camera, the scene must wait until the privileges have been granted.
            _privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;

            MLCamera.OnCameraConnected += OnCameraConnected;
            MLCamera.OnCameraDisconnected += OnCameraDisconnected;

            MLCamera.OnCameraCaptureStarted += OnCameraCaptureStarted;
            MLCamera.OnCameraCaptureCompleted +=OnCameraCaptureCompleted;
            #endif

            OnVideoCaptureStarted += _videoCaptureVisualizer.OnCaptureStarted;
            OnVideoCaptureEnded += _videoCaptureVisualizer.OnCaptureEnded;
        }

        #if PLATFORM_LUMIN
        private void OnCameraConnected(MLResult result)
        {
            if (result.IsOk)
            {
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
        }

        private void OnCameraCaptureStarted(MLResult result, string pathName)
        {
            if (result.IsOk)
            {
                _isCapturing = true;
                _captureStartTime = Time.time;
                _captureFilePath = pathName;
                OnVideoCaptureStarted.Invoke();
            }
            else
            {
                Debug.LogErrorFormat("Error: VideoCaptureExample failed to start video capture for {0}. Reason: {1}", pathName, MLCamera.GetErrorCode().ToString());
            }
        }

        private void OnCameraCaptureCompleted(MLResult result)
        {
            if (result.IsOk)
            {
                // If we did not record long enough make sure our path is marked as invalid to avoid trying to load invalid file.
                if (Time.time - _captureStartTime < _minRecordingTime)
                {
                    _captureFilePath = null;
                }

                OnVideoCaptureEnded.Invoke(_captureFilePath);

                _isCapturing = false;
                _captureStartTime = 0;
                _captureFilePath = null;
            }
            else
            {
                Debug.LogErrorFormat("Error: VideoCaptureExample failed to end video capture. Error Code: {0}", MLCamera.GetErrorCode().ToString());
            }
        }
        #endif

        void Update()
        {
           if (_isCapturing)
           {
                // If the recording has gone longer than the max time
                if (Time.time - _captureStartTime > _maxRecordingTime)
                {
                    EndCapture();
                }
            }

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
                if (_isCameraConnected && MLCamera.IsStarted)
                {
                    MLResult result = MLCamera.ApplicationPause(_appPaused);
                    if(!result.IsOk)
                    {
                        Debug.LogErrorFormat("Error: VideoCaptureExample failed to pause MLCamera, disabling script. Reason: {0}", result);
                        enabled = false;
                        return;
                    }

                    // If we did not record long enough make sure our path is marked as invalid to avoid trying to load invalid file.
                    if (Time.time - _captureStartTime < _minRecordingTime)
                    {
                        _captureFilePath = null;
                    }

                    if (_isCapturing)
                    {
                        OnVideoCaptureEnded.Invoke(_captureFilePath);
                    }

                    _isCapturing = false;
                    _captureStartTime = 0;
                    _captureFilePath = null;
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

            OnVideoCaptureStarted -= _videoCaptureVisualizer.OnCaptureStarted;
            OnVideoCaptureEnded -= _videoCaptureVisualizer.OnCaptureEnded;

            #if PLATFORM_LUMIN
            MLCamera.OnCameraCaptureStarted -= OnCameraCaptureStarted;
            MLCamera.OnCameraCaptureCompleted -= OnCameraCaptureCompleted;

            MLCamera.OnCameraConnected -= OnCameraConnected;
            MLCamera.OnCameraDisconnected -= OnCameraDisconnected;
            #endif
        }

        /// <summary>
        /// Start capturing video.
        /// </summary>
        public void StartCapture()
        {
            string fileName = System.DateTime.Now.ToString("MM_dd_yyyy__HH_mm_ss") + _validFileFormat;
            #if PLATFORM_LUMIN
            if(!_isCapturing && _isCameraConnected)
            {
                // Check file fileName extensions
                string extension = System.IO.Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(extension) || !extension.Equals(_validFileFormat, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogErrorFormat("Invalid fileName extension '{0}' passed into Capture({1}).\n" +
                        "Videos must be saved in {2} format.", extension, fileName, _validFileFormat);
                    return;
                }

                string pathName = System.IO.Path.Combine(Application.persistentDataPath, fileName);

                MLCamera.StartVideoCaptureAsync(pathName);
            }
            else
            {
                Debug.LogErrorFormat("Error: VideoCaptureExample failed to start video capture for {0} because '{1}' is already recording!",
                    fileName, _captureFilePath);
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
                Debug.LogError("Error: VideoCaptureExample failed to end video capture because the camera is not recording.");
            }
        }

           /// <summary>
        /// Update Status Tabin Ui.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data </b></color>\nStatus: {0}\n", ControllerStatus.Text);

            _statusText.text += "\n<color=#dbfb76><b>Video Data</b></color>:\n";
            _statusText.text += "Mode: VideoCapture\n";
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
            if (MLCamera.IsStarted)
            {
                if (_isCapturing)
                {
                    EndCapture();
                }
                MLCamera.DisconnectAsync();
            }
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
                Debug.LogErrorFormat("Error: VideoCaptureExample failed to get all requested privileges, disabling script. Reason: {0}", result);
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
                    Debug.LogErrorFormat("Error: VideoCaptureExample failed to resume MLCamera, disabling script. Reason: {0}", result);
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
}
}

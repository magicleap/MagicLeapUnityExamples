// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

// Disabling MLMedia deprecated warning for the internal project
#pragma warning disable 618

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MagicLeap.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class handles video recording and image capturing based on controller
    /// input.
    /// </summary>
    public class CameraCaptureExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text statusText = null;

        [SerializeField, Tooltip("Refrence to the Raw Video Capture Visualizer gameobject for YUV frames")]
        private CameraCaptureVisualizer cameraCaptureVisualizer = null;

        [SerializeField, Tooltip("Reference to media player behavior used in camera capture playback")]
        private MLMediaPlayerBehavior mediaPlayerBehavior;

        [Header("UI Objects")]
        [SerializeField, Tooltip("Canvas Group for the content")]
        private CanvasGroup contentCanvasGroup;

        [SerializeField, Tooltip("Text Displaying Info about Video/Image")]
        private Text captureInfoText;

        [SerializeField, Tooltip("Text Displaying Video FPS")]
        private Text fpsText;

        [SerializeField, Tooltip("Dropdown for selecting camera connection flag.")]
        private EnumDropdown connectionFlagDropdown;

        [SerializeField, Tooltip("Dropdown for selecting desired Stream capabilities.")]
        private Dropdown streamCapabilitiesDropdown;

        [SerializeField, Tooltip("Dropdown for selecting desired output format")]
        private EnumDropdown outputFormatDropDown;

        [SerializeField, Tooltip("Dropdown for selecting desired FrameRate.")]
        private EnumDropdown frameRateDropDown;

        [SerializeField, Tooltip("Dropdown for selecting desired capture type.")]
        private EnumDropdown captureTypeDropDown;

        [SerializeField, Tooltip("Dropdown for selecting desired quality.")]
        private EnumDropdown qualityDropDown;

        [SerializeField, Tooltip("Toggle for enabling recording during capture")]
        private Toggle recordToggle;

        [SerializeField, Tooltip("Button that starts the Capture")]
        private Button captureButton;

        [SerializeField, Tooltip("Button that connects to MLCamera")]
        private Button connectButton;

        [SerializeField, Tooltip("Button that disconnect from MLCamera")]
        private Button disconnectButton;

        private Coroutine recordingRoutine;

        private bool IsCameraConnected => captureCamera != null && captureCamera.ConnectionEstablished;

        private List<MLCamera.StreamCapability> streamCapabilities;

        private MLCamera captureCamera;
        private readonly CameraRecorder cameraRecorder = new CameraRecorder();
        private string recordedFilePath;

        private bool cameraDeviceAvailable;
        private bool isCapturingVideo = false;
        private bool isCapturingPreview = false;
        private bool isDisplayingImage;
        private const string validFileFormat = ".mp4";

        private float fpsRefreshRateTimeSinceLastRefresh;
        private const float fpsRefreshRate = 0.5f; // refresh FPS UI every .5 seconds 

        private MLCamera.ConnectFlag ConnectFlag => connectionFlagDropdown.GetSelected<MLCamera.ConnectFlag>();
        private MLCamera.CaptureType CaptureType => captureTypeDropDown.GetSelected<MLCamera.CaptureType>();
        private MLCamera.MRQuality MRQuality => qualityDropDown.GetSelected<MLCamera.MRQuality>();
        private MLCamera.CaptureFrameRate FrameRate => frameRateDropDown.GetSelected<MLCamera.CaptureFrameRate>();
        private MLCamera.OutputFormat OutputFormat => outputFormatDropDown.GetSelected<MLCamera.OutputFormat>();
        private bool RecordToFile => recordToggle.isOn;
        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        private bool skipFrame = false;

        private List<Button> cameraCaptureButtons;

        private StringBuilder status = new();

        private void Awake()
        {
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
            
            cameraCaptureButtons = new()
            {
                captureButton,
                connectButton, 
                disconnectButton,
            };

            connectionFlagDropdown.AddOptions(
                MLCamera.ConnectFlag.CamOnly,
                MLCamera.ConnectFlag.MR,
                MLCamera.ConnectFlag.VirtualOnly);

            captureButton.onClick.AddListener(OnCaptureButtonClicked);
            connectButton.onClick.AddListener(ConnectCamera);
            disconnectButton.onClick.AddListener(DisconnectCamera);
            connectionFlagDropdown.onValueChanged.AddListener(v => RefreshUI());
            streamCapabilitiesDropdown.onValueChanged.AddListener(v => RefreshUI());
            qualityDropDown.onValueChanged.AddListener(v => RefreshUI());
            captureTypeDropDown.onValueChanged.AddListener(v => RefreshUI());
            frameRateDropDown.onValueChanged.AddListener(v => RefreshUI());

            RefreshUI();
        }

        private void Start()
        {
            MLPermissions.RequestPermission(MLPermission.Camera, permissionCallbacks);
            MLPermissions.RequestPermission(MLPermission.RecordAudio, permissionCallbacks);

            TryEnableMLCamera();
        }

        /// <summary>
        /// Stop the camera, unregister callbacks.
        /// </summary>
        void OnDisable()
        {
            DisconnectCamera();
        }

        /// <summary>
        /// Handle Camera connection if application is paused.
        /// </summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && IsCameraConnected)
            {
                if (recordingRoutine != null)
                {
                    StopCoroutine(recordingRoutine);
                }
                StopVideoCapture();
                DisconnectCamera();
            }
            else
            {
                DisableImageCaptureObject();
                mediaPlayerBehavior.Reset();
                mediaPlayerBehavior.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Display permission error if necessary or update status text.
        /// </summary>
        private void Update()
        {
            UpdateStatusText();
            UpdateFPSText();
        }

        private void OnDestroy()
        {
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
        }

        private void TryEnableMLCamera()
        {
            if (!MLPermissions.CheckPermission(MLPermission.Camera).IsOk)
                return;

            StartCoroutine(EnableMLCamera());
        }

        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        private IEnumerator EnableMLCamera()
        {
            while (!cameraDeviceAvailable)
            {
                MLResult result =
                    MLCamera.GetDeviceAvailabilityStatus(MLCamera.Identifier.Main, out cameraDeviceAvailable);
                if (!(result.IsOk && cameraDeviceAvailable))
                {
                    // Wait until camera device is available
                    yield return new WaitForSeconds(1.0f);
                }
            }

            Debug.Log("Camera device available");
        }

        /// <summary>
        /// Connects to the MLCamera.
        /// </summary>
        private void ConnectCamera()
        {
            MLCamera.ConnectContext context = MLCamera.ConnectContext.Create();
            context.Flags = ConnectFlag;
            context.EnableVideoStabilization = true;

            if (context.Flags != MLCamera.ConnectFlag.CamOnly)
            {
                context.MixedRealityConnectInfo = MLCamera.MRConnectInfo.Create();
                context.MixedRealityConnectInfo.MRQuality = MRQuality;
                context.MixedRealityConnectInfo.MRBlendType = MLCamera.MRBlendType.Additive;
                context.MixedRealityConnectInfo.FrameRate = FrameRate;
            }

            captureCamera = MLCamera.CreateAndConnect(context);

            if (captureCamera != null)
            {
                Debug.Log("Camera device connected");
                captureCamera.OnCameraPaused += CameraPausedHandler;
                captureCamera.OnCameraResumed += CameraResumedHandler;
                if (GetImageStreamCapabilities())
                {
                    Debug.Log("Camera device received stream caps");
                    captureCamera.OnRawVideoFrameAvailable += OnCaptureRawVideoFrameAvailable;
                    captureCamera.OnRawImageAvailable += OnCaptureRawImageComplete;
                }
            }

            RefreshUI();
        }

        private void CameraResumedHandler()
        {
            foreach (var button in cameraCaptureButtons)
            {
                button.interactable = true;
            }
        }

        private void CameraPausedHandler()
        {
            foreach (var button in cameraCaptureButtons)
            {
                button.interactable = false;
            }
        }

        /// <summary>
        /// Disconnects the camera.
        /// </summary>
        private void DisconnectCamera()
        {
            if (captureCamera == null || !IsCameraConnected)
            {
                // Note that some APIs like MLCameraInit() can be called before MLCameraConnect()
                // is called. This is to make sure all is cleaned up if CameraConnect is not called
                MLCamera.Uninitialize();
                return;
            }

            streamCapabilities = null;

            captureCamera.OnRawVideoFrameAvailable -= OnCaptureRawVideoFrameAvailable;
            captureCamera.OnRawImageAvailable -= OnCaptureRawImageComplete;

            captureCamera.OnCameraPaused -= CameraPausedHandler;
            captureCamera.OnCameraResumed -= CameraResumedHandler;

            // media player not supported in Magic Leap App Simulator
#if !UNITY_EDITOR
            mediaPlayerBehavior.MediaPlayer.OnPrepared -= MediaPlayerOnOnPrepared;
            mediaPlayerBehavior.MediaPlayer.OnCompletion -= MediaPlayerOnCompletion;
#endif
            captureCamera.Disconnect();
            RefreshUI();
        }

        /// <summary>
        /// Gets currently selected StreamCapability
        /// </summary>
        private MLCamera.StreamCapability GetStreamCapability()
        {
            if (ConnectFlag != MLCamera.ConnectFlag.CamOnly)
            {
                return streamCapabilities.FirstOrDefault(s => s.CaptureType == CaptureType);
            }

            foreach (var streamCapability in streamCapabilities.Where(s => s.CaptureType == CaptureType))
            {
                if (streamCapabilitiesDropdown.options[streamCapabilitiesDropdown.value].text ==
                    $"{streamCapability.Width}x{streamCapability.Height}")
                {
                    return streamCapability;
                }
            }

            return streamCapabilities[0];
        }

        /// <summary>
        /// Capture Button Clicked.
        /// </summary>
        private void OnCaptureButtonClicked()
        {
            if (isCapturingVideo || isDisplayingImage || isCapturingPreview)
                return;

            if (GetStreamCapability().CaptureType == MLCamera.CaptureType.Image)
            {
                CaptureImage();
                Invoke(nameof(DisableImageCaptureObject), 10);
            }
            else if (GetStreamCapability().CaptureType == MLCamera.CaptureType.Video ||
                     GetStreamCapability().CaptureType == MLCamera.CaptureType.Preview)
            {
                StartVideoCapture();
                recordingRoutine = StartCoroutine(StopVideo());
            }

            RefreshUI();
        }

        private IEnumerator StopVideo()
        {
            float startTimestamp = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTimestamp < 10)
            {
                yield return null;
            }

            StopVideoCapture();
            RefreshUI();
        }

        /// <summary>
        /// Disables Image Rendered.
        /// </summary>
        void DisableImageCaptureObject()
        {
            cameraCaptureVisualizer.HideRenderer();
            isDisplayingImage = false;
            captureInfoText.text = string.Empty;
        }

        private void StartVideoCapture()
        {
            recordedFilePath = string.Empty;
            skipFrame = false;

            var result = MLPermissions.CheckPermission(MLPermission.Camera);
            MLResult.DidNativeCallSucceed(result.Result, nameof(MLPermissions.RequestPermission));

            if (!result.IsOk)
            {
                Debug.LogError($"{MLPermission.Camera} permission denied. Video will not be recorded.");
                return;
            }

            if (RecordToFile)
                StartRecording();
            else
                StartPreview();
        }

        /// <summary>
        /// Captures a preview of the device's camera and displays it in front of the user.
        /// If Record to File is selected then it will not show the preview.
        /// </summary>
        private void StartPreview()
        {
            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig();
            captureConfig.CaptureFrameRate = FrameRate;
            captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
            captureConfig.StreamConfigs[0] =
                MLCamera.CaptureStreamConfig.Create(GetStreamCapability(), OutputFormat);

            MLResult result = captureCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);

            if (MLResult.DidNativeCallSucceed(result.Result, nameof(captureCamera.PrepareCapture)))
            {
                captureCamera.PreCaptureAEAWB();

                if (CaptureType == MLCamera.CaptureType.Video)
                {
                    result = captureCamera.CaptureVideoStart();
                    isCapturingVideo = MLResult.DidNativeCallSucceed(result.Result, nameof(captureCamera.CaptureVideoStart));
                    if (isCapturingVideo)
                    {
                        cameraCaptureVisualizer.DisplayCapture(captureConfig.StreamConfigs[0].OutputFormat, RecordToFile);
                    }
                }

                if (CaptureType == MLCamera.CaptureType.Preview)
                {
                    result = captureCamera.CapturePreviewStart();
                    isCapturingPreview = MLResult.DidNativeCallSucceed(result.Result, nameof(captureCamera.CapturePreviewStart));
                    if (isCapturingPreview)
                    {
                        cameraCaptureVisualizer.DisplayPreviewCapture(captureCamera.PreviewTexture, RecordToFile);
                    }
                }
            }
        }

        /// <summary>
        /// Takes a picture with the device's camera and displays it in front of the user.
        /// </summary>
        private void CaptureImage()
        {
            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig();

            captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS;
            captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
            captureConfig.StreamConfigs[0] =
                MLCamera.CaptureStreamConfig.Create(GetStreamCapability(), OutputFormat);
            MLResult result = captureCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);

            if (!result.IsOk)
                return;

            captureCamera.PreCaptureAEAWB();
            result = captureCamera.CaptureImage(1);

            if (!result.IsOk)
            {
                Debug.LogError("Image capture failed!");
            }
            else
            {
                cameraCaptureVisualizer.DisplayCapture(captureConfig.StreamConfigs[0].OutputFormat, false);
            }
        }

        /// <summary>
        /// Stops the Video Capture.
        /// </summary>
        private void StopVideoCapture()
        {
            if (!isCapturingVideo && !isCapturingPreview)
                return;

            if (isCapturingVideo)
            {
                captureCamera.CaptureVideoStop();
            }
            else if (isCapturingPreview)
            {
                captureCamera.CapturePreviewStop();
            }

            cameraCaptureVisualizer.HideRenderer();

            if (RecordToFile)
            {
                StopRecording();
                DisplayPlayback();
            }

            captureInfoText.text = string.Empty;
            isCapturingVideo = false;
            isCapturingPreview = false;
        }

        /// <summary>
        /// Starts Recording camera capture to file.
        /// </summary>
        private void StartRecording()
        {
            // media player not supported in Magic Leap App Simulator
#if !UNITY_EDITOR
            mediaPlayerBehavior.MediaPlayer.OnPrepared += MediaPlayerOnOnPrepared;
            mediaPlayerBehavior.MediaPlayer.OnCompletion += MediaPlayerOnCompletion;
#endif
            string fileName = DateTime.Now.ToString("MM_dd_yyyy__HH_mm_ss") + validFileFormat;
            recordedFilePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            CameraRecorderConfig config = CameraRecorderConfig.CreateDefault();
            config.Width = GetStreamCapability().Width;
            config.Height = GetStreamCapability().Height;
            config.FrameRate = MapFrameRate(FrameRate);

            cameraRecorder.StartRecording(recordedFilePath, config);

            int MapFrameRate(MLCamera.CaptureFrameRate frameRate)
            {
                switch (frameRate)
                {
                    case MLCamera.CaptureFrameRate.None: return 0;
                    case MLCamera.CaptureFrameRate._15FPS: return 15;
                    case MLCamera.CaptureFrameRate._30FPS: return 30;
                    case MLCamera.CaptureFrameRate._60FPS: return 60;
                    default: return 0;
                }
            }

            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig();
            captureConfig.CaptureFrameRate = FrameRate;
            captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
            captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create(GetStreamCapability(), OutputFormat);
            //MediaRecorder can be null when used through Appsim, so adding a nullcheck
            captureConfig.StreamConfigs[0].Surface = cameraRecorder.MediaRecorder?.InputSurface;

            MLResult result = captureCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);

            if (MLResult.DidNativeCallSucceed(result.Result, nameof(captureCamera.PrepareCapture)))
            {
                captureCamera.PreCaptureAEAWB();

                if (CaptureType == MLCamera.CaptureType.Video)
                {
                    result = captureCamera.CaptureVideoStart();
                    isCapturingVideo = MLResult.DidNativeCallSucceed(result.Result, nameof(captureCamera.CaptureVideoStart));
                    if (isCapturingVideo)
                    {
                        cameraCaptureVisualizer.DisplayCapture(captureConfig.StreamConfigs[0].OutputFormat, RecordToFile);
                    }
                }

                if (CaptureType == MLCamera.CaptureType.Preview)
                {
                    result = captureCamera.CapturePreviewStart();
                    isCapturingPreview = MLResult.DidNativeCallSucceed(result.Result, nameof(captureCamera.CapturePreviewStart));
                    if (isCapturingPreview)
                    {
                        cameraCaptureVisualizer.DisplayPreviewCapture(captureCamera.PreviewTexture, RecordToFile);
                    }
                }
            }
        }

        /// <summary>
        /// Stops recording.
        /// </summary>
        private void StopRecording()
        {
            MLResult result = cameraRecorder.EndRecording();
            if (!result.IsOk)
            {
                Debug.Log("Saving Recording failed, reason:" + result);
                recordedFilePath = string.Empty;
            }
            else
            {
                Debug.Log("Recording saved at path: " + recordedFilePath);
            }
        }

        /// <summary>
        /// Displays recorded video.
        /// </summary>
        private void DisplayPlayback()
        {
            mediaPlayerBehavior.gameObject.SetActive(true);
            mediaPlayerBehavior.source = recordedFilePath;

            // media player not supported in Magic Leap App Simulator
#if !UNITY_EDITOR
            mediaPlayerBehavior.PrepareMLMediaPlayer();
#endif
        }

        private void MediaPlayerOnOnPrepared(MLMedia.Player mediaplayer)
        {
            // media player not supported in Magic Leap App Simulator
#if !UNITY_EDITOR
            mediaPlayerBehavior.Play();
#endif
        }

        private void MediaPlayerOnCompletion(MLMedia.Player mediaplayer)
        {
            // media player not supported in Magic Leap App Simulator
#if !UNITY_EDITOR
            mediaPlayerBehavior.StopMLMediaPlayer();
#endif
            mediaPlayerBehavior.gameObject.SetActive(false);
            mediaPlayerBehavior.Reset();
        }

        /// <summary>
        /// Gets the Image stream capabilities.
        /// </summary>
        /// <returns>True if MLCamera returned at least one stream capability.</returns>
        private bool GetImageStreamCapabilities()
        {
            var result =
                captureCamera.GetStreamCapabilities(out MLCamera.StreamCapabilitiesInfo[] streamCapabilitiesInfo);

            if (!result.IsOk)
            {
                Debug.Log("Could not get Stream capabilities Info.");
                return false;
            }

            streamCapabilities = new List<MLCamera.StreamCapability>();

            for (int i = 0; i < streamCapabilitiesInfo.Length; i++)
            {
                foreach (var streamCap in streamCapabilitiesInfo[i].StreamCapabilities)
                {
                    streamCapabilities.Add(streamCap);
                }
            }

            return streamCapabilities.Count > 0;
        }

        /// <summary>
        /// Handles the event of a new image getting captured.
        /// </summary>
        /// <param name="capturedFrame">Captured Frame.</param>
        /// <param name="resultExtras">Result Extra.</param>
        private void OnCaptureRawVideoFrameAvailable(MLCamera.CameraOutput capturedFrame, MLCamera.ResultExtras resultExtras, MLCamera.Metadata metadataHandle)
        {
            if (string.IsNullOrEmpty(captureInfoText.text) && isCapturingVideo)
            {
                captureInfoText.text = capturedFrame.ToString();
            }

            if (OutputFormat == MLCamera.OutputFormat.RGBA_8888 && FrameRate == MLCamera.CaptureFrameRate._30FPS && GetStreamCapability().Width >= 3840)
            {
                // cameraCaptureVisualizer cannot handle throughput of:
                // 1) RGBA_8888 3840x2160 at 30 fps
                // 2) RGBA_8888 4096x3072 at 30 fps
                skipFrame = !skipFrame;
                if (skipFrame)
                {
                    return;
                }
            }
            cameraCaptureVisualizer.OnCaptureDataReceived(resultExtras, capturedFrame);
        }

        /// <summary>
        /// Handles the event of a new image getting captured.
        /// </summary>
        /// <param name="capturedImage">Captured frame.</param>
        /// <param name="resultExtras">Results Extras.</param>
        private void OnCaptureRawImageComplete(MLCamera.CameraOutput capturedImage, MLCamera.ResultExtras resultExtras, MLCamera.Metadata metadataHandle)
        {
            captureInfoText.text = capturedImage.ToString();

            isDisplayingImage = true;
            cameraCaptureVisualizer.OnCaptureDataReceived(resultExtras, capturedImage);

            if (RecordToFile)
            {
                if (capturedImage.Format != MLCamera.OutputFormat.YUV_420_888)
                {
                    string fileName = DateTime.Now.ToString("MM_dd_yyyy__HH_mm_ss") + ".jpg";
                    recordedFilePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                    try
                    {
                        File.WriteAllBytes(recordedFilePath, capturedImage.Planes[0].Data);
                        captureInfoText.text += $"\nSaved to {recordedFilePath}";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }
            }
        }

        private void OnPermissionDenied(string permission)
        {
            if (permission == MLPermission.Camera)
            {
                MLPluginLog.Error($"{permission} denied, example won't function.");
            }
            else if (permission == MLPermission.RecordAudio)
            {
                MLPluginLog.Error($"{permission} denied, audio wont be recorded in the file.");
            }

            RefreshUI();
        }

        private void OnPermissionGranted(string permission)
        {
            MLPluginLog.Debug($"Granted {permission}.");
            TryEnableMLCamera();

            RefreshUI();
        }

        #region UI functions

        /// <summary>
        /// Updates info about the Video capture.
        /// </summary>
        private void UpdateFPSText()
        {
            fpsText.enabled = !RecordToFile && (isCapturingVideo || isCapturingPreview);

            if (!fpsText.enabled)
                return;

            fpsRefreshRateTimeSinceLastRefresh += Time.deltaTime;
            if (fpsRefreshRateTimeSinceLastRefresh >= fpsRefreshRate)
            {
                fpsText.text = string.Empty;
                fpsText.text += $"FPS: {captureCamera.CurrentFPS:#.#}\n";
                fpsRefreshRateTimeSinceLastRefresh = 0;
            }
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            status.Clear();
            status.AppendLine($"<color=#B7B7B8><b>Controller Data</b></color>\nStatus: {ControllerStatus.Text}");
         
            if (captureCamera is { IsPaused: true })
            {
                status.AppendLine($"Waiting for camera to resume");
            }
            else
            {
                status.AppendLine($"Camera Available: {cameraDeviceAvailable}");
                status.AppendLine($"\nCamera Connected: {IsCameraConnected}");
            }
            if (!isCapturingVideo && !isCapturingPreview && !string.IsNullOrEmpty(recordedFilePath))
            {
                status.AppendLine( $"Recorded video file path:\n {recordedFilePath}");
            }

            statusText.text = status.ToString();
        }

        /// <summary>
        /// Refresh states of all dropdowns and buttons.
        /// </summary>
        private void RefreshUI()
        {
            contentCanvasGroup.interactable = !isCapturingVideo && !isCapturingPreview;

            connectionFlagDropdown.interactable = !IsCameraConnected && !isCapturingVideo && !isCapturingPreview;
            recordToggle.gameObject.SetActive(IsCameraConnected && (CaptureType == MLCamera.CaptureType.Video || OutputFormat == MLCamera.OutputFormat.JPEG) && !Application.isEditor);
            captureButton.gameObject.SetActive(IsCameraConnected);
            connectButton.gameObject.SetActive(!IsCameraConnected);
            disconnectButton.gameObject.SetActive(IsCameraConnected);
            
            SetupCapture();
            RefreshStreamCapabilitiesUI();
            SetupCaptureFormat();
            SetupQuality();
            SetUpFrameRateDropDown();
        }

        private void SetupCapture()
        {
            captureTypeDropDown.transform.parent.gameObject.SetActive(IsCameraConnected || ConnectFlag != MLCamera.ConnectFlag.CamOnly);
            captureTypeDropDown.interactable = !IsCameraConnected || ConnectFlag == MLCamera.ConnectFlag.CamOnly;

            var selectedCaptureType = captureTypeDropDown.GetSelected<MLCamera.CaptureType>();
            captureTypeDropDown.ClearOptions();
            captureTypeDropDown.AddOptions(MLCamera.CaptureType.Image, MLCamera.CaptureType.Video);

            if (ConnectFlag == MLCamera.ConnectFlag.CamOnly)
            {
                captureTypeDropDown.AddOptions(MLCamera.CaptureType.Preview);
            }

            captureTypeDropDown.SelectOption(selectedCaptureType, false);
        }

        /// <summary>
        /// Sets the Quality dropdown
        /// </summary>
        private void SetupQuality()
        {
            if (ConnectFlag == MLCamera.ConnectFlag.CamOnly)
            {
                qualityDropDown.transform.parent.gameObject.SetActive(false);
                return;
            }

            qualityDropDown.transform.parent.gameObject.SetActive(true);
            qualityDropDown.interactable = true;

            if (IsCameraConnected && ConnectFlag != MLCamera.ConnectFlag.CamOnly)
            {
                qualityDropDown.interactable = false;
                return;
            }

            var selectedOption = qualityDropDown.GetSelected<MLCamera.MRQuality>();
            qualityDropDown.ClearOptions();

            qualityDropDown.AddOptions(MLCamera.MRQuality._648x720, MLCamera.MRQuality._960x720, MLCamera.MRQuality._972x1080,
                MLCamera.MRQuality._1440x1080);

            if (FrameRate != MLCamera.CaptureFrameRate._60FPS)
            {
                qualityDropDown.AddOptions(MLCamera.MRQuality._1944x2160, MLCamera.MRQuality._2880x2160);
            }

            qualityDropDown.SelectOption(selectedOption, false);
        }

        /// <summary>
        /// Sets the capture format dropdown.
        /// </summary>
        private void SetupCaptureFormat()
        {
            if (!IsCameraConnected)
            {
                outputFormatDropDown.transform.parent.gameObject.SetActive(false);
                return;
            }

            outputFormatDropDown.transform.parent.gameObject.SetActive(true);

            var selected = outputFormatDropDown.GetSelected<MLCamera.OutputFormat>();
            outputFormatDropDown.ClearOptions();

            if (ConnectFlag == MLCamera.ConnectFlag.CamOnly)
            {
                outputFormatDropDown.AddOptions(MLCamera.OutputFormat.YUV_420_888);
            }

            if (GetStreamCapability().CaptureType == MLCamera.CaptureType.Image)
            {
                outputFormatDropDown.AddOptions(MLCamera.OutputFormat.JPEG);
            }

            if (GetStreamCapability().CaptureType == MLCamera.CaptureType.Video ||
                ConnectFlag != MLCamera.ConnectFlag.CamOnly)
            {
                outputFormatDropDown.AddOptions(MLCamera.OutputFormat.RGBA_8888);
            }

            outputFormatDropDown.SelectOption(selected, false);
        }

        /// <summary>
        /// Sets the frame rate dropdown
        /// </summary>
        private void SetUpFrameRateDropDown()
        {
            if (ConnectFlag == MLCamera.ConnectFlag.CamOnly &&
                (CaptureType == MLCamera.CaptureType.Image || !IsCameraConnected))
            {
                frameRateDropDown.transform.parent.gameObject.SetActive(false);
                return;
            }

            frameRateDropDown.transform.parent.gameObject.SetActive(true);
            frameRateDropDown.interactable = true;

            if (IsCameraConnected && ConnectFlag != MLCamera.ConnectFlag.CamOnly)
            {
                frameRateDropDown.interactable = false;
            }

            var selectedOption = frameRateDropDown.GetSelected<MLCamera.CaptureFrameRate>();
            frameRateDropDown.ClearOptions();

            if (ConnectFlag == MLCamera.ConnectFlag.CamOnly)
            {
                frameRateDropDown.AddOptions(MLCamera.CaptureFrameRate._15FPS);
            }

            frameRateDropDown.AddOptions(MLCamera.CaptureFrameRate._30FPS);

            if (CanDisplay60FPS())
            {
                frameRateDropDown.AddOptions(MLCamera.CaptureFrameRate._60FPS);
            }

            bool CanDisplay60FPS()
            {
                return
                    (IsCameraConnected || ConnectFlag == MLCamera.ConnectFlag.CamOnly || CaptureType != MLCamera.CaptureType.Image) &&
                    (!IsCameraConnected || GetStreamCapability().Height <= 1080) &&
                    (ConnectFlag == MLCamera.ConnectFlag.CamOnly ||
                     (MRQuality != MLCamera.MRQuality._1944x2160 && MRQuality != MLCamera.MRQuality._2880x2160));
            }

            frameRateDropDown.SelectOption(selectedOption, false);
        }

        /// <summary>
        /// Refresh values in stream capabilities dropdown.
        /// </summary>
        private void RefreshStreamCapabilitiesUI()
        {
            streamCapabilitiesDropdown.transform.parent.gameObject.SetActive(IsCameraConnected &&
                                                                             ConnectFlag == MLCamera.ConnectFlag.CamOnly);

            if (!IsCameraConnected)
            {
                return;
            }

            string selectedStream = string.Empty;
            if (streamCapabilitiesDropdown.options.Count > 0)
            {
                selectedStream = streamCapabilitiesDropdown.options[streamCapabilitiesDropdown.value].text;
            }

            streamCapabilitiesDropdown.ClearOptions();

            streamCapabilitiesDropdown.AddOptions(streamCapabilities
                .Where(s => s.CaptureType == CaptureType &&
                            (s.CaptureType != MLCamera.CaptureType.Video ||
                             FrameRate != MLCamera.CaptureFrameRate._60FPS ||
                             s.Height <= 1080)).GroupBy(s => s.Width * 1000 + s.Height)
                .Select(s => s.FirstOrDefault())
                .Select(s => $"{s.Width}x{s.Height}")
                .ToList());

            for (int i = 0; i < streamCapabilitiesDropdown.options.Count; i++)
            {
                if (streamCapabilitiesDropdown.options[i].text == selectedStream)
                {
                    streamCapabilitiesDropdown.SetValueWithoutNotify(i);
                    break;
                }
            }

            // Video encoder only supports upto 3840x2160 (4K)
            // MLMediaRecorder.Prepare will throw unspecified failure when running with higher resolution.
            // MMF-3718 will provide a better error code.
            if (GetStreamCapability().Width > 3840 || !recordToggle.isActiveAndEnabled)
            {
                recordToggle.isOn = false;
                recordToggle.interactable = false;
            }
            else
            {
                recordToggle.interactable = true;
            }
        }

        #endregion
    }
}

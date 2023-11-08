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
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using Object = UnityEngine.Object;

namespace MagicLeap.Examples
{
    /// <summary>
    ///     This class uses a controller to start/stop audio capture
    ///     using the Unity Microphone class. The audio is then played
    ///     through an audio source attached to the parrot in the scene.
    /// </summary>
    public class AudioCaptureExample : MonoBehaviour
    {
        private enum CaptureMode
        {
            Inactive = 0,
            Realtime,
            Interactive
        }

        private const int AudioClipLengthSeconds = 60;
        private const int AudioClipFrequencyHertz = 48000;

        [SerializeField] [Tooltip("The reference to the place from camera script for the parrot.")]
        private PlaceFromCamera placeFromCamera;

        [SerializeField] [Tooltip("The audio source that should replay the captured audio.")]
        private AudioSource playbackAudioSource;

        [SerializeField] [Tooltip("The text to display the recording status.")]
        private Text statusLabel;

        [Space] [Header("Interactive Playback")] [SerializeField] [Range(1, 2)] [Tooltip("The pitch used for delayed audio playback.")]
        private float pitch = 1.5f;

         [SerializeField] [Tooltip("Game object to use for visualizing the root mean square of the microphone audio")]
        private GameObject rmsVisualizer;

        [SerializeField] [Min(0)] [Tooltip("Scale value to set for AmplitudeVisualizer when rms is 0")]
        private float minScale = 0.1f;

        [SerializeField] [Min(0)] [Tooltip("Scale value to set for AmplitudeVisualizer when rms is 1")]
        private float maxScale = 1.0f;

        [SerializeField] private Dropdown captureModeDropdown;
        
        private readonly MLPermissions.Callbacks permissionCallbacks = new();

        private Material rmsVisualizerMaterial;
        private CaptureMode captureMode = CaptureMode.Inactive;
        private MagicLeapInputs.ControllerActions controllerActions;
        
        private bool hasPermission;

        private MLAudioInput.MicCaptureType micCaptureType = MLAudioInput.MicCaptureType.VoiceComm;
        private MLAudioInput.BufferClip mlAudioBufferClip;
        private MLAudioInput.StreamingClip mlAudioStreamingClip;

        private MagicLeapInputs mlInputs;

        private float[] playbackSamples;
        
        private readonly StringBuilder status = new();
        
        private int captureModeLength;
        private bool isCapturingInteractiveAudio;
        private float currentRecordedAudioLength;

        private void Awake()
        {
            bool LogMissingFieldError(Object fieldToCheck, string fieldName)
            {
                if (fieldToCheck != null)
                {
                    return true;
                }
                Debug.LogError($"AudioCaptureExample.{fieldName} is not set, disabling script");
                enabled = false;
                return false;
            }

            if (!(LogMissingFieldError(playbackAudioSource, nameof(playbackAudioSource)) && LogMissingFieldError(statusLabel, nameof(statusLabel)) && LogMissingFieldError(placeFromCamera, nameof(placeFromCamera)) && LogMissingFieldError(rmsVisualizer, nameof(rmsVisualizer))))
            {
                return;
            }
            
            captureModeLength = Enum.GetValues(typeof(CaptureMode)).Length;

            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            MLPermissions.RequestPermission(MLPermission.RecordAudio, permissionCallbacks);

            rmsVisualizerMaterial = rmsVisualizer.GetComponent<Renderer>().material;

            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Bumper.performed += HandleOnBumperDown;
            controllerActions.Trigger.performed += HandleOnTriggerDown;
            controllerActions.TouchpadTouch.performed += HandleTouchpadClick;

            // Frequency = number of samples per second
            // 1000ms => AUDIO_CLIP_FREQUENCY_HERTZ
            // 1ms => AUDIO_CLIP_FREQUENCY_HERTZ / 1000
            // 16ms => AUDIO_CLIP_FREQUENCY_HERTZ * 16 / 1000
            playbackSamples = new float[AudioClipFrequencyHertz * 16 / 1000];

            PopulateDropdowns();
        }
        
        private void Update()
        {
            VisualizeRecording();
            VisualizePlayback();
            UpdateStatus();

            if (captureMode != CaptureMode.Interactive || !isCapturingInteractiveAudio)
            {
                return;
            }
            //if capturing audio then increase the recorded time
            currentRecordedAudioLength += Time.deltaTime;
            if (currentRecordedAudioLength >= AudioClipLengthSeconds)
            {
                StopInteractiveCapture();
            }
        }

        private void OnDestroy()
        {
            StopCapture();
            mlAudioStreamingClip?.Dispose();

            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            controllerActions.Bumper.performed -= HandleOnBumperDown;
            controllerActions.Trigger.performed -= HandleOnTriggerDown;
            controllerActions.TouchpadTouch.performed -= HandleTouchpadClick;

            mlInputs.Dispose();
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                return;
            }
            captureMode = CaptureMode.Inactive;
            StopCapture();
            mlAudioStreamingClip?.Dispose();
            mlAudioStreamingClip = null;
        }

        private void PopulateDropdowns()
        {
            captureModeDropdown.AddOptions(Enum.GetNames(typeof(MLAudioInput.MicCaptureType)).ToList());
            captureModeDropdown.onValueChanged.AddListener(CaptureModeChangedListener);
        }

        private void CaptureModeChangedListener(int micCaptureMode)
        {
            micCaptureType = micCaptureMode switch
            {
                < 2 => (MLAudioInput.MicCaptureType)micCaptureMode,
                _ => MLAudioInput.MicCaptureType.WorldCapture
            };
        }

        private void StartMicrophone()
        {
            if (!MLPermissions.CheckPermission(MLPermission.RecordAudio).IsOk)
            {
                Debug.LogError($"AudioCaptureExample.StartMicrophone() cannot start, {MLPermission.RecordAudio} not granted.");
                return;
            }

            if (captureMode == CaptureMode.Inactive)
            {
                Debug.LogError("AudioCaptureExample.StartMicrophone() cannot start with CaptureMode.Inactive.");
                return;
            }

            playbackAudioSource.Stop();
            if (captureMode != CaptureMode.Realtime)
            {
                return;
            }
            
            if (mlAudioStreamingClip == null)
            {
                mlAudioStreamingClip = new MLAudioInput.StreamingClip(micCaptureType, 3, MLAudioInput.GetSampleRate(micCaptureType));
                playbackAudioSource.pitch = 1;
            }

            playbackAudioSource.clip = mlAudioStreamingClip.UnityAudioClip;
            playbackAudioSource.loop = true;
            playbackAudioSource.Play();
        }
        
        private void VisualizeRecording()
        {
            rmsVisualizerMaterial.color = isCapturingInteractiveAudio ? Color.green : Color.white;
        }

        private void VisualizePlayback()
        {
            if (playbackAudioSource.isPlaying)
            {
                playbackAudioSource.GetOutputData(playbackSamples, 0);

                var squaredSum = playbackSamples.Sum(t => t * t);

                var rootMeanSq = Mathf.Sqrt(squaredSum / playbackSamples.Length);
                var scaleFactor = rootMeanSq * (maxScale - minScale) + minScale;
                rmsVisualizer.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            }
            else
            {
                rmsVisualizer.transform.localScale = new Vector3(minScale, minScale, minScale);
            }
        }

        private void StopCapture()
        {
            isCapturingInteractiveAudio = false;
            currentRecordedAudioLength = 0;

            mlAudioStreamingClip?.ClearBuffer();
            
            mlAudioBufferClip?.Dispose();
            mlAudioBufferClip = null;
            
            // Stop audio playback source and reset settings.
            playbackAudioSource.Stop();
            playbackAudioSource.time = 0;
            playbackAudioSource.pitch = 1;
            playbackAudioSource.loop = false;
            playbackAudioSource.clip = null;
        }

        /// <summary>
        ///     Update the example status label.
        /// </summary>
        private void UpdateStatus()
        {
            status.Clear();
            status.AppendLine($"<color=#B7B7B8><b>Controller Data</b></color>\nStatus: {ControllerStatus.Text}\n");
            status.AppendLine("\n<color=#B7B7B8><b>AudioCapture Data</b></color>\n");
            status.AppendLine($"Status: {captureMode}");
            status.AppendLine($"Mic Capture Mode: {micCaptureType}");
            if (captureMode == CaptureMode.Interactive)
            {
                status.AppendLine($"Interactive Audio Being Recorded: {isCapturingInteractiveAudio}");
                status.AppendLine($"Maximum Clip Length: {AudioClipLengthSeconds}s");
                status.AppendLine($"Current Clip Length: {currentRecordedAudioLength}s");
            }
            
            statusLabel.text = status.ToString();
        }

        private void BeginInteractiveCapture()
        {
            playbackAudioSource.Stop();
            isCapturingInteractiveAudio = true;
            mlAudioBufferClip = new MLAudioInput.BufferClip(micCaptureType, AudioClipLengthSeconds, MLAudioInput.GetSampleRate(micCaptureType));
        }

        private void StopInteractiveCapture()
        {
            currentRecordedAudioLength = 0;
            isCapturingInteractiveAudio = false;
            playbackAudioSource.clip = mlAudioBufferClip.FlushToClip();
            playbackAudioSource.pitch = pitch;
            playbackAudioSource.Play();
            mlAudioBufferClip.Dispose();
            mlAudioBufferClip = null;
        }
        
        /// <summary>
        ///     Responds to permission requester result.
        /// </summary>
        private void OnPermissionDenied(string permission)
        {
            Debug.LogError($"AudioCaptureExample failed to get requested permission {permission}, disabling script.");
            UpdateStatus();
            enabled = false;
        }

        private void OnPermissionGranted(string permission)
        {
            hasPermission = true;
            Debug.Log($"Succeeded in requesting {permission}.");
        }

        private void HandleOnTriggerDown(InputAction.CallbackContext inputCallback)
        {
            if (!hasPermission || EventSystem.current.IsPointerOverGameObject()) return;

            if (!controllerActions.Trigger.WasPressedThisFrame()) return;
            
            captureMode = (CaptureMode)((int)(captureMode + 1) % captureModeLength);
            captureModeDropdown.interactable = captureMode == CaptureMode.Inactive;
            // Stop & Start to clear the previous mode.
            StopCapture();

            if (captureMode != CaptureMode.Inactive) StartMicrophone();
        }

        private void HandleTouchpadClick(InputAction.CallbackContext _)
        {
            if (captureMode != CaptureMode.Interactive)
            {
                return;
            }

            if (isCapturingInteractiveAudio)
            {
                StopInteractiveCapture();
            }
            else
            {
                BeginInteractiveCapture();
            }
        }

        private void HandleOnBumperDown(InputAction.CallbackContext inputCallback)
        {
            StartCoroutine(nameof(SingleFrameUpdate));
        }

        private IEnumerator SingleFrameUpdate()
        {
            placeFromCamera.PlaceOnUpdate = true;
            yield return new WaitForEndOfFrame();
            placeFromCamera.PlaceOnUpdate = false;
        }
    }
}

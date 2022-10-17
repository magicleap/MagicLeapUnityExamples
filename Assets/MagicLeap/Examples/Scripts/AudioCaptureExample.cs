// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class uses a controller to start/stop audio capture
    /// using the Unity Microphone class. The audio is then played
    /// through an audio source attached to the parrot in the scene.
    /// </summary>
    public class AudioCaptureExample : MonoBehaviour
    {
        public enum CaptureMode
        {
            Inactive = 0,
            Realtime,
            Delayed
        }

        [SerializeField, Tooltip("The reference to the place from camera script for the parrot.")]
        private PlaceFromCamera _placeFromCamera = null;

        [SerializeField, Tooltip("The audio source that should replay the captured audio.")]
        private AudioSource _playbackAudioSource = null;

        [SerializeField, Tooltip("The text to display the recording status.")]
        private Text _statusLabel = null;

        [Space]
        [Header("Delayed Playback")]
        [SerializeField, Range(1, 2), Tooltip("The pitch used for delayed audio playback.")]
        private float _pitch = 1.5f;

        [SerializeField, Tooltip("Game object to use for visualizing the root mean square of the microphone audio")]
        private GameObject _rmsVisualizer = null;

        [SerializeField, Min(0), Tooltip("Scale value to set for AmplitudeVisualizer when rms is 0")]
        private float _minScale = 0.1f;

        [SerializeField, Min(0), Tooltip("Scale value to set for AmplitudeVisualizer when rms is 1")]
        private float _maxScale = 1.0f;

        private Material _rmsVisualizerMaterial;

        private bool hasPermission = false;
        private bool isCapturing = false;
        private CaptureMode captureMode = CaptureMode.Inactive;
        private string deviceMicrophone = string.Empty;

        private int detectionAudioPosition = 0;
        private readonly float[] detectionAudioSamples = new float[128];

        private bool isAudioDetected = false;
        private float audioLastDetectionTime = 0;
        private float audioDetectionStart = 0;

        private float[] playbackSamples = null;

        private const int AUDIO_CLIP_LENGTH_SECONDS = 60;
        private const int AUDIO_CLIP_FREQUENCY_HERTZ = 48000;
        private const float AUDIO_SENSITVITY = 0.02f;
        private const float AUDIO_CLIP_TIMEOUT_SECONDS = 2;
        private const float AUDIO_CLIP_FALLOFF_SECONDS = 0.5f;

        private const int NUM_SYNC_ITERATIONS = 30;
        private const int NUM_SAMPLES_LATENCY = 1024;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();
        private MLAudioInput.StreamingClip mlAudioStreamingClip;
        private MLAudioInput.BufferClip mlAudioBufferClip;

        void Awake()
        {
            if (_playbackAudioSource == null)
            {
                Debug.LogError("AudioCaptureExample._playbackAudioSource is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusLabel == null)
            {
                Debug.LogError("AudioCaptureExample._statusLabel is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_placeFromCamera == null)
            {
                Debug.LogError("AudioCaptureExample._placeFromCamera is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_rmsVisualizer == null)
            {
                Debug.LogError("AudioCaptureExample._rmsVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            MLPermissions.RequestPermission(MLPermission.RecordAudio, permissionCallbacks);

            _rmsVisualizerMaterial = _rmsVisualizer.GetComponent<Renderer>().material;

#if UNITY_MAGICLEAP || UNITY_ANDROID
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Bumper.performed += HandleOnBumperDown;
            controllerActions.Trigger.performed += HandleOnTriggerDown;
#endif
            // Frequency = number of samples per second
            // 1000ms => AUDIO_CLIP_FREQUENCY_HERTZ
            // 1ms => AUDIO_CLIP_FREQUENCY_HERTZ / 1000
            // 16ms => AUDIO_CLIP_FREQUENCY_HERTZ * 16 / 1000
            playbackSamples = new float[AUDIO_CLIP_FREQUENCY_HERTZ * 16 / 1000];
        }

        void OnDestroy()
        {
            StopCapture();

            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

#if UNITY_MAGICLEAP || UNITY_ANDROID
            controllerActions.Bumper.performed -= HandleOnBumperDown;
            controllerActions.Trigger.performed -= HandleOnTriggerDown;

            mlInputs.Dispose();
#endif
        }

        private void Update()
        {
            VisualizeRecording();
            VisualizePlayback();

            if (captureMode == CaptureMode.Delayed)
            {
                DetectAudio();
            }

            UpdateStatus();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                captureMode = CaptureMode.Inactive;
                StopCapture();
            }
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

            _playbackAudioSource.Stop();
            var captureType = MLAudioInput.MicCaptureType.VoiceCapture;
            isCapturing = true;

            switch (captureMode)
            {
                case CaptureMode.Realtime:
                    mlAudioStreamingClip = new MLAudioInput.StreamingClip(MLAudioInput.MicCaptureType.VoiceCapture, 3, MLAudioInput.GetSampleRate(captureType));

                    _playbackAudioSource.pitch = 1;
                    _playbackAudioSource.clip = mlAudioStreamingClip.UnityAudioClip;
                    _playbackAudioSource.loop = true;
                    _playbackAudioSource.Play();
                    break;
                case CaptureMode.Delayed:
                    mlAudioBufferClip = new MLAudioInput.BufferClip(MLAudioInput.MicCaptureType.VoiceCapture, AUDIO_CLIP_LENGTH_SECONDS, MLAudioInput.GetSampleRate(captureType));

                    _playbackAudioSource.pitch = _pitch;
                    _playbackAudioSource.clip = null;
                    _playbackAudioSource.loop = false;
                    isAudioDetected = false;
                    audioDetectionStart = 0;
                    detectionAudioPosition = 0;
                    audioLastDetectionTime = 0;
                    break;
            }
        }

        private void VisualizeRecording()
        {
            _rmsVisualizerMaterial.color = (Time.time - audioLastDetectionTime < 0.05f) ? Color.green : Color.white;
        }

        private void VisualizePlayback()
        {
            if (_playbackAudioSource.isPlaying)
            {
                _playbackAudioSource.GetOutputData(playbackSamples, 0);

                float squaredSum = 0;
                for (int i = 0; i < playbackSamples.Length; ++i)
                {
                    squaredSum += playbackSamples[i] * playbackSamples[i];
                }

                float rootMeanSq = Mathf.Sqrt(squaredSum / playbackSamples.Length);
                float scaleFactor = rootMeanSq * (_maxScale - _minScale) + _minScale;
                _rmsVisualizer.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            }
            else
            {
                _rmsVisualizer.transform.localScale = new Vector3(_minScale, _minScale, _minScale);
            }
        }

        private void StopCapture()
        {
            isCapturing = false;

            mlAudioStreamingClip?.Dispose();
            mlAudioStreamingClip = null;
            mlAudioBufferClip?.Dispose();
            mlAudioBufferClip = null;

            // Stop audio playback source and reset settings.
            _playbackAudioSource.Stop();
            _playbackAudioSource.time = 0;
            _playbackAudioSource.pitch = 1;
            _playbackAudioSource.loop = false;
            _playbackAudioSource.clip = null;
        }

        /// <summary>
        /// Update the example status label.
        /// </summary>
        private void UpdateStatus()
        {
            _statusLabel.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);
            _statusLabel.text += "\n<color=#dbfb76><b>AudioCapture Data</b></color>\n";
            _statusLabel.text += string.Format("Status: {0}\n", captureMode.ToString());
        }

        private void DetectAudio()
        {
            // Analyze the input spectrum data, to determine when someone is speaking.
            float maxAudioSample = 0f;

            while (true)
            {
                int readSampleCount = mlAudioBufferClip.GetData(detectionAudioSamples, detectionAudioPosition, out int nextPosition);
                if (readSampleCount == 0)
                {
                    break;
                }
                detectionAudioPosition = nextPosition;
                maxAudioSample = detectionAudioSamples.Take(readSampleCount).Append(maxAudioSample).Max();
            }

            if (maxAudioSample > AUDIO_SENSITVITY)
            {
                audioLastDetectionTime = Time.time;

                if (isAudioDetected == false)
                {
                    isAudioDetected = true;
                    audioDetectionStart = Time.time;
                }
            }
            else if (isAudioDetected && (Time.time > audioLastDetectionTime + AUDIO_CLIP_TIMEOUT_SECONDS))
            {
                var audioDetectionDuration = Time.time - audioDetectionStart;

                _playbackAudioSource.clip = mlAudioBufferClip.FlushToClip();
                _playbackAudioSource.time = _playbackAudioSource.clip.length - audioDetectionDuration;
                _playbackAudioSource.Play();

                // Reset and allow for new captured speech.
                isAudioDetected = false;
                audioDetectionStart = 0;
                detectionAudioPosition = 0;
                audioLastDetectionTime = 0;
            }
        }

        /// <summary>
        /// Responds to permission requester result.
        /// </summary>
        /// <param name="result"/>
        private void OnPermissionDenied(string permission)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            Debug.LogError($"AudioCaptureExample failed to get requested permission {permission}, disabling script.");
            UpdateStatus();
            enabled = false;
            return;
#endif
        }

        private void OnPermissionGranted(string permission)
        {
            hasPermission = true;
            Debug.Log($"Succeeded in requesting {permission}.");
        }

        private void HandleOnTriggerDown(InputAction.CallbackContext inputCallback)
        {
            if (!hasPermission)
            {
                return;
            }

            if (!controllerActions.Trigger.WasPressedThisFrame())
            {
                return;
            }

            captureMode = (captureMode == CaptureMode.Delayed) ? CaptureMode.Inactive : captureMode + 1;

            // Stop & Start to clear the previous mode.
            if (isCapturing)
            {
                StopCapture();
            }

            if (captureMode != CaptureMode.Inactive)
            {
                StartMicrophone();
            }
        }

        private void HandleOnBumperDown(InputAction.CallbackContext inputCallback)
        {
            StartCoroutine(nameof(SingleFrameUpdate));
        }

        private IEnumerator SingleFrameUpdate()
        {
            _placeFromCamera.PlaceOnUpdate = true;
            yield return new WaitForEndOfFrame();
            _placeFromCamera.PlaceOnUpdate = false;
        }
    }
}

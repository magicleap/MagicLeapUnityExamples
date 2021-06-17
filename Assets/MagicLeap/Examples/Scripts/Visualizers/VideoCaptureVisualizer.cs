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

using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class handles visualization of the video and the UI with the status
    /// of the recording.
    /// </summary>
    public class VideoCaptureVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The screen to show the video capture preview")]
        private GameObject _screen = null;
        private Renderer _screenRenderer = null;

        #pragma warning disable 414
        private MLMediaPlayer _mediaPlayer = null;
        #pragma warning restore 414

        [SerializeField, Tooltip("Object that will show up when recording")]
        private GameObject _recordingIndicator = null;

        // time delay between video preparation and enabling screen preview
        private const float SCREEN_PREVIEW_DELAY = 0.6f;

        /// <summary>
        /// Check for all required variables to be initialized.
        /// </summary>
        void Start()
        {
            if(_screen == null)
            {
                Debug.LogError("Error: VideoCaptureVisualizer._screen is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_recordingIndicator == null)
            {
                Debug.LogError("Error: VideoCaptureVisualizer._recordingIndicator is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            _mediaPlayer = _screen.AddComponent<MLMediaPlayer>();
            _mediaPlayer.OnVideoPrepared += HandleVideoPrepared;
            #endif

            _screenRenderer = _screen.GetComponent<Renderer>();
            if(_screenRenderer == null)
            {
                Debug.LogError("Error: VideoCaptureVisualizer._screen does not have a Renderer component, disabling script.");
                enabled = false;
                return;
            }
            _screenRenderer.enabled = false;
        }

        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            if (_mediaPlayer != null)
            {
                _mediaPlayer.OnVideoPrepared -= HandleVideoPrepared;
            }
            #endif
        }

        private IEnumerator EnablePreview()
        {
            // delay is needed for Media Player to load the video after preparing it
            // otherwise, the last frame from the prevous capture will show up
            yield return new WaitForSeconds(SCREEN_PREVIEW_DELAY);
            _screenRenderer.enabled = true;
        }

        /// <summary>
        /// Disables rendering of the video and audio.
        /// </summary>
        public void DisablePreview()
        {
            _screenRenderer.enabled = false;
            _recordingIndicator.SetActive(false);
            #if PLATFORM_LUMIN
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            #endif
        }

        /// <summary>
        /// Handles video capture being started.
        /// </summary>
        public void OnCaptureStarted()
        {
            #if PLATFORM_LUMIN
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            #endif

            // Manage canvas visuals
            _recordingIndicator.SetActive(true);

            // Disable the preview
            _screenRenderer.enabled = false;
        }

        /// <summary>
        /// Handles video capture ending.
        /// </summary>
        /// <param name="path">file path to load captured video to.</param>
        public void OnCaptureEnded(string path)
        {
            // Manage canvas visuals
            _recordingIndicator.SetActive(false);

            #if PLATFORM_LUMIN
            // Only attempt to display video if we have a valid filename.
            if (!string.IsNullOrEmpty(path))
            {
                // Load the captured video
                _mediaPlayer.VideoSource = path;
                MLResult result = _mediaPlayer.PrepareVideo();
                if (!result.IsOk)
                {
                    Debug.LogErrorFormat("Error: VideoCaptureVisualizer failed to prepare video on capture end. Reason: {0}", result);
                }
            }
            #endif
        }

        /// <summary>
        /// Executed when video has successfully loaded
        /// </summary>
        private void HandleVideoPrepared()
        {
            #if PLATFORM_LUMIN
            _mediaPlayer.IsLooping = true;
            #endif

            StartCoroutine(EnablePreview());
        }
    }
}

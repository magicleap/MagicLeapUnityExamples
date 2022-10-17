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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core;

namespace MagicLeap.Examples
{
    public class MediaPlayerControls : MonoBehaviour
    {
        private const float STARTING_VOLUME = 0.75f;

        [SerializeField, Tooltip("Behavior containing a media player reference.")]
        private MLMediaPlayerBehavior mediaPlayerBehavior = null;

        [SerializeField, Tooltip("Pause/Play Button")]
        private MediaPlayerPlayPauseButton pausePlayButton = null;

        [SerializeField, Tooltip("Stop Button")]
        private MediaPlayerStopButton stopButton = null;

        [SerializeField, Tooltip("Rewind Button")]
        private MediaPlayerButton rewindButton = null;
        
        [SerializeField, Tooltip("Number of ms to rewind")]
        private int rewindMS = -10000;

        [SerializeField, Tooltip("Forward Button")]
        private MediaPlayerButton forwardButton = null;
        
        [SerializeField, Tooltip("Number of ms to forward")]
        private int forwardMS = 10000;

        [SerializeField, Tooltip("Timeline Slider")]
        private MediaPlayerTimelineSlider timelineSlider = null;

        [SerializeField, Tooltip("Buffer Bar")]
        private Image bufferBar = null;

        [SerializeField, Tooltip("Volume Slider")]
        private MediaPlayerVolumeSlider volumeSlider = null;

        [SerializeField, Tooltip("Text Mesh for Elapsed Time")]
        private Text elapsedTime = null;

        [SerializeField, Tooltip("Instance of Spinner")]
        private GameObject spinner = null;

        [SerializeField, Tooltip("Text for captions")]
        private Text captionsText = null;

        void Awake()
        {
            if (mediaPlayerBehavior == null)
            {
                Debug.LogError("Error: MLMediaPlayerControls.mediaPlayerBehavior is not set, disabling script.");
                enabled = false;
                return;
            }
        }

        void OnEnable()
        {
            RegisterCallbacks();
        }

        void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void RegisterCallbacks()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            mediaPlayerBehavior.OnPrepared += HandleOnPrepared;
            mediaPlayerBehavior.OnPlay += HandleOnPlay;
            mediaPlayerBehavior.OnPause += HandleOnPause;
            mediaPlayerBehavior.OnCompletion += HandleOnCompletion;
            mediaPlayerBehavior.OnBufferingUpdate += HandleOnBufferingUpdate;
            mediaPlayerBehavior.OnSeekComplete += HandleOnSeekComplete;
            mediaPlayerBehavior.OnCaptionsText += OnCaptionsText;
            mediaPlayerBehavior.OnTrackSelected += OnTrackSelected;
            mediaPlayerBehavior.OnUpdateTimeline += HandleOnTimelineChanged;
            mediaPlayerBehavior.OnUpdateElapsedTime += HandleOnElapsedTimeChanged;
            mediaPlayerBehavior.OnIsBufferingChanged += HandleOnIsBufferingChanged;
            mediaPlayerBehavior.OnReset += HandleOnReset;

            pausePlayButton.OnClicked += PlayPause;
            stopButton.OnButtonClick += Stop;
            stopButton.OnButtonClick += HandleOnStop;
            rewindButton.OnButtonClick += Rewind;
            forwardButton.OnButtonClick += FastForward;
            timelineSlider.OnTimelineChanged += TimelineSliderChange;
            volumeSlider.OnVolumeChanged += SetVolume;
#endif
        }

        private void UnregisterCallbacks()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            mediaPlayerBehavior.OnPrepared -= HandleOnPrepared;
            mediaPlayerBehavior.OnPlay -= HandleOnPlay;
            mediaPlayerBehavior.OnPause -= HandleOnPause;
            mediaPlayerBehavior.OnCompletion -= HandleOnCompletion;
            mediaPlayerBehavior.OnBufferingUpdate -= HandleOnBufferingUpdate;
            mediaPlayerBehavior.OnSeekComplete -= HandleOnSeekComplete;
            mediaPlayerBehavior.OnCaptionsText -= OnCaptionsText;
            mediaPlayerBehavior.OnTrackSelected -= OnTrackSelected;
            mediaPlayerBehavior.OnUpdateTimeline -= HandleOnTimelineChanged;
            mediaPlayerBehavior.OnUpdateElapsedTime -= HandleOnElapsedTimeChanged;
            mediaPlayerBehavior.OnIsBufferingChanged -= HandleOnIsBufferingChanged;
            mediaPlayerBehavior.OnReset -= HandleOnReset;

            pausePlayButton.OnClicked -= PlayPause;
            stopButton.OnButtonClick -= Stop;
            stopButton.OnButtonClick -= HandleOnStop;
            rewindButton.OnButtonClick -= Rewind;
            forwardButton.OnButtonClick -= FastForward;
            timelineSlider.OnTimelineChanged -= TimelineSliderChange;
            volumeSlider.OnVolumeChanged -= SetVolume;
#endif
        }

#if UNITY_MAGICLEAP || UNITY_ANDROID
        
        /// <summary>
        /// Handler when Play/Pause Toggle is triggered.
        /// See HandlePlay() and HandlePause() for more info
        /// </summary>
        private void PlayPause()
        {
            if (mediaPlayerBehavior.IsPlaying)
            {
                mediaPlayerBehavior.Pause();
            }
            else
            {
                mediaPlayerBehavior.Play();
            }
        }

        /// <summary>
        /// Handler when Stop button has been triggered.
        /// </summary>
        private void Stop()
        {
            mediaPlayerBehavior.StopMLMediaPlayer();
        }

        /// <summary>
        /// Handler when Rewind button has been triggered.
        /// Moves the play head backward.
        /// </summary>
        private void Rewind()
        {
            if (!mediaPlayerBehavior.IsPrepared)
                return;

            EnableUI(false);
            // This moves the playhead by an offset in ms
            mediaPlayerBehavior.Seek(rewindMS);
        }

        /// <summary>
        /// Handler when Forward button has been triggered.
        /// Moves the play head forward.
        /// </summary>
        private void FastForward()
        {
            if (!mediaPlayerBehavior.IsPrepared)
                return;

            // Note: this calls the int version of seek.
            // This moves the playhead by an offset in ms
            mediaPlayerBehavior.Seek(forwardMS);
        }

        /// <summary>
        /// Handler when timeline has been changed.
        /// </summary>
        /// <param name="sliderValue">Normalized slider value</param>
        void TimelineSliderChange(float sliderValue)
        {
            if (mediaPlayerBehavior.IsSeeking)
                return;
            
            if (!mediaPlayerBehavior.IsPrepared)
                return;

            float absolutePosition = mediaPlayerBehavior.DurationInMiliseconds * sliderValue;
            EnableUI(false);
            mediaPlayerBehavior.SeekTo(absolutePosition);
        }

        /// <summary>
        /// Enable all UI elements
        /// </summary>
        /// <param name="enabled">True if the UI should be enabled, false if disabled</param>
        private void EnableUI(bool enabled)
        {
            // show the spinner when UI is disabled and vice versa
            spinner.SetActive(!enabled);

            forwardButton.enabled = enabled;
            pausePlayButton.enabled = enabled;
            stopButton.SetEnabled(enabled);
            rewindButton.enabled = enabled;
            timelineSlider.enabled = enabled;
            volumeSlider.enabled = enabled;
            
            if (!enabled)
            {
                elapsedTime.text = "--:--:--";
            }
        }

        /// <summary>
        /// Handler when Volume Sider has changed value.
        /// </summary>
        /// <param name="sliderValue">Normalized slider value</param>
        private void SetVolume(float sliderValue)
        {
            if (!mediaPlayerBehavior.IsPrepared)
                return;

            mediaPlayerBehavior.MediaPlayer.SetVolume(sliderValue);
        }

        /// <summary>
        /// Callback handler on Prepared.
        /// </summary>
        private void HandleOnPrepared()
        {
            // Zero duration means the stream is a live video feed.
            SetVolume(STARTING_VOLUME);
            volumeSlider.SetValueWithoutNotifying(STARTING_VOLUME);

            EnableUI(true);
            stopButton.SetEnabled(true);
        }

        /// <summary>
        /// Callback handler on Play.
        /// </summary>
        private void HandleOnPlay()
        {
            pausePlayButton.SetValue(true);
            stopButton.SetEnabled(true);
        }

        /// <summary>
        /// Callback handler on Pause.
        /// </summary>
        private void HandleOnPause()
        {
            pausePlayButton.SetValue(false);
        }

        /// <summary>
        /// Callback handler on Stop
        /// </summary>
        private void HandleOnStop()
        {
            elapsedTime.text = "--:--:--";
            timelineSlider.SetValueWithoutNotifying(0);
            pausePlayButton.SetValue(false);
            stopButton.SetEnabled(false);
            bufferBar.fillAmount = 0f;
        }

        /// <summary>
        /// Callback handler for Reset
        /// </summary>
        private void HandleOnReset()
        {
            pausePlayButton.SetValue(false);
        }

        /// <summary>
        /// Callback handler on video completion.
        /// </summary>
        private void HandleOnCompletion()
        {
            // Force timeline slider to the end position without triggering a seek.
            timelineSlider.SetValueWithoutNotifying(1f);
            UpdateElapsedTime(mediaPlayerBehavior.MediaPlayer.GetDurationMilliseconds());
        }

        /// <summary>
        /// Callback handler on Buffering update.
        /// </summary>
        private void HandleOnBufferingUpdate(float percent)
        {
            bufferBar.fillAmount = percent * 0.01f;
        }

        /// <summary>
        /// Callback handler on IsBuffering flag changed.
        /// </summary>
        /// <param name="isBuffering"></param>
        private void HandleOnIsBufferingChanged(bool isBuffering)
        {
            EnableUI(!isBuffering);
        }

        /// <summary>
        /// Callback handler on Timeline changed.
        /// </summary>
        /// <param name="position"></param>
        private void HandleOnTimelineChanged(float position)
        {
            timelineSlider.SetValueWithoutNotifying(position);
        }

        /// <summary>
        /// Callback handler on elapsed time changed.
        /// </summary>
        /// <param name="timeInMiliseconds"></param>
        private void HandleOnElapsedTimeChanged(long timeInMiliseconds)
        {
            UpdateElapsedTime(timeInMiliseconds);
        }

        /// <summary>
        /// Callback handler.
        /// </summary>
        private void HandleOnSeekComplete()
        {
            EnableUI(!mediaPlayerBehavior.IsBuffering);
        }

        /// <summary>
        /// Callback handler on track selected.
        /// </summary>
        /// <param name="track"></param>
        void OnTrackSelected(MLMedia.Player.Track track)
        {
            if (track.TrackType == MLMedia.Player.Track.Type.TimedText || track.TrackType == MLMedia.Player.Track.Type.Subtitle)
                captionsText.text = string.Empty;
        }

        /// <summary>
        /// Callback handler on captions text.
        /// </summary>
        /// <param name="text"></param>
        void OnCaptionsText(string text)
        {
            captionsText.text = text;
        }

        /// <summary>
        /// Function to update the elapsed time text
        /// </summary>
        /// <param name="elapsedTimeMs">Elapsed time in milliseconds</param>
        private void UpdateElapsedTime(long elapsedTimeMs)
        {
            TimeSpan timeSpan = new TimeSpan(elapsedTimeMs * TimeSpan.TicksPerMillisecond);
            elapsedTime.text = $"{timeSpan.Hours}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
#endif
    }
}


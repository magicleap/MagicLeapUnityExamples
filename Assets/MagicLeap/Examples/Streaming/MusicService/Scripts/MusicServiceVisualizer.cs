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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using MagicLeap;
using System.IO;
using System;

namespace MagicLeap
{
    /// <summary>
    /// This class handles visualization for the MLMusicService API.
    /// </summary>
    public class MusicServiceVisualizer : MonoBehaviour
    {
        private const float SEEK_EPSILON = 0.001f;
        private uint trackLengthMS = 0;
        private uint trackHeadPositionMS = 0;

        [Header("MLMusicServiceBehavior")]
        [SerializeField, Tooltip("Reference to the MLMusicServiceBehavior in the scene.")]
        private MLMusicServiceBehavior musicService = null;

        [Space, Header("UI Elements")]
        [SerializeField, Tooltip("Play Material")]
        private Material playMaterial = null;
        [SerializeField, Tooltip("Pause Material")]
        private Material pauseMaterial = null;
        [SerializeField, Tooltip("Shuffle On Material")]
        private Material shuffleOnMaterial = null;
        [SerializeField, Tooltip("Shuffle Off Material")]
        private Material shuffleOffMaterial = null;
        [SerializeField, Tooltip("Repeat Off Material")]
        private Material repeatOffMaterial = null;
        [SerializeField, Tooltip("Repeat On Song Material")]
        private Material repeatSongMaterial = null;
        [SerializeField, Tooltip("Repeat On Album Material")]
        private Material repeatAlbumMaterial = null;

        [SerializeField, Tooltip("PlaybackBar reference.")]
        private MediaPlayerSlider playbackBar = null;

        [SerializeField, Tooltip("Volume bar reference.")]
        private MediaPlayerSlider volumeBar = null;

        [SerializeField, Tooltip("Play button reference.")]
        private MediaPlayerToggle playButton = null;

        [SerializeField, Tooltip("Next button reference.")]
        private MediaPlayerButton nextButton = null;

        [SerializeField, Tooltip("Previous button reference.")]
        private MediaPlayerButton prevButton = null;
        [SerializeField, Tooltip("Shuffle button reference.")]
        private MediaPlayerToggle shuffleButton = null;
        [SerializeField, Tooltip("Repeat button reference.")]
        private MediaPlayerButton repeatButton = null;

        [SerializeField, Tooltip("ElapsedTime reference.")]
        private TextMesh elapsedTime = null;

        #pragma warning disable 414
        [SerializeField, Tooltip("Metadata display for the previous track title.")]
        private TextMesh metadataPreviousTrack = null;

        [SerializeField, Tooltip("Metadata display for the next track title.")]
        private TextMesh metadataNextTrack = null;
        #pragma warning restore 414

        /// <summary>
        /// Check required variables and register callbacks.
        /// </summary>
        void Start()
        {
            if (!CheckReferences())
            {
                return;
            }

            playbackBar.OnValueChanged += HandlePlaybackBarChange;
            volumeBar.OnValueChanged += HandleVolumeBarChange;
            playButton.OnToggle += HandlePlayButtonClick;
            prevButton.OnControllerTriggerDown += HandlePreviousButtonClick;
            nextButton.OnControllerTriggerDown += HandleNextButtonClick;
            shuffleButton.OnToggle += HandleShuffleButtonClick;
            repeatButton.OnControllerTriggerDown += HandleRepeatButtonClick;

            musicService.OnRepeatStateChanged += HandleRepeatStateChanged;
            musicService.OnShuffleStateChanged += HandleShuffleStateChanged;

            #if PLATFORM_LUMIN
            musicService.OnMetadataChanged += HandleMetadataChanged;
            #endif

            musicService.OnPlaybackStateChanged += HandlePlaybackStateChanged;
            musicService.OnPositionChanged += HandlePositionChanged;

            #if PLATFORM_LUMIN
            musicService.OnError += HandleError;
            #endif
        }

        /// <summary>
        /// Unregister from the callbacks
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            musicService.OnError -= HandleError;
            #endif

            musicService.OnPositionChanged -= HandlePositionChanged;
            musicService.OnPlaybackStateChanged -= HandlePlaybackStateChanged;

            #if PLATFORM_LUMIN
            musicService.OnMetadataChanged -= HandleMetadataChanged;
            #endif

            musicService.OnShuffleStateChanged -= HandleShuffleStateChanged;
            musicService.OnRepeatStateChanged -= HandleRepeatStateChanged;

            repeatButton.OnControllerTriggerDown -= HandleRepeatButtonClick;
            shuffleButton.OnToggle -= HandleShuffleButtonClick;
            nextButton.OnControllerTriggerDown -= HandleNextButtonClick;
            prevButton.OnControllerTriggerDown -= HandlePreviousButtonClick;
            playButton.OnToggle -= HandlePlayButtonClick;
            volumeBar.OnValueChanged -= HandleVolumeBarChange;
            playbackBar.OnValueChanged -= HandlePlaybackBarChange;
        }

        /// <summary>
        /// Verify that all the necessary references have been set
        /// </summary>
        /// <returns>True if all references are set, false if any are null</returns>
        private bool CheckReferences()
        {
            if (playbackBar == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.playbackBar is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (volumeBar == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.volumeBar is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (nextButton == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.nextButton is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (prevButton == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.prevButton is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (shuffleButton == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.shuffleButton is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (repeatButton == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.repeatButton is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (elapsedTime == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.elapsedTime is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (playMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.playMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (pauseMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.pauseMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (shuffleOnMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.shuffleOnMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (shuffleOffMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.shuffleOffMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (repeatOffMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.repeatOffMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (repeatSongMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.repeatSongMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }
            if (repeatAlbumMaterial == null)
            {
                Debug.LogError("Error: MusicServiceVisualizer.repeatAlbumMaterial is not set, disabling script.");
                enabled = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle the playback bar being moved to a new position.
        /// </summary>
        /// <param name="sliderValue">The new value of the playback bar in range [0, 1]</param>
        private void HandlePlaybackBarChange(float sliderValue)
        {
            uint targetMS = (uint)(trackLengthMS * sliderValue);

            float currentValue = trackHeadPositionMS / (float)trackLengthMS;
            if (Math.Abs(currentValue - sliderValue) < SEEK_EPSILON)
            {
                return;
            }

            musicService.Seek(targetMS);
        }

        /// <summary>
        /// Handle the volume bar being moved to a new position.
        /// </summary>
        /// <param name="sliderValue">The new value of the volume bar in range [0, 1]</param>
        private void HandleVolumeBarChange(float sliderValue)
        {
            musicService.SetVolume(sliderValue);
        }

        /// <summary>
        /// Handles the play/pause button being clicked.
        /// </summary>
        private void HandlePlayButtonClick()
        {
            musicService.PlayPause();
        }

        /// <summary>
        /// Handle the repeat mode button being triggered.
        /// </summary>
        /// <param name="triggerReading">Unused parameter</param>
        private void HandleRepeatButtonClick(float triggerReading)
        {
            switch (musicService.RepeatState)
            {
                case MLMusicService.RepeatStateType.Off:
                {
                    musicService.ChangeRepeatState(MLMusicService.RepeatStateType.Song);
                }
                break;
                case MLMusicService.RepeatStateType.Song:
                {
                    musicService.ChangeRepeatState(MLMusicService.RepeatStateType.Album);
                }
                break;
                default:
                case MLMusicService.RepeatStateType.Album:
                {
                    musicService.ChangeRepeatState(MLMusicService.RepeatStateType.Off);
                }
                break;
            }
        }

        /// <summary>
        /// Handle the previous button being triggered
        /// </summary>
        /// <param name="triggerReading">Unused parameter</param>
        private void HandlePreviousButtonClick(float triggerReading)
        {
            musicService.Previous();
        }

        /// <summary>
        /// Handle the next button being triggered
        /// </summary>
        /// <param name="triggerReading">Unused parameter</param>
        private void HandleNextButtonClick(float triggerReading)
        {
            musicService.Next();
        }

        /// <summary>
        /// Handle the shuffle button being triggered
        /// </summary>
        private void HandleShuffleButtonClick()
        {
            musicService.ToggleShuffle();
        }

        /// <summary>
        /// Set the visual state of the repeat button when the internal shuffle state changes.
        /// </summary>
        /// <param name="state">The new state</param>
        void HandleRepeatStateChanged(MLMusicService.RepeatStateType state)
        {
            switch (musicService.RepeatState)
            {
                case MLMusicService.RepeatStateType.Off:
                {
                    repeatButton.Material = repeatOffMaterial;
                }
                break;
                case MLMusicService.RepeatStateType.Song:
                {
                    repeatButton.Material = repeatSongMaterial;
                }
                break;
                default:
                case MLMusicService.RepeatStateType.Album:
                {
                    repeatButton.Material = repeatAlbumMaterial;
                }
                break;
            }
        }

        /// <summary>
        /// Set the visual state of the shuffle button when the internal shuffle state changes.
        /// </summary>
        /// <param name="state">The new state</param>
        void HandleShuffleStateChanged(MLMusicService.ShuffleStateType state)
        {
            switch (state)
            {
                case MLMusicService.ShuffleStateType.On:
                    shuffleButton.Material = shuffleOnMaterial;
                    break;
                case MLMusicService.ShuffleStateType.Off:
                    shuffleButton.Material = shuffleOffMaterial;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Set the visual state of the play button when the internal playback state changes.
        /// </summary>
        /// <param name="state">The new state</param>
        void HandlePlaybackStateChanged(MLMusicService.PlaybackStateType state)
        {
            if (state == MLMusicService.PlaybackStateType.Playing)
            {
                playButton.Material = pauseMaterial;
            }
            else if (state == MLMusicService.PlaybackStateType.Paused || state == MLMusicService.PlaybackStateType.Stopped)
            {
                playButton.Material = playMaterial;
            }
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Event handler for the metadata of the track being changed
        /// </summary>
        /// <param name="metaData">Metadata of the track</param>
        void HandleMetadataChanged(MLMusicService.Metadata metaData)
        {
            metadataPreviousTrack.text = string.Format("Previous: {0}", musicService.PreviousTrackMetadata.TrackTitle);
            metadataNextTrack.text = string.Format("Next: {0}", musicService.NextTrackMetadata.TrackTitle);

            trackLengthMS = MLMusicService.TrackLength;
        }
        #endif

        /// <summary>
        /// Event handler for the playback head position changing
        /// </summary>
        /// <param name="newPosition">The new position of the playback head</param>
        void HandlePositionChanged(int newPosition)
        {
            if (trackLengthMS == 0)
            {
                // something went wrong
                return;
            }

            trackHeadPositionMS = (uint)newPosition;
            if (playbackBar != null)
            {
                float barValue = (float)newPosition / (float)trackLengthMS;
                playbackBar.Value = barValue;
            }

            if (elapsedTime != null)
            {
                TimeSpan timeSpan = new TimeSpan(newPosition * TimeSpan.TicksPerMillisecond);
                elapsedTime.text = String.Format("{0}:{1}:{2}",
                    timeSpan.Hours.ToString(), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00"));
            }
        }

        #if PLATFORM_LUMIN
        private void HandleError(MLMusicService.Error error)
        {
            enabled = false;
        }
        #endif
    }
}

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

namespace MagicLeap
{
    public class MediaPlayer51SurroundEnabler : MonoBehaviour
    {
        #if PLATFORM_LUMIN

        [SerializeField, Tooltip("GameObject with MLMediaPlayer")]
        private GameObject _screen = null;

        [SerializeField]
        private Transform _frontLeftSpeaker           = null;
        [SerializeField]
        private Transform _frontCenterSpeaker         = null;
        [SerializeField]
        private Transform _frontRightSpeaker          = null;
        [SerializeField]
        private Transform _surroundLeftSpeaker        = null;
        [SerializeField]
        private Transform _surroundRightSpeaker       = null;
        [SerializeField]
        private Transform _lowFrequencyEffectsSpeaker = null;

        [SerializeField]
        private bool _useRecommendedSpeakerPositions = false;

        private MLMediaPlayer _mediaPlayer = null;

        // Start is called before the first frame update
        void Start()
        {
            if (!_screen)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._screen is not set, disabling script.");
                enabled = false;
                return;
            }

            _mediaPlayer = _screen.GetComponent<MLMediaPlayer>();
            if (!_mediaPlayer)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._screen doesn't contain an MLMediaPlayer component.");
                enabled = false;
                return;
            }

            if (!_frontLeftSpeaker)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._frontLeftSpeaker is not set, disabling script.");
                enabled = false;
                return;
            }

            if (!_frontCenterSpeaker)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._frontCenterSpeaker  is not set, disabling script.");
                enabled = false;
                return;
            }

            if (!_frontRightSpeaker)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._frontRightSpeaker  is not set, disabling script.");
                enabled = false;
                return;
            }

            if (!_surroundLeftSpeaker)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._surroundLeftSpeaker  is not set, disabling script.");
                enabled = false;
                return;
            }

            if (!_surroundRightSpeaker)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._surroundRightSpeaker  is not set, disabling script.");
                enabled = false;
                return;
            }

            if (!_lowFrequencyEffectsSpeaker)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler._lowFrequencyEffectsSpeaker  is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_useRecommendedSpeakerPositions)
            {
                _frontLeftSpeaker.position           = MLMediaPlayer.RecommendedSpeakerOffsets[(int)MLMediaPlayer.AudioChannel.FrontLeft];
                _frontCenterSpeaker.position         = MLMediaPlayer.RecommendedSpeakerOffsets[(int)MLMediaPlayer.AudioChannel.FrontCenter];
                _frontRightSpeaker.position          = MLMediaPlayer.RecommendedSpeakerOffsets[(int)MLMediaPlayer.AudioChannel.FrontRight];
                _surroundLeftSpeaker.position        = MLMediaPlayer.RecommendedSpeakerOffsets[(int)MLMediaPlayer.AudioChannel.SurroundLeft];
                _surroundRightSpeaker.position       = MLMediaPlayer.RecommendedSpeakerOffsets[(int)MLMediaPlayer.AudioChannel.SurroundRight];
                _lowFrequencyEffectsSpeaker.position = MLMediaPlayer.RecommendedSpeakerOffsets[(int)MLMediaPlayer.AudioChannel.LowFrequencyEffects];
            }

            _mediaPlayer.OnInfo += HandleInfo;
        }

        /// <summary>
        /// Unsubscribe from all events when component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            _mediaPlayer.OnInfo -= HandleInfo;
        }

        /// <summary>
        /// Event Handler for miscellaneous informational events
        /// </summary>
        /// <param name="info">The event that occurred</param>
        /// <param name="extra">The data associated with the event (if any), otherwise, 0</param>
        private void HandleInfo(MLMediaPlayer.PlayerInfo info, int extra)
        {
            int audioChannelCount;
            switch (info)
            {
                case MLMediaPlayer.PlayerInfo.RenderingStart:
                    _mediaPlayer.GetAudioChannelCount(out audioChannelCount);
                    if (audioChannelCount == 6)
                    {
                        EnableSpatialAudio();
                    }
                    break;
            }
        }

        public void EnableSpatialAudio()
        {
            MLResult result = _mediaPlayer.GetSpatialAudio(out bool isEnabled);
            if (result.IsOk)
            {
                if (!isEnabled)
                {
                    result = _mediaPlayer.SetSpatialAudio(true);
                    if (result.IsOk)
                    {
                        ApplySpeakerPositions();
                    }
                    else
                    {
                        Debug.LogError("Error: MediaPlayer51SurroundEnabler.EnableSpatialAudio failed calling SetSpatialAudio: " + result);
                    }
                }
            }
            else
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.EnableSpatialAudio failed calling GetSpatialAudio: " + result);
            }
        }

        public void ApplySpeakerPositions()
        {
            MLResult result = MLResult.Create(MLResult.Code.Ok);

            result = _mediaPlayer.SetAudioChannelPosition(MLMediaPlayer.AudioChannel.FrontLeft, _frontLeftSpeaker.position);
            if (!result.IsOk)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.ApplySpeakerPositions failed setting front left speaker position: " + result);
            }
            result = _mediaPlayer.SetAudioChannelPosition(MLMediaPlayer.AudioChannel.FrontCenter, _frontCenterSpeaker.position);
            if (!result.IsOk)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.ApplySpeakerPositions failed setting front center speaker position: " + result);
            }
            result = _mediaPlayer.SetAudioChannelPosition(MLMediaPlayer.AudioChannel.FrontRight, _frontRightSpeaker.position);
            if (!result.IsOk)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.ApplySpeakerPositions failed setting front right speaker position: " + result);
            }
            result = _mediaPlayer.SetAudioChannelPosition(MLMediaPlayer.AudioChannel.SurroundLeft, _surroundLeftSpeaker.position);
            if (!result.IsOk)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.ApplySpeakerPositions failed setting surround left speaker position: " + result);
            }
            result = _mediaPlayer.SetAudioChannelPosition(MLMediaPlayer.AudioChannel.SurroundRight, _surroundRightSpeaker.position);
            if (!result.IsOk)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.ApplySpeakerPositions failed setting surround right speaker position: " + result);
            }
            result = _mediaPlayer.SetAudioChannelPosition(MLMediaPlayer.AudioChannel.LowFrequencyEffects, _lowFrequencyEffectsSpeaker.position);
            if (!result.IsOk)
            {
                Debug.LogError("Error: MediaPlayer51SurroundEnabler.ApplySpeakerPositions failed setting low frequency effects speaker position: " + result);
            }
        }

        #endif
    }
}

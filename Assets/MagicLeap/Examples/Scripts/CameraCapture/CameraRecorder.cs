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
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public struct CameraRecorderConfig
    {
        public MLMediaRecorder.AudioSource AudioSource;
        public MLMediaRecorder.OutputFormat OutputFormat;
        public MLMediaRecorder.VideoEncoder VideoEncoder;
        public MLMediaRecorder.AudioEncoder AudioEncoder;

        public int Width;
        public int Height;
        public int FrameRate;
        public int VideoBitrate;
        public int AudioBitrate;
        public int ChannelCount;
        public int SampleRate;

        public static CameraRecorderConfig CreateDefault()
        {
            return new CameraRecorderConfig()
            {
                AudioSource = MLMediaRecorder.AudioSource.Voice,
                OutputFormat = MLMediaRecorder.OutputFormat.MPEG_4,
                VideoEncoder = MLMediaRecorder.VideoEncoder.H264,
                AudioEncoder = MLMediaRecorder.AudioEncoder.AAC,
                Width = 1920,
                Height = 1080,
                FrameRate = 30,
                VideoBitrate = 20000000,
                AudioBitrate = 96000,
                ChannelCount = 1,
                SampleRate = 16000
            };
        }
    }

    public class CameraRecorder
    {
        /// <summary>
        /// MediaRecorder received a general info/warning message.
        /// </summary>
        public event Action<MLMediaRecorder.OnInfoData> OnInfo;

        /// <summary>
        /// MediaRecorder received a general error message.
        /// </summary>
        public event Action<MLMediaRecorder.OnErrorData> OnError;

        /// <summary>
        /// MediaRecorder received a track-related info/warning message.
        /// </summary>
        public event Action<MLMediaRecorder.OnTrackInfoData> OnTrackInfo;

        /// <summary>
        /// MediaRecorder received a track-related error message.
        /// </summary>
        public event Action<MLMediaRecorder.OnTrackErrorData> OnTrackError;

        public MLMediaRecorder MediaRecorder { get; private set; }

        private bool isRecording;

        /// <summary>
        /// Starts recording a Video capture. Should be called before MLCamera.CaptureVideoStart
        /// </summary>
        /// <param name="filePath">Path in which video will be saved</param>
        public MLResult StartRecording(string filePath, CameraRecorderConfig config)
        {
            // This particular feature is not supported in AppSim. This sample uses the ml_media_recorder which is not implemented in AppSim. 
#if UNITY_EDITOR
            return MLResult.Create(MLResult.Code.NotImplemented);
#endif
            
            if (isRecording)
            {
                return MLResult.Create(MLResult.Code.Ok);
            }

            MediaRecorder = MLMediaRecorder.Create();
            MediaRecorder.OnInfo += MediaRecorderOnInfo;
            MediaRecorder.OnError += MediaRecorderOnError;
            MediaRecorder.OnTrackInfo += MediaRecorderOnTrackInfo;
            MediaRecorder.OnTrackError += MediaRecorderOnTrackError;

            MediaRecorder.SetVideoSource(MLMediaRecorder.VideoSource.Camera);
            if (MLPermissions.CheckPermission(MLPermission.RecordAudio).IsOk)
            {
                MediaRecorder.SetAudioSource(config.AudioSource);
            }
            else
            {
                UnityEngine.Debug.LogError($"{MLPermission.RecordAudio} not granted. AudioSource for recording won't be set.");
            }
            MediaRecorder.SetOutputFormat(config.OutputFormat);
            MediaRecorder.SetVideoEncoder(config.VideoEncoder);
            MediaRecorder.SetAudioEncoder(config.AudioEncoder);
            MediaRecorder.SetOutputFileForPath(filePath);

            var mediaFormat = MLMediaFormat.CreateEmpty();
            mediaFormat.SetValue(MLMediaFormatKey.Width, config.Width);
            mediaFormat.SetValue(MLMediaFormatKey.Height, config.Height);
            mediaFormat.SetValue(MLMediaFormatKey.Frame_Rate, config.FrameRate);
            mediaFormat.SetValue(MLMediaFormatKey.Parameter_Video_Bitrate, config.VideoBitrate);
            mediaFormat.SetValue(MLMediaFormatKey.Bit_Rate, config.AudioBitrate);
            mediaFormat.SetValue(MLMediaFormatKey.Channel_Count, config.ChannelCount);
            mediaFormat.SetValue(MLMediaFormatKey.Sample_Rate, config.SampleRate);

            MLResult result = MediaRecorder.Prepare(mediaFormat);

            if (!result.IsOk)
                return result;

            result = MediaRecorder.Start();
            MediaRecorder.GetInputSurface();
            isRecording = true;

            return result;
        }

        /// <summary>
        /// Stops Recording of MLCamera video capture. Should be called after MLCamera.CaptureVideoStop;
        /// </summary>
        public MLResult EndRecording()
        {
            if (!isRecording)
            {
                return MLResult.Create(MLResult.Code.Ok);
            }

            MLResult result = MediaRecorder.Stop();
            isRecording = false;

            MediaRecorder.OnInfo -= MediaRecorderOnInfo;
            MediaRecorder.OnError -= MediaRecorderOnError;
            MediaRecorder.OnTrackInfo -= MediaRecorderOnTrackInfo;
            MediaRecorder.OnTrackError -= MediaRecorderOnTrackError;

            MediaRecorder = null;
            return result;
        }

        private void MediaRecorderOnTrackError(MLMediaRecorder.OnTrackErrorData trackInfo)
        {
            OnTrackError?.Invoke(trackInfo);
        }

        private void MediaRecorderOnTrackInfo(MLMediaRecorder.OnTrackInfoData info)
        {
            OnTrackInfo?.Invoke(info);
        }

        private void MediaRecorderOnInfo(MLMediaRecorder.OnInfoData info)
        {
            OnInfo?.Invoke(info);
        }

        private void MediaRecorderOnError(MLMediaRecorder.OnErrorData error)
        {
            OnError?.Invoke(error);
        }
    }
}

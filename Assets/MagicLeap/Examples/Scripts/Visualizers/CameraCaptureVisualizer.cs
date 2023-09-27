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
using System.IO;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class handles visualization of the video and the UI with the status
    /// of the recording.
    /// </summary>
    public class CameraCaptureVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The renderer to show the camera capture in YUV format")]
        private Renderer _screenRendererYUV = null;

        [SerializeField, Tooltip("The renderer to show the camera capture on RGB format")]
        private Renderer _screenRendererRGB = null;

        [SerializeField, Tooltip("The renderer to show the camera capture on JPEG format")]
        private Renderer _screenRendererJPEG = null;

        [SerializeField, Tooltip("The renderer to show the preview capture of the camera")]
        private Renderer _previewRenderer = null;

        [Header("Visuals")] [SerializeField, Tooltip("Object that will show up when recording")]
        private GameObject _recordingIndicator = null;

#pragma warning disable 414
        private Texture2D[] rawVideoTexturesYUV = new Texture2D[3];
        private Texture2D rawVideoTexturesRGBA;
        private Texture2D imageTexture;
#pragma warning restore 414

        private byte[] yChannelBuffer;
        private byte[] uChannelBuffer;
        private byte[] vChannelBuffer;
        private static readonly string[] samplerNamesYUV = new string[] { "_MainTex", "_UTex", "_VTex" };

        private float currentAspectRatio;

        private bool alreadyCapturedDataThisFrame;

        /// <summary>
        /// Check for all required variables to be initialized.
        /// </summary>
        void Start()
        {
            if (_screenRendererYUV == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._screenRendererYUV is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_screenRendererRGB == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._screenRendererRGB is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_screenRendererJPEG == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._screenRendererJPEG is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_recordingIndicator == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._recordingIndicator is not set, disabling script.");
                enabled = false;
                return;
            }

            _screenRendererYUV.enabled = false;
            _screenRendererRGB.enabled = false;
            _screenRendererJPEG.enabled = false;
        }

        /// <summary>
        /// Handles video capture being started.
        /// </summary>
        public void DisplayCapture(MLCamera.OutputFormat outputFormat, bool isRecording)
        {
            // Manage canvas visuals
            if (!isRecording && outputFormat == MLCamera.OutputFormat.RGBA_8888)
            {
                _screenRendererRGB.enabled = true;
            }
            else if (!isRecording && outputFormat == MLCamera.OutputFormat.YUV_420_888)
            {
                _screenRendererYUV.enabled = true;
            }
            else if (!isRecording && outputFormat == MLCamera.OutputFormat.JPEG)
            {
                _screenRendererJPEG.enabled = true;
            }

            _recordingIndicator.SetActive(isRecording);
        }

        public void DisplayPreviewCapture(RenderTexture texture, bool isRecording)
        {
            if (isRecording)
                return;

            SetProperRatio(texture.width, texture.height, _previewRenderer);
            _previewRenderer.enabled = true;
            _previewRenderer.material.mainTexture = texture;
            _recordingIndicator.SetActive(isRecording);
        }

        /// <summary>
        /// Handles video capture ending.
        /// </summary>
        public void HideRenderer()
        {
            Destroy(rawVideoTexturesYUV[0]);
            rawVideoTexturesYUV[0] = null;
            Destroy(rawVideoTexturesYUV[1]);
            rawVideoTexturesYUV[1] = null;
            Destroy(rawVideoTexturesYUV[2]);
            rawVideoTexturesYUV[2] = null;
            Destroy(rawVideoTexturesRGBA);
            rawVideoTexturesRGBA = null;
            Destroy(imageTexture);
            imageTexture = null;

            _recordingIndicator.SetActive(false);
            _screenRendererYUV.enabled = false;
            _screenRendererRGB.enabled = false;
            _screenRendererJPEG.enabled = false;
            _previewRenderer.enabled = false;
            currentAspectRatio = 0;
        }

        /// <summary>
        /// Display the raw video frame on the texture object.
        /// </summary>
        /// <param name="extras">Unused.</param>
        /// <param name="frameData">Contains raw frame bytes to manipulate.</param>
        /// <param name="frameMetadata">Unused.</param>
        public void OnCaptureDataReceived(MLCamera.ResultExtras extras, MLCamera.CameraOutput frameData)
        {
            if (alreadyCapturedDataThisFrame)
                return;

            if (frameData.Format == MLCamera.OutputFormat.JPEG)
            {
                UpdateJPGTexture(frameData.Planes[0], _screenRendererJPEG);
            }
            else if (frameData.Format == MLCamera.OutputFormat.YUV_420_888)
            {
                MLCamera.FlipFrameVertically(ref frameData);
                SetProperRatio((int)frameData.Planes[0].Width, (int)frameData.Planes[0].Height, _screenRendererYUV);
                UpdateYUVTextureChannel(ref rawVideoTexturesYUV[0], frameData.Planes[0], _screenRendererYUV, samplerNamesYUV[0], ref yChannelBuffer, true);
                UpdateYUVTextureChannel(ref rawVideoTexturesYUV[1], frameData.Planes[1], _screenRendererYUV, samplerNamesYUV[1], ref uChannelBuffer, false);
                UpdateYUVTextureChannel(ref rawVideoTexturesYUV[2], frameData.Planes[2], _screenRendererYUV, samplerNamesYUV[2], ref vChannelBuffer, false);
            }
            else if (frameData.Format == MLCamera.OutputFormat.RGBA_8888)
            {
                UpdateRGBTexture(ref rawVideoTexturesRGBA, frameData.Planes[0], _screenRendererRGB);

                // Flip texture vertically since the image data is reversed
                _screenRendererRGB.material.mainTextureScale = new Vector2(1.0f, -1.0f);
            }

            StartCoroutine(ResetCapturedDataFlagAtEndOfFrame());

            alreadyCapturedDataThisFrame = true;
        }

        private void UpdateJPGTexture(MLCamera.PlaneInfo imagePlane, Renderer renderer)
        {
            if (imageTexture != null)
            {
                Destroy(imageTexture);
            }

            imageTexture = new Texture2D(8, 8);
            bool status = imageTexture.LoadImage(imagePlane.Data);

            if (status && (imageTexture.width != 8 && imageTexture.height != 8))
            {
                SetProperRatio(imageTexture.width, imageTexture.height, _screenRendererJPEG);
                renderer.material.mainTexture = imageTexture;
            }
        }

        private void UpdateYUVTextureChannel(ref Texture2D channelTexture, MLCamera.PlaneInfo imagePlane,
                                             Renderer renderer, string samplerName, ref byte[] newTextureChannel,
                                             bool setTextureScale = false)
        {
            if (channelTexture != null &&
                (channelTexture.width != imagePlane.Width || channelTexture.height != imagePlane.Height))
            {
                Destroy(channelTexture);
                channelTexture = null;
            }

            if (channelTexture == null)
            {
                if (imagePlane.PixelStride == 2)
                {
                    channelTexture = new Texture2D((int)imagePlane.Width, (int)(imagePlane.Height), TextureFormat.RG16, false)
                    {
                        filterMode = FilterMode.Bilinear
                    };
                }
                else
                {
                    channelTexture = new Texture2D((int)imagePlane.Width, (int)(imagePlane.Height), TextureFormat.Alpha8, false)
                    {
                        filterMode = FilterMode.Bilinear
                    };
                }

                Material material = renderer.material;
                material.SetTexture(samplerName, channelTexture);
                if (setTextureScale)
                {
                    material.mainTextureScale = new Vector2(1f / imagePlane.PixelStride, 1.0f);
                }
            }

            int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);
            
            if (imagePlane.Stride != actualWidth)
            {
                if (newTextureChannel == null || newTextureChannel.Length != (actualWidth * imagePlane.Height))
                {
                    newTextureChannel = new byte[actualWidth * imagePlane.Height];
                }
                
                for (int i = 0; i < imagePlane.Height; i++)
                {
                    Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel,
                        i * actualWidth, actualWidth);
                }
                
                channelTexture.LoadRawTextureData(newTextureChannel);
            }
            else
            {
                channelTexture.LoadRawTextureData(imagePlane.Data);
            }

            channelTexture.Apply();
        }

        private void UpdateRGBTexture(ref Texture2D videoTextureRGB, MLCamera.PlaneInfo imagePlane, Renderer renderer)
        {
            int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);
            
            if (videoTextureRGB != null &&
                (videoTextureRGB.width != imagePlane.Width || videoTextureRGB.height != imagePlane.Height))
            {
                Destroy(videoTextureRGB);
                videoTextureRGB = null;
            }

            if (videoTextureRGB == null)
            {
                videoTextureRGB = new Texture2D((int)imagePlane.Width, (int)imagePlane.Height, TextureFormat.RGBA32, false);
                videoTextureRGB.filterMode = FilterMode.Bilinear;

                Material material = renderer.material;
                material.mainTexture = videoTextureRGB;
                material.mainTextureScale = new Vector2(1.0f, 1.0f);
            }

            SetProperRatio((int)imagePlane.Width, (int)imagePlane.Height, _screenRendererRGB);

            if (imagePlane.Stride != actualWidth)
            {
                var newTextureChannel = new byte[actualWidth * imagePlane.Height];
                for(int i = 0; i < imagePlane.Height; i++)
                {
                    Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel, i * actualWidth, actualWidth);
                }
                videoTextureRGB.LoadRawTextureData(newTextureChannel);
            }
            else
            {
                videoTextureRGB.LoadRawTextureData(imagePlane.Data);
            }
            videoTextureRGB.Apply();
        }

        private void SetProperRatio(int textureWidth, int textureHeight, Renderer renderer)
        {
            float ratio = textureWidth / (float)textureHeight;

            if (Math.Abs(currentAspectRatio - ratio) < float.Epsilon)
                return;

            currentAspectRatio = ratio;
            var localScale = renderer.transform.localScale;
            localScale = new Vector3(currentAspectRatio * localScale.y, localScale.y, 1);
            renderer.transform.localScale = localScale;
        }

        private IEnumerator ResetCapturedDataFlagAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            alreadyCapturedDataThisFrame = false;
        }
    }
}

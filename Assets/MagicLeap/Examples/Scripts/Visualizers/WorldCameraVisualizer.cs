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
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{

    public class WorldCameraVisualizer : MonoBehaviour
    {
        [SerializeField]
        private Renderer screenRenderer = null;

        [SerializeField]
        private bool convertDataToGrayScale;

        private Texture2D rawVideoTexture;
        private float currentAspectRatio;
        private byte[] imageData = new byte[0];
        private int numChannels;
        private TextureFormat textureFormat;

        void Start()
        {
            if (screenRenderer == null)
            {
                Debug.LogError("Error: screenRenderer is not set, disabling script.");
                enabled = false;
            }

            if (convertDataToGrayScale)
            {
                numChannels = 3;
                textureFormat = TextureFormat.RGB24;
            }
            else
            {
                numChannels = 1;
                textureFormat = TextureFormat.R8;
            }
        }

        public void RenderFrame(MLWorldCamera.Frame frame)
        {
            UpdateTextureChannel(frame.FrameBuffer, screenRenderer);
        }

        private void UpdateTextureChannel(MLWorldCamera.Frame.Buffer frameBuffer, Renderer renderer)
        {
            if (rawVideoTexture != null &&
                (rawVideoTexture.width != frameBuffer.Width || rawVideoTexture.height != frameBuffer.Height))
            {
                Destroy(rawVideoTexture);
                rawVideoTexture = null;
            }

            if (rawVideoTexture == null)
            {
                rawVideoTexture = new Texture2D((int)frameBuffer.Width, (int)frameBuffer.Height, textureFormat, false);
                rawVideoTexture.filterMode = FilterMode.Bilinear;
                Material material = renderer.material;
                material.mainTexture = rawVideoTexture;
                material.mainTextureScale = new Vector2(1.0f / numChannels, -1.0f / numChannels);
            }

            if (imageData.Length < frameBuffer.DataSize * numChannels)
                imageData = new byte[frameBuffer.DataSize * numChannels];

            System.Runtime.InteropServices.Marshal.Copy(frameBuffer.Data, imageData, 0, frameBuffer.DataSize);

            for (int i = 1; i < numChannels; ++i)
                Buffer.BlockCopy(imageData, 0, imageData, frameBuffer.DataSize * i, frameBuffer.DataSize);

            rawVideoTexture.LoadRawTextureData(imageData);
            rawVideoTexture.Apply();

            SetProperRatio((int)frameBuffer.Width, (int)frameBuffer.Height, screenRenderer);
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
    }
}

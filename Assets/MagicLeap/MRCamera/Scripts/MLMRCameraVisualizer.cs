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

namespace MagicLeap
{
    using System;
    using UnityEngine;
    using UnityEngine.XR.MagicLeap;

    public class MLMRCameraVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The MLMRCameraBehavior to subscribe to.")]
        private MLMRCameraBehavior behavior;

        [SerializeField, Tooltip("The display to show the remote video capture on")]
        private Renderer remoteDisplay = null;

        private Texture2D rawVideoTextureRGB;

        private bool isFittingDimensions = false;

        private void OnEnable()
        {
            if (behavior != null)
            {
                behavior.OnNewImagePlane += RenderNewImagePlane;
            }
        }
        private void OnDisable()
        {
            if (behavior != null)
            {
                behavior.OnNewImagePlane -= RenderNewImagePlane;
            }
        }

        public void RenderNewImagePlane(MLMRCamera.Frame.ImagePlane imagePlane)
        {
            if (!isFittingDimensions)
            {
                float aspectRatio = imagePlane.Width / imagePlane.Height;
                float scaleWidth = transform.lossyScale.z * aspectRatio;

                // sets this gameObject's transform to the aspect ratio of the imagePlane
                if (transform.lossyScale.x != scaleWidth)
                {
                    Transform parent = transform.parent;
                    transform.parent = null;
                    transform.localScale = new Vector3(scaleWidth, transform.localScale.y, transform.localScale.z);
                    transform.parent = parent;
                }
                isFittingDimensions = true;
            }

            int width = (int)(imagePlane.Stride / imagePlane.BytesPerPixel);
            if (rawVideoTextureRGB == null || rawVideoTextureRGB.width != width || rawVideoTextureRGB.height != imagePlane.Height)
            {
                rawVideoTextureRGB = new Texture2D(width, (int)imagePlane.Height, TextureFormat.RGBA32, false);
                rawVideoTextureRGB.filterMode = FilterMode.Bilinear;
                remoteDisplay.material.mainTexture = rawVideoTextureRGB;
                remoteDisplay.material.mainTextureScale = new Vector2(1.0f, -1.0f);
            }

            rawVideoTextureRGB.LoadRawTextureData(imagePlane.Data);
            rawVideoTextureRGB.Apply();
        }
    }

}

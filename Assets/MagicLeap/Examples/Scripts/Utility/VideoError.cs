// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This provides textual state feedback for the connected controller.
    /// </summary>
    public class VideoError : MonoBehaviour
    {
        public Texture2D errorImage;

        private void Awake()
        {
            if (errorImage == null)
            {
                Debug.LogError("Error: VideoError no image found, disabling script.");
                enabled = false;
                return;
            }
        }

        public void ShowError()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer)
            {
                renderer.material.SetTexture("_MainTex", errorImage);
            }
        }
    }
}

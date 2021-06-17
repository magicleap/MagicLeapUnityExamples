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
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.XR.MagicLeap;

using System.Collections;

namespace MagicLeap
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
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
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core.StarterKit;

namespace MagicLeap
{
    /// <summary>
    /// Class outputs to input UI.Text the most up to date gestures
    /// and confidence values for each of the hands.
    /// </summary>
    public class HandTrackingExample : MonoBehaviour
    {
        [SerializeField, Tooltip("Text to display gesture status to.")]
        private Text _statusText = null;

        /// <summary>
        /// Validates fields.
        /// </summary>
        void Awake()
        {
            if (_statusText == null)
            {
                Debug.LogError("Error: HandTrackingExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        ///  Polls the Gestures API for up to date confidence values.
        /// </summary>
        void Update()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n\n", ControllerStatus.Text);

            #if PLATFORM_LUMIN
            _statusText.text += string.Format(
                "<color=#dbfb76><b>Hands Data</b></color>\n<color=#dbfb76>Left</color>: {0}\n{1}% Confidence\n\n<color=#dbfb76>Right</color>: {2}\n{3}% Confidence",
                MLHandTracking.Left.KeyPose.ToString(),
                (MLHandTracking.Left.HandKeyPoseConfidence * 100.0f).ToString("n0"),
                MLHandTracking.Right.KeyPose.ToString(),
                (MLHandTracking.Right.HandKeyPoseConfidence * 100.0f).ToString("n0"));
            #endif
        }
    }
}

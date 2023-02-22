// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// Class outputs to input UI.Text the most up to date gestures
    /// and confidence values for each of the hands. 
    /// Also helps managed what keyposes should be tracked
    /// by the gesture subsystem.
    /// </summary>
    public class HandTrackingExample : MonoBehaviour
    {
        [SerializeField, Tooltip("Text to display gesture status to.")]
        private Text _statusText = null;
        [SerializeField, Tooltip("Default: True. Set to false to not have a pre render Handtracking update. Not recommended for handtracking with visuals as this can affect smoothness.")]
        private bool preRenderHandUpdate = true;

        private InputDevice leftHandDevice;
        private InputDevice rightHandDevice;

        /// <summary>
        /// Validates fields.
        /// </summary>
        void Start()
        {
            if (_statusText == null)
            {
                Debug.LogError("Error: HandTrackingExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            // HAND_TRACKING is a normal permission, so we don't request it at runtime. It is auto-granted if included in the app manifest.
            // If it's missing from the manifest, the permission is not available.
            if (!MLPermissions.CheckPermission(MLPermission.HandTracking).IsOk)
            {
                Debug.LogError($"You must include the {MLPermission.HandTracking} permission in the AndroidManifest.xml to run this example.");
                enabled = false;
                return;
            }

            InputSubsystem.Extensions.MLHandTracking.StartTracking();
            InputSubsystem.Extensions.MLHandTracking.SetPreRenderHandUpdate(preRenderHandUpdate);
        }


        void Update()
        {
            if (!leftHandDevice.isValid || !rightHandDevice.isValid)
            {
                leftHandDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left);
                rightHandDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right);
                return;
            }

            leftHandDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.Confidence, out float leftConfidence);
            leftHandDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool leftIsTracked);

            rightHandDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.Confidence, out float rightConfidence);
            rightHandDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool rightIsTracked);

            _statusText.text = string.Format("<color=#B7B7B8><b>Controller Data</b></color>\nStatus: {0}\n\n", ControllerStatus.Text);

            _statusText.text += string.Format(
                "<color=#B7B7B8><b>Hands Data</b>\n<color=#B7B7B8>Pre-Render Update</color>:</color>{0}\n\n<color=#B7B7B8>Left</color>: {1}% Confidence\n<color=#B7B7B8>IsTracked</color>: {2}\n\n<color=#B7B7B8>Right</color>: {3}% Confidence\n<color=#B7B7B8>IsTracked</color>: {4}",
                preRenderHandUpdate.ToString(), (leftConfidence * 100.0f).ToString("n0"), leftIsTracked.ToString(),
                (rightConfidence * 100.0f).ToString("n0"), rightIsTracked.ToString());
        }


        void OnDestroy()
        {
        }
    }
}

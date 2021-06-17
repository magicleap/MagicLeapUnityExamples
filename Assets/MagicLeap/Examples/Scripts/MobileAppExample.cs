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
using System;

namespace MagicLeap
{
      /// <summary>
    /// This provides visual feedback for the status and keyboard data of the mobile app controller.
    /// </summary>
    public class MobileAppExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The reference to the MLControllerConnectionHandlerBehavior in the scene.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("The mobile app visualizer to use for displaying keyboard text.")]
        private MobileAppControllerVisualizer _mobileAppVisualizer = null;

        [SerializeField, Tooltip("The status text that will display input.")]
        private Text _statusText = null;

        void Awake()
        {
            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: MobileAppExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_mobileAppVisualizer == null)
            {
                Debug.LogError("Error: MobileAppExample._mobileAppVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: MobileAppExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }
        }

        void Update()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);

            MLInput.Controller controller = _controllerConnectionHandler.ConnectedController;
            if(controller != null)
            {
                #if PLATFORM_LUMIN
                _statusText.text += string.Format("" +
                    "Position: <i>{0}</i>\n" +
                    "Rotation: <i>{1}</i>\n\n" +
                    "<color=#dbfb76><b>Buttons</b></color>\n" +
                    "Trigger: <i>{2}</i>\n" +
                    "Bumper: <i>{3}</i>\n\n" +
                    "<color=#dbfb76><b>Touchpad</b></color>\n" +
                    "Touch 1 Location: <i>({4},{5})</i>\n" +
                    "Touch 2 Location: <i>({6},{7})</i>\n\n" +
                    "<color=#dbfb76><b>Gestures</b></color>\n" +
                    "<i>{8} {9}</i>\n\n",

                   "No information available",
                   controller.Orientation.eulerAngles.ToString("n2"),
                   controller.TriggerValue.ToString("n2"),
                   controller.IsBumperDown,
                   controller.Touch1Active ? controller.Touch1PosAndForce.x.ToString("n2") : "0.00",
                   controller.Touch1Active ? controller.Touch1PosAndForce.y.ToString("n2") : "0.00",
                   controller.Touch2Active ? controller.Touch2PosAndForce.x.ToString("n2") : "0.00",
                   controller.Touch2Active ? controller.Touch2PosAndForce.y.ToString("n2") : "0.00",
                   controller.CurrentTouchpadGesture.Type.ToString(),
                   controller.TouchpadGestureState.ToString());
                #endif

                _statusText.text += string.Format("<color=#dbfb76><b>Keyboard</b></color>\nInput: {0}",
                    _mobileAppVisualizer.KeyboardText);
            }
        }
    }
}

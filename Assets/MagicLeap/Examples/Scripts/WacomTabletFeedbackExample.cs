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

namespace MagicLeap
{
    /// <summary>
    /// This provides visual feedback for the information about the wacom tablet's last known state of the device, pen, and buttons.
    /// </summary>
    public class WacomTabletFeedbackExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The wacom tablet visualizer to use for displaying pen and button data.")]
        private WacomTabletVisualizer _wacomTabletVisualizer = null;

        [SerializeField, Tooltip("The component that adjusts the placement of the wacom tablet.")]
        private PlaceFromCamera _wacomTabletPlacement = null;

        [SerializeField, Tooltip("The controller reference to use.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("UI text for the MLInput tablet API values.")]
        private Text _statusText = null;

        /// <summary>
        /// Validates fields and subscribes to input event.
        /// </summary>
        void Start()
        {
            if (_wacomTabletVisualizer == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._wacomTabletVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_wacomTabletPlacement == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._wacomTabletPlacement is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            #endif
        }

        /// <summary>
        /// Unregisters from input event.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            #endif
        }

        /// <summary>
        /// Updates the UI text.
        /// </summary>
        void Update()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Tablet Data</b></color>\nStatus: {0}\n\n",
            _wacomTabletVisualizer.Connected ? "Connected" : "Disconnected");

            if (_wacomTabletVisualizer.Connected)
            {
                _statusText.text += string.Format(
                "<b><color=#dbfb76>{0}</color></b>\n" +
                "\t{1}:\t\t({2}, {3})\n" +
                "\t{4}:\t\t{5}\n" +
                "\t{6}:\t\t{7}\n" +
                "\t{8}:\t\t\t\t\t({9}, {10})\n" +
                "\t{11}:\t\t{12}\n" +
                "\t{13}:\t{14}\n" +
                "\n" +
                "<b><color=#dbfb76>{15}</color></b>\n" +
                "\t{16}: {17}\n" +
                "\t{18}:\t\t\t{19} - {20}",
                "Pen",
                "Location",
                _wacomTabletVisualizer.LastPositionAndForce.x,
                _wacomTabletVisualizer.LastPositionAndForce.y,
                "Pressure",
                _wacomTabletVisualizer.LastPositionAndForce.z,
                "Distance",
                _wacomTabletVisualizer.LastDistance,
                "Tilt",
                _wacomTabletVisualizer.LastTilt.x,
                _wacomTabletVisualizer.LastTilt.y,
                "Touching",
                _wacomTabletVisualizer.LastIsTouching,
                "Tool Type",
                #if PLATFORM_LUMIN
                (_wacomTabletVisualizer.ButtonErase) ? "Button - Eraser" : _wacomTabletVisualizer.LastToolType.ToString(),
                #else
                string.Empty,
                #endif
                "Events",
                "Touch Ring",
                _wacomTabletVisualizer.LastTouchRing,
                "Button",
                #if PLATFORM_LUMIN
                _wacomTabletVisualizer.LastButton,
                #else
                string.Empty,
                #endif
                _wacomTabletVisualizer.LastButtonState);
            }
        }

        /// <summary>
        /// Handles the event for button down.
        /// Toggles if the wacom tablet should update it's placement to the user position.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void HandleOnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId) && button == MLInput.Controller.Button.HomeTap)
            {
                _wacomTabletPlacement.PlaceOnUpdate = !_wacomTabletPlacement.PlaceOnUpdate;
            }
        }
    }
}

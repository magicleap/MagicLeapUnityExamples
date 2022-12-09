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
using UnityEngine.InputSystem;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class provides the current status of the controller
    /// exposed in text format, with an associated color value.
    /// Red: MLInput error.
    /// Green: Controller connected.
    /// Yellow: Controller disconnected.
    /// </summary>
    public class ControllerStatus : MonoBehaviour
    {
        private static ControllerStatus _instance = null;
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        private string _text = "Unknown";
        private Color _color = Color.red;


        /// <summary>
        /// Returns the status text of the controller.
        /// </summary>
        public static string Text
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError(
                        "Error: ControllerStatus._instance is not set, this component must be included in your scene.");

                    return string.Empty;
                }

                return _instance._text;
            }

            private set { _instance._text = value; }
        }

        /// <summary>
        /// Returns the status color of the controller.
        /// </summary>
        public static Color Color
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError(
                        "Error: ControllerStatus._instance is not set, this component must be included in your scene.");

                    return Color.red;
                }

                return _instance._color;
            }

            private set { _instance._color = value; }
        }

        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _instance = this;
        }

        void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.IsTracked.performed += HandleOnControllerChanged;
            controllerActions.IsTracked.canceled += HandleOnControllerChanged;

            // Wait until the next cycle to check the status.
            UpdateStatus();
        }

        void OnDestroy()
        {
            controllerActions.IsTracked.performed -= HandleOnControllerChanged;
            controllerActions.IsTracked.canceled -= HandleOnControllerChanged;

            mlInputs.Dispose();
        }

        /// <summary>
        /// Update the text for the currently connected Control or MCA device.
        /// </summary>
        private void UpdateStatus()
        {
            if (controllerActions.IsTracked.WasPerformedThisFrame())
            {
                Text = "Controller Connected";
                Color = Color.green;
            }
            else
            {
                Text = "Disconnected";
                Color = Color.yellow;
            }
        }

        private void HandleOnControllerChanged(InputAction.CallbackContext callbackContext)
        {
            UpdateStatus();
        }
    }
}

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
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class provides the current status of the controller
    /// exposed in text format, with an associated color value.
    /// Red: MLInput error.
    /// Green: Controller connected.
    /// Yellow: Controller disconnected.
    /// </summary>
    [RequireComponent(typeof(MLControllerConnectionHandlerBehavior))]
    public class ControllerStatus : MonoBehaviour
    {
        private static ControllerStatus _instance = null;
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        private string _text = "Unknown";
        private Color _color = Color.red;

        /// <summary>
        /// Returns the status text of the controller.
        /// </summary>
        public static string Text
        {
            get
            {
                if(_instance == null)
                {
                    Debug.LogError("Error: ControllerStatus._instance is not set, this component must be included in your scene.");

                    return string.Empty;
                }

                return _instance._text;
            }

            private set
            {
                _instance._text = value;
            }
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
                    Debug.LogError("Error: ControllerStatus._instance is not set, this component must be included in your scene.");

                    return Color.red;
                }

                return _instance._color;
            }

            private set
            {
                _instance._color = value;
            }
        }

        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _instance = this;

            _controllerConnectionHandler = GetComponent<MLControllerConnectionHandlerBehavior>();

            _controllerConnectionHandler.OnControllerConnected += HandleOnControllerChanged;
            _controllerConnectionHandler.OnControllerDisconnected += HandleOnControllerChanged;
        }

        void Start()
        {
            // Wait until the next cycle to check the status.
            UpdateStatus();
        }

        void OnDestroy()
        {
            _controllerConnectionHandler.OnControllerConnected -= HandleOnControllerChanged;
            _controllerConnectionHandler.OnControllerDisconnected -= HandleOnControllerChanged;
        }

        void OnApplicationPause(bool pause)
        {
            if(!pause)
            {
                UpdateStatus();
            }
        }

        /// <summary>
        /// Update the text for the currently connected Control or MCA device.
        /// </summary>
        private void UpdateStatus()
        {
            if (_controllerConnectionHandler.enabled)
            {
                if (_controllerConnectionHandler.IsControllerValid())
                {
                    #if PLATFORM_LUMIN
                    MLInput.Controller controller = _controllerConnectionHandler.ConnectedController;
                    if (controller.Type == MLInput.Controller.ControlType.Control)
                    {
                        Text = "Controller Connected";
                        Color = Color.green;
                    }
                    else if (controller.Type == MLInput.Controller.ControlType.MobileApp)
                    {
                        Text = "MLA Connected";
                        Color = Color.green;
                    }
                    else
                    {
                        Text = "Unknown";
                        Color = Color.red;
                    }
                    #else
                    Text = "Unknown";
                    Color = Color.red;
                    #endif
                }
                else
                {
                    Text = "Disconnected";
                    Color = Color.yellow;
                }
            }
            else
            {
                Text = "Input Failed to Start";
                Color = Color.red;
            }
        }

        private void HandleOnControllerChanged(byte controllerId)
        {
            UpdateStatus();
        }
    }
}

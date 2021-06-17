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
    /// This represents the controller text connectivity status.
    /// Red: MLInput error.
    /// Green: Controller connected.
    /// Yellow: Controller disconnected.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ControllerStatusText : MonoBehaviour
    {
        [SerializeField, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        private Text _controllerStatusText = null;

        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _controllerStatusText = gameObject.GetComponent<Text>();

            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: ControllerStatusText._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

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
                        _controllerStatusText.text = "Controller Connected";
                        _controllerStatusText.color = Color.green;
                    }
                    else if (controller.Type == MLInput.Controller.ControlType.MobileApp)
                    {
                        _controllerStatusText.text = "MLA Connected";
                        _controllerStatusText.color = Color.green;
                    }
                    else
                    {
                        _controllerStatusText.text = "Unknown";
                        _controllerStatusText.color = Color.red;
                    }
                    #else
                    _controllerStatusText.text = "Unknown";
                    _controllerStatusText.color = Color.red;
                    #endif
                }
                else
                {
                    _controllerStatusText.text = "Disconnected";
                    _controllerStatusText.color = Color.yellow;
                }
            }
            else
            {
                _controllerStatusText.text = "Input Failed to Start";
                _controllerStatusText.color = Color.red;
            }
        }

        private void HandleOnControllerChanged(byte controllerId)
        {
            UpdateStatus();
        }
    }
}

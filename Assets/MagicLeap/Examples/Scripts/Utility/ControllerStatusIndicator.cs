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
    /// This represents the controller sprite icon connectivity status.
    /// Red: MLInput error.
    /// Green: Controller connected.
    /// Yellow: Controller disconnected.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ControllerStatusIndicator : MonoBehaviour
    {
        [SerializeField, Tooltip("Controller Icon")]
        private Sprite _controllerIcon = null;

        [SerializeField, Tooltip("Mobile App Icon")]
        private Sprite _mobileAppIcon = null;

        [SerializeField, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

            if (_controllerIcon == null)
            {
                Debug.LogError("Error: ControllerStatusIndicator._controllerIcon is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_mobileAppIcon == null)
            {
                Debug.LogError("Error: ControllerStatusIndicator._mobileAppIcon is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: ControllerStatusIndicator._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            _controllerConnectionHandler.OnControllerConnected += HandleOnControllerChanged;
            _controllerConnectionHandler.OnControllerDisconnected += HandleOnControllerChanged;
        }

        void Start()
        {
            SetDefaultIcon();

            UpdateColor();
            UpdateIcon();
        }

        void OnDestroy()
        {
            _controllerConnectionHandler.OnControllerConnected -= HandleOnControllerChanged;
            _controllerConnectionHandler.OnControllerDisconnected -= HandleOnControllerChanged;
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                UpdateColor();
                UpdateIcon();
            }
        }

        /// <summary>
        /// Update the color depending on the controller connection.
        /// </summary>
        private void UpdateColor()
        {
            if (_controllerConnectionHandler.enabled)
            {
                if (_controllerConnectionHandler.IsControllerValid())
                {
                    _spriteRenderer.color = Color.green;
                }
                else
                {
                    _spriteRenderer.color = Color.yellow;
                }
            }
            else
            {
                _spriteRenderer.color = Color.red;
            }
        }

        /// <summary>
        /// Update Icon to show type of connected icon or device allowed.
        /// </summary>
        private void UpdateIcon()
        {
            #if PLATFORM_LUMIN
            if (_controllerConnectionHandler.enabled &&
                _controllerConnectionHandler.IsControllerValid())
            {
                switch (_controllerConnectionHandler.ConnectedController.Type)
                {
                    case MLInput.Controller.ControlType.Control:
                        {
                            _spriteRenderer.sprite = _controllerIcon;
                            break;
                        }
                    case MLInput.Controller.ControlType.MobileApp:
                        {
                            _spriteRenderer.sprite = _mobileAppIcon;
                            break;
                        }
                }
            }
            #endif
        }

        /// <summary>
        /// This will set the default icon used to represent the controller.
        /// When the device controller is excluded, MobileApp will be used instead.
        /// </summary>
        private void SetDefaultIcon()
        {
            #if PLATFORM_LUMIN
            if ((_controllerConnectionHandler.DevicesAllowed & MLControllerConnectionHandlerBehavior.DeviceTypesAllowed.ControllerLeft) != 0 ||
                (_controllerConnectionHandler.DevicesAllowed & MLControllerConnectionHandlerBehavior.DeviceTypesAllowed.ControllerRight) != 0)
            {
                _spriteRenderer.sprite = _controllerIcon;
            }
            else
            {
                _spriteRenderer.sprite = _mobileAppIcon;
            }
            #endif
        }

        private void HandleOnControllerChanged(byte controllerId)
        {
            UpdateColor();
            UpdateIcon();
        }
    }
}

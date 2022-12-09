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

        private SpriteRenderer _spriteRenderer;
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

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
        }

        void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.IsTracked.performed += HandleOnControllerChanged;
            controllerActions.IsTracked.canceled += HandleOnControllerChanged;

            SetDefaultIcon();

            UpdateColor();
            UpdateIcon();
        }

        void OnDestroy()
        {
            controllerActions.IsTracked.performed -= HandleOnControllerChanged;
            controllerActions.IsTracked.canceled -= HandleOnControllerChanged;

            mlInputs.Dispose();
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
            if (controllerActions.IsTracked.IsPressed())
            {
                _spriteRenderer.color = Color.green;
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
            if (controllerActions.IsTracked.IsPressed())
            {
                _spriteRenderer.sprite = _controllerIcon;
            }
        }

        /// <summary>
        /// This will set the default icon used to represent the controller.
        /// When the device controller is excluded, MobileApp will be used instead.
        /// </summary>
        private void SetDefaultIcon()
        {
            _spriteRenderer.sprite = _controllerIcon;
        }

        private void HandleOnControllerChanged(InputAction.CallbackContext callbackContext)
        {
            UpdateColor();
            UpdateIcon();
        }
    }
}

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
    /// This class provides the functionality for the object's transform assigned
    /// to this script to match the 6dof data from input when using a Control
    /// and the 3dof data when using the Mobile App.
    /// </summary>
    public class ControllerTransform : MonoBehaviour
    {
        private Camera _camera;

#pragma warning disable 414
        // MobileApp-specific variables
        private bool _isCalibrated = false;
#pragma warning restore 414

        private Quaternion _calibrationOrientation = Quaternion.identity;
        private const float MOBILEAPP_FORWARD_DISTANCE_FROM_CAMERA = 0.75f;
        private const float MOBILEAPP_UP_DISTANCE_FROM_CAMERA = -0.1f;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        /// <summary>
        /// Initialize variables, callbacks and check null references.
        /// </summary>
        void Start()
        {
            _camera = Camera.main;

            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Menu.canceled += HandleOnHomeUp;
        }

        /// <summary>
        /// Update controller input based feedback.
        /// </summary>
        void Update()
        {
            if (controllerActions.IsTracked.IsPressed())
            {
                // For Control, raw input is enough
                transform.localPosition = controllerActions.Position.ReadValue<Vector3>();
                transform.localRotation = controllerActions.Position.ReadValue<Quaternion>();
            }
        }

        private void OnDestroy()
        {
            controllerActions.Menu.canceled -= HandleOnHomeUp;

            mlInputs.Dispose();
        }

        /// <summary>
        /// For Mobile App, this initiates/ends the recalibration when the home tap event is triggered
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnHomeUp(InputAction.CallbackContext callbackContext)
        {
            if (!_isCalibrated)
            {
                _calibrationOrientation = transform.rotation * Quaternion.Inverse(controllerActions.Rotation.ReadValue<Quaternion>());
            }

            _isCalibrated = !_isCalibrated;
        }
    }
}

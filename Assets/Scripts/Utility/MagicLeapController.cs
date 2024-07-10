// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace MagicLeap.Examples
{
    /// <summary>
    /// Convenient utility for accessing InputActions bound to the ML2 controller's OpenXR action paths
    /// </summary>
    public class MagicLeapController
    {
        private static MagicLeapController instance;

        /// <summary>
        /// Singleton instance of the ML2 Controller action map bindings
        /// </summary>
        public static MagicLeapController Instance
        {
            get
            {
                instance ??= new MagicLeapController();
                return instance;
            }
        }

        private readonly InputActionAsset inputActionsAsset;
        private readonly InputActionMap inputMap;
        private readonly InputAction triggerAction, triggerValueAction, trackpadClickAction, trackpadForceAction, bumperAction, menuButtonAction, trackpadAction, positionAction,
                             rotationAction, trackingStatusAction, isTrackedAction, pointerPositionAction, pointerRotationAction, velocityAction, angularVelocityAction;

        private MagicLeapController()
        {
            var manager = UnityEngine.Object.FindObjectOfType<InputActionManager>();
            if (manager == null)
                throw new System.NullReferenceException("Could not find an InputActionManager to initialize a MagicLeapController from");

            inputActionsAsset = manager.actionAssets[0];
            if (inputActionsAsset == null)
                throw new System.NullReferenceException("Could not find an InputActionAsset");

            inputActionsAsset.Enable();

            inputMap = inputActionsAsset.FindActionMap("Controller");

            triggerAction = inputMap.FindAction("Trigger");
            bumperAction = inputMap.FindAction("Bumper");
            menuButtonAction = inputMap.FindAction("MenuButton");
            trackpadClickAction = inputMap.FindAction("TrackpadClick");
            isTrackedAction = inputMap.FindAction("IsTracked");
            positionAction = inputMap.FindAction("Position");
            rotationAction = inputMap.FindAction("Rotation");
            pointerPositionAction = inputMap.FindAction("PointerPosition");
            pointerRotationAction = inputMap.FindAction("PointerRotation");
            velocityAction = inputMap.FindAction("Velocity");
            angularVelocityAction = inputMap.FindAction("AngularVelocity");
            trackingStatusAction = inputMap.FindAction("Status");
            triggerAction = inputMap.FindAction("Trigger");
            triggerValueAction = inputMap.FindAction("TriggerValue");
            trackpadAction = inputMap.FindAction("Trackpad");
            trackpadForceAction ??= inputMap.FindAction("TrackpadForce");
        }

        ~MagicLeapController()
        {
            if (inputActionsAsset != null && instance == this)
                inputActionsAsset.Disable();
        }

        /// <summary>
        /// Trigger press event
        /// </summary>
        public event Action<InputAction.CallbackContext> TriggerPressed
        {
            add => triggerAction.performed += value;
            remove => triggerAction.performed -= value;
        }

        /// <summary>
        /// Bumper press event
        /// </summary>
        public event Action<InputAction.CallbackContext> BumperPressed
        {
            add => bumperAction.performed += value;
            remove => bumperAction.performed -= value;
        }

        /// <summary>
        /// Menu button press event
        /// </summary>
        public event Action<InputAction.CallbackContext> MenuPressed
        {
            add => menuButtonAction.performed += value;
            remove => menuButtonAction.performed -= value;
        }

        /// <summary>
        /// Trackpad click event
        /// </summary>
        public event Action<InputAction.CallbackContext> TrackpadClicked
        {
            add => trackpadClickAction.performed += value;
            remove => trackpadClickAction.performed -= value;
        }

        /// <summary>
        /// Is the controller currently being tracked?
        /// </summary>
        public bool IsTracked => isTrackedAction.IsPressed();

        /// <summary>
        /// The world position of the controller body where the user should be holding it.
        /// </summary>
        public Vector3 Position => positionAction.ReadValue<Vector3>();

        /// <summary>
        /// The world rotation of the controller body where the user should be holding it.
        /// </summary>
        public Quaternion Rotation => rotationAction.ReadValue<Quaternion>();

        /// <summary>
        /// The world position of the controller's pointer raycast origin.
        /// </summary>
        public Vector3 PointerPosition => pointerPositionAction.ReadValue<Vector3>();

        /// <summary>
        /// The world rotation of the controller's pointer raycast origin.
        /// </summary>
        public Quaternion PointerRotation => pointerRotationAction.ReadValue<Quaternion>();

        /// <summary>
        /// The current movement velocity of the controller.
        /// </summary>
        public Vector3 Velocity => velocityAction.ReadValue<Vector3>();

        /// <summary>
        /// The current angular velocity of the controller.
        /// </summary>
        public Vector3 AngularVelocity => angularVelocityAction.ReadValue<Vector3>();

        /// <summary>
        /// The current <see cref="InputTrackingState"/> of the controller.
        /// </summary>
        public InputTrackingState TrackingState => (InputTrackingState)trackingStatusAction.ReadValue<int>();

        /// <summary>
        /// Is the controller's trigger button currently pressed?
        /// </summary>
        public bool TriggerIsPressed => triggerAction.IsPressed();

        /// <summary>
        /// The current analog value of the controller's trigger, from 0 to 1.
        /// </summary>
        public float TriggerValue => triggerValueAction.ReadValue<float>();

        /// <summary>
        /// Is the controller's bumper currently pressed?
        /// </summary>
        public bool BumperIsPressed => bumperAction.IsPressed();

        /// <summary>
        /// Is the controller's menu button currently pressed?
        /// </summary>
        public bool MenuIsPressed => menuButtonAction.IsPressed();

        /// <summary>
        /// The position on the controller's touchpad that is currently touched.
        /// </summary>
        public Vector2 TouchPosition => trackpadAction.ReadValue<Vector2>();

        /// <summary>
        /// The current touch pressure being applied to the controller's touchpad, from 0 to 1.
        /// </summary>
        public float TouchPressure => trackpadForceAction.ReadValue<float>();
    }
}

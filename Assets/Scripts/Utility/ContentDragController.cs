// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicLeap.Examples
{
    /// <summary>
    /// Utility class that relays controller trigger events to drag events
    /// </summary>
    public class ContentDragController : MonoBehaviour
    {
        bool _isDragging = false;

        /// <summary>
        /// Triggered when dragging begins
        /// </summary>
        public event Action OnBeginDrag;

        /// <summary>
        /// Triggered every frame while a drag is on-going and the transform has changed
        /// </summary>
        public event Action OnDrag;

        /// <summary>
        /// Triggered when dragging ends
        /// </summary>
        public event Action OnEndDrag;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        /// <summary>
        /// Set Up
        /// </summary>
        void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Trigger.performed += HandleTriggerDown;
            controllerActions.Trigger.canceled += HandleTriggerUp;
        }

        /// <summary>
        /// Clean Up
        /// </summary>
        private void OnDestroy()
        {
            controllerActions.Trigger.performed -= HandleTriggerDown;
            controllerActions.Trigger.canceled -= HandleTriggerUp;

            mlInputs.Dispose();
        }

        /// <summary>
        /// Triggers drag event if needed
        /// </summary>
        private void Update()
        {
            if (_isDragging && transform.hasChanged)
            {
                transform.hasChanged = false;
                OnDrag?.Invoke();
            }
        }

        /// <summary>
        /// Handler for controller trigger down
        /// </summary>
        /// <param name="controllerId">Controller ID</param>
        /// <param name="triggerValue">Trigger Value (unused)</param>
        private void HandleTriggerDown(InputAction.CallbackContext callbackContext)
        {
            _isDragging = true;
            OnBeginDrag?.Invoke();
        }

        /// <summary>
        /// Handler for controller trigger up
        /// </summary>
        /// <param name="controllerId">Controller ID</param>
        /// <param name="triggerValue">Trigger Value (unused)</param>
        private void HandleTriggerUp(InputAction.CallbackContext callbackContext)
        {
            _isDragging = false;
            OnEndDrag?.Invoke();
        }
    }
}

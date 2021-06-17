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

using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Utility class that relays controller trigger events to drag events
    /// </summary>
    [RequireComponent(typeof(MLControllerConnectionHandlerBehavior))]
    public class ContentDragController : MonoBehaviour
    {
        MLControllerConnectionHandlerBehavior _controllerConnectionHandler;
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

        /// <summary>
        /// Set Up
        /// </summary>
        void Start()
        {
            _controllerConnectionHandler = GetComponent<MLControllerConnectionHandlerBehavior>();

            #if PLATFORM_LUMIN
            MLInput.OnTriggerDown += HandleTriggerDown;
            MLInput.OnTriggerUp += HandleTriggerUp;
            #endif
        }

        /// <summary>
        /// Clean Up
        /// </summary>
        private void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLInput.OnTriggerDown -= HandleTriggerDown;
            MLInput.OnTriggerUp -= HandleTriggerUp;
            #endif
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
        private void HandleTriggerDown(byte controllerId, float triggerValue)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId))
            {
                _isDragging = true;
                OnBeginDrag?.Invoke();
            }
        }

        /// <summary>
        /// Handler for controller trigger up
        /// </summary>
        /// <param name="controllerId">Controller ID</param>
        /// <param name="triggerValue">Trigger Value (unused)</param>
        private void HandleTriggerUp(byte controllerId, float triggerValue)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId))
            {
                _isDragging = false;
                OnEndDrag?.Invoke();
            }
        }
    }
}

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
    /// ContentTap is responsible for relaying a custom controller event of
    /// tapping the touchpad at any speed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ContentTap: MonoBehaviour
    {
        private class TouchpadCustomEvents
        {
            bool _pressed = false;
            public bool pressed
            {
                get
                {
                    return _pressed;
                }

                set
                {
                    if (_pressed != value)
                    {
                        if (!_pressed)
                        {
                            TouchpadPressed?.Invoke();
                        }

                        else
                        {
                            TouchpadReleased?.Invoke();
                        }

                    }
                    _pressed = value;
                }
            }

            public Action TouchpadPressed, TouchpadReleased;
        }

        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler;
        private bool _touchpadPressedOnObject = false;
        private TouchpadCustomEvents _touchpadEvents = new TouchpadCustomEvents();


        /// <summary>
        /// Triggered when this content is tapped on.
        /// </summary>
        public event Action<GameObject> OnContentTap;

        /// <summary>
        /// Keeps track of when the touchpad is currently pressed.
        /// </summary>
        void Update()
        {
            #if PLATFORM_LUMIN
            if (_controllerConnectionHandler != null && _controllerConnectionHandler.IsControllerValid())
            {
                _touchpadEvents.pressed = _controllerConnectionHandler.ConnectedController.Touch1Active;
            }
            #endif
        }

        /// <summary>
        /// Unregisters from touchpad events.
        /// </summary>
        void OnDestroy()
        {
            if (_controllerConnectionHandler != null)
            {
                _controllerConnectionHandler = null;
                _touchpadEvents.TouchpadPressed -= OnTouchpadPressed;
                _touchpadEvents.TouchpadReleased -= OnTouchpadRelease;
                _touchpadPressedOnObject = false;
            }
        }

        /// <summary>
        /// Registers for controller input only when a controller enters the trigger area.
        /// </summary>
        /// <param name="other">Collider of the Controller</param>
        void OnTriggerEnter(Collider other)
        {
            MLControllerConnectionHandlerBehavior controllerConnectionHandler = other.GetComponent<MLControllerConnectionHandlerBehavior>();
            if (controllerConnectionHandler == null)
            {
                return;
            }

            _controllerConnectionHandler = controllerConnectionHandler;
            // Setting being pressed to 'true' here will call the OnTouchpadPressed event before we subscribe to it, forcing the user to both tap and release on this object to destroy it.
            _touchpadEvents.pressed = true;
            _touchpadEvents.TouchpadPressed += OnTouchpadPressed;
            _touchpadEvents.TouchpadReleased += OnTouchpadRelease;
        }

        /// <summary>
        /// Unregisters controller input when controller leaves the trigger area.
        /// </summary>
        /// <param name="other">Collider of the Controller</param>
        void OnTriggerExit(Collider other)
        {
            MLControllerConnectionHandlerBehavior controllerConnectionHandler = other.GetComponent<MLControllerConnectionHandlerBehavior>();
            if (_controllerConnectionHandler == controllerConnectionHandler)
            {
                _controllerConnectionHandler = null;
                _touchpadEvents.TouchpadPressed -= OnTouchpadPressed;
                _touchpadEvents.TouchpadReleased -= OnTouchpadRelease;
                _touchpadPressedOnObject = false;
            }
        }

        /// <summary>
        /// Handler for touchpad pressed events.
        /// </summary>
        private void OnTouchpadPressed()
        {
            _touchpadPressedOnObject = true;
        }

        /// <summary>
        /// Handler for touchpad released events.
        /// </summary>
        private void OnTouchpadRelease()
        {
            if (_touchpadPressedOnObject)
            {
                OnContentTap?.Invoke(gameObject);
            }
        }
    }
}

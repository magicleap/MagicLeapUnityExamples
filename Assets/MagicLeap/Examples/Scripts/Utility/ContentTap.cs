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

namespace MagicLeap.Examples
{
    /// <summary>
    /// ContentTap is responsible for relaying a custom controller event of
    /// tapping the touchpad at any speed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ContentTap : MonoBehaviour
    {
        private class TouchpadCustomEvents
        {
            bool _pressed = false;

            public bool pressed
            {
                get => _pressed;

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

        private bool _touchpadPressedOnObject = false;
        private TouchpadCustomEvents _touchpadEvents = new TouchpadCustomEvents();
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        /// <summary>
        /// Triggered when this content is tapped on.
        /// </summary>
        public event Action<GameObject> OnContentTap;

        /// <summary>
        /// Keeps track of when the touchpad is currently pressed.
        /// </summary>
        void Update()
        {
            if (controllerActions.IsTracked.IsPressed())
            {
                _touchpadEvents.pressed = controllerActions.TouchpadClick.IsPressed();
            }
        }

        private void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
        }

        /// <summary>
        /// Unregisters from touchpad events.
        /// </summary>
        void OnDestroy()
        {
            _touchpadEvents.TouchpadPressed -= OnTouchpadPressed;
            _touchpadEvents.TouchpadReleased -= OnTouchpadRelease;
            _touchpadPressedOnObject = false;
            mlInputs.Dispose();
        }

        /// <summary>
        /// Registers for controller input only when a controller enters the trigger area.
        /// </summary>
        /// <param name="other">Collider of the Controller</param>
        void OnTriggerEnter(Collider other)
        {
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
            _touchpadEvents.TouchpadPressed -= OnTouchpadPressed;
            _touchpadEvents.TouchpadReleased -= OnTouchpadRelease;
            _touchpadPressedOnObject = false;
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

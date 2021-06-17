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
    /// This class provides examples of how you can use haptics and LEDs
    /// on the Control.
    /// </summary>
    public class ControlExample : MonoBehaviour
    {
        [SerializeField]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("The text used to display status information for the example..")]
        private Text _statusText = null;

        // UpdateLED - Constants
        private const float HALF_HOUR_IN_DEGREES = 15.0f;
        private const float DEGREES_PER_HOUR = 12.0f / 360.0f;

        #pragma warning disable 414
        private int _lastLEDindex = -1;
        #pragma warning restore 414

        private const int MIN_LED_INDEX = (int)(MLInput.Controller.FeedbackPatternLED.Clock12);
        private const int MAX_LED_INDEX = (int)(MLInput.Controller.FeedbackPatternLED.Clock6And12);
        private const int LED_INDEX_DELTA = MAX_LED_INDEX - MIN_LED_INDEX;

        private MLInput.Controller _control;

        /// <summary>
        /// Initialize variables, callbacks and check null references.
        /// </summary>
        void Start()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonUp += HandleOnButtonUp;
            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            MLInput.OnTriggerDown += HandleOnTriggerDown;
            #endif
        }

        /// <summary>
        /// Update controller input based feedback.
        /// </summary>
        void Update()
        {
            UpdateLED();
            UpdateStatus();
        }

        /// <summary>
        /// Stop input api and unregister callbacks.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLInput.OnTriggerDown -= HandleOnTriggerDown;
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            MLInput.OnControllerButtonUp -= HandleOnButtonUp;
            #endif
        }

        /// <summary>
        /// Updates LED on the physical controller based on touch pad input.
        /// </summary>
        private void UpdateLED()
        {
            if (!_controllerConnectionHandler.IsControllerValid())
            {
                return;
            }

            #if PLATFORM_LUMIN
            MLInput.Controller controller = _controllerConnectionHandler.ConnectedController;
            if (controller.Touch1Active)
            {
                // Get angle of touchpad position.
                float angle = -Vector2.SignedAngle(Vector2.up, controller.Touch1PosAndForce);
                if (angle < 0.0f)
                {
                    angle += 360.0f;
                }

                // Get the correct hour and map it to [0,6]
                int index = (int)((angle + HALF_HOUR_IN_DEGREES) * DEGREES_PER_HOUR) % LED_INDEX_DELTA;

                // Pass from hour to MLInputControllerFeedbackPatternLED index  [0,6] -> [MAX_LED_INDEX, MIN_LED_INDEX + 1, ..., MAX_LED_INDEX - 1]
                index = (MAX_LED_INDEX + index > MAX_LED_INDEX) ? MIN_LED_INDEX + index : MAX_LED_INDEX;

                if (_lastLEDindex != index)
                {
                    // a duration of 0 means leave it on indefinitely
                    controller.StartFeedbackPatternLED((MLInput.Controller.FeedbackPatternLED)index, MLInput.Controller.FeedbackColorLED.BrightCosmicPurple, 0);
                    _lastLEDindex = index;
                }
            }
            else if (_lastLEDindex != -1)
            {
                controller.StopFeedbackPatternLED();
                _lastLEDindex = -1;
            }
            #endif
        }

        private void UpdateStatus()
        {
            _statusText.text = string.Format(
                "<color=#dbfb76><b>Controller Data</b></color>\n" +
                "Status: {0}\n",

                ControllerStatus.Text
                );

            if (!_controllerConnectionHandler.IsControllerValid())
            {
                return;
            }

            // Update the controller reference, to the currently connected controller.
            _control = _controllerConnectionHandler.ConnectedController;

            #if PLATFORM_LUMIN
            if(_control != null)
            {
                if (_control.Type == MLInput.Controller.ControlType.Control)
                {
                    _statusText.text += string.Format("" +
                        "Position: <i>{0}</i>\n" +
                        "Rotation: <i>{1}</i>\n\n" +
                        "<color=#dbfb76><b>Buttons</b></color>\n" +
                        "Trigger: <i>{2}</i>\n" +
                        "Bumper: <i>{3}</i>\n\n" +
                        "<color=#dbfb76><b>Touchpad</b></color>\n" +
                        "Location: <i>({4},{5})</i>\n" +
                        "Pressure: <i>{6}</i>\n\n" +
                        "<color=#dbfb76><b>Gestures</b></color>\n" +
                        "<i>{7} {8}</i>",

                        _control.Position.ToString("n2"),
                        _control.Orientation.eulerAngles.ToString("n1"),
                        _control.TriggerValue.ToString("n2"),
                        _control.IsBumperDown,
                        _control.Touch1Active ? _control.Touch1PosAndForce.x.ToString("n2") : "0.00",
                        _control.Touch1Active ? _control.Touch1PosAndForce.y.ToString("n2") : "0.00",
                        _control.Touch1Active ? _control.Touch1PosAndForce.z.ToString("n2") : "0.00",
                        _control.CurrentTouchpadGesture.Type.ToString(),
                        _control.TouchpadGestureState.ToString());
                }
                else if (_control.Type == MLInput.Controller.ControlType.MobileApp)
                {
                    _statusText.text += string.Format("" +
                        "Position: <i>{0}</i>\n" +
                        "Rotation: <i>{1}</i>\n\n" +
                        "<color=#dbfb76><b>Buttons</b></color>\n" +
                        "Trigger: <i>{2}</i>\n" +
                        "Bumper: <i>{3}</i>\n\n" +
                        "<color=#dbfb76><b>Touchpad</b></color>\n" +
                        "Touch 1 Location: <i>({4},{5})</i>\n" +
                        "Touch 2 Location: <i>({6},{7})</i>\n\n" +
                        "<color=#dbfb76><b>Gestures</b></color>\n" +
                        "<i>{8} {9}</i>",

                        "No information available",
                        _control.Orientation.eulerAngles.ToString("n2"),
                        _control.TriggerValue.ToString("n2"),
                        _control.IsBumperDown,
                        _control.Touch1Active ? _control.Touch1PosAndForce.x.ToString("n2") : "0.00",
                        _control.Touch1Active ? _control.Touch1PosAndForce.y.ToString("n2") : "0.00",
                        _control.Touch2Active ? _control.Touch2PosAndForce.x.ToString("n2") : "0.00",
                        _control.Touch2Active ? _control.Touch2PosAndForce.y.ToString("n2") : "0.00",
                        _control.CurrentTouchpadGesture.Type.ToString(),
                        _control.TouchpadGestureState.ToString());
                }
            }
            #endif
        }

        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void HandleOnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            MLInput.Controller controller = _controllerConnectionHandler.ConnectedController;

            #if PLATFORM_LUMIN
            if (controller != null && controller.Id == controllerId &&
                button == MLInput.Controller.Button.Bumper)
            {
                // Demonstrate haptics using callbacks.
                controller.StartFeedbackPatternVibe(MLInput.Controller.FeedbackPatternVibe.ForceDown, MLInput.Controller.FeedbackIntensity.Medium);
            }
            #endif
        }

        /// <summary>
        /// Handles the event for button up.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnButtonUp(byte controllerId, MLInput.Controller.Button button)
        {
            MLInput.Controller controller = _controllerConnectionHandler.ConnectedController;

            #if PLATFORM_LUMIN
            if (controller != null && controller.Id == controllerId &&
                button == MLInput.Controller.Button.Bumper)
            {
                // Demonstrate haptics using callbacks.
                controller.StartFeedbackPatternVibe(MLInput.Controller.FeedbackPatternVibe.ForceUp, MLInput.Controller.FeedbackIntensity.Medium);
            }
            #endif
        }

        /// <summary>
        /// Handles the event for trigger down.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="value">The value of the trigger button.</param>
        private void HandleOnTriggerDown(byte controllerId, float value)
        {
            MLInput.Controller controller = _controllerConnectionHandler.ConnectedController;

            #if PLATFORM_LUMIN
            if (controller != null && controller.Id == controllerId)
            {
                MLInput.Controller.FeedbackIntensity intensity = (MLInput.Controller.FeedbackIntensity)((int)(value * 2.0f));
                controller.StartFeedbackPatternVibe(MLInput.Controller.FeedbackPatternVibe.Buzz, intensity);
            }
            #endif
        }
    }
}

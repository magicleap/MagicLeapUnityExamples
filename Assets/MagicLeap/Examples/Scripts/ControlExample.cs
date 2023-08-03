// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.MagicLeap.Native;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class provides examples of how you can use haptics
    /// on the Control.
    /// </summary>
    public class ControlExample : MonoBehaviour
    {
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        [SerializeField] private GestureSubsystemComponent gestureComponent;

        [SerializeField, Tooltip("The text used to display status information for the example..")]
        private Text _statusText = null;

        private bool enableSnapshotPrediction = false;

        /// <summary>
        /// Initialize variables, callbacks and check null references.
        /// </summary>
        void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
            controllerActions.TouchpadPosition.performed += HandleOnTouchpad;
            // canceled event used to detect when bumper button is released
            controllerActions.Bumper.canceled += HandleOnBumper;
            controllerActions.Bumper.performed += HandleOnBumper;
            controllerActions.Bumper.performed += ToggleSnapshotPrediction;
            controllerActions.Trigger.performed += HandleOnTrigger;

            InputSubsystem.Extensions.Controller.AttachTriggerListener(HandleOnTriggerEvent);

            _statusText.fontSize = 11;
        }

        /// <summary>
        /// Update controller input based feedback.
        /// </summary>
        void Update()
        {
            UpdateStatus();
        }

        /// <summary>
        /// Stop input api and unregister callbacks.
        /// </summary>
        void OnDestroy()
        {
            controllerActions.TouchpadPosition.performed -= HandleOnTouchpad;
            controllerActions.Bumper.canceled -= HandleOnBumper;
            controllerActions.Bumper.performed -= HandleOnBumper;
            controllerActions.Bumper.performed -= ToggleSnapshotPrediction;
            controllerActions.Trigger.performed -= HandleOnTrigger;

            InputSubsystem.Extensions.Controller.RemoveTriggerListener(HandleOnTriggerEvent);

            MLGraphicsHooks.Shutdown();

            mlInputs.Dispose();
        }

        private void UpdateStatus()
        {
            _statusText.text = $"<color=#B7B7B8><b>Controller Data</b></color>\n Status: {ControllerStatus.Text}\n";

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append($"Position: <i>{controllerActions.Position.ReadValue<Vector3>().ToString("n2")}</i>\n");
            strBuilder.Append($"Velocity: <i>{controllerActions.Velocity.ReadValue<Vector3>().ToString("n2")}</i>\n");
            strBuilder.Append($"AngularVelocity: <i>{controllerActions.AngularVelocity.ReadValue<Vector3>().ToString("n2")}</i>\n");
            strBuilder.Append($"Acceleration: <i>{controllerActions.Acceleration.ReadValue<Vector3>().ToString("n2")}</i>\n");
            strBuilder.Append($"AngularAcceleration: <i>{controllerActions.AngularAcceleration.ReadValue<Vector3>().ToString("n2")}</i>\n");

            strBuilder.Append($"Rotation: <i>{controllerActions.Rotation.ReadValue<Quaternion>().ToString("n1")}</i>\n\n");
            strBuilder.Append($"<color=#B7B7B8><b>Buttons</b></color>\n");
            strBuilder.Append($"Menu: <i>{controllerActions.Menu.IsPressed()}</i>\n\n");
            strBuilder.Append($"Trigger: <i>{controllerActions.Trigger.ReadValue<float>():n2}</i>\n");
            strBuilder.Append($"TriggerHold phase: <i>{controllerActions.TriggerHold.phase}</i>\n");
            strBuilder.Append($"Bumper: <i>{controllerActions.Bumper.IsPressed()}</i>\n\n");
            strBuilder.Append($"<color=#B7B7B8><b>Touchpad</b></color>\n");
            strBuilder.Append($"Location: <i>({controllerActions.TouchpadPosition.ReadValue<Vector2>().x:n2}," +
                              $"{controllerActions.TouchpadPosition.ReadValue<Vector2>().y:n2})</i>\n");
            strBuilder.Append($"Pressure: <i>{controllerActions.TouchpadForce.ReadValue<float>()}</i>\n\n");
            strBuilder.Append($"<color=#B7B7B8><b>Gestures</b></color>\n<i></i>");
            if (gestureComponent != null && gestureComponent.gestureSubsystem != null)
            {
                foreach (var touchpadEvent in gestureComponent.gestureSubsystem.touchpadGestureEvents)
                {
                    strBuilder.Append($"<i>{touchpadEvent.type} {touchpadEvent.state}</i>");
                }
            }

            _statusText.text += strBuilder.ToString();
        }

        /// <summary>
        /// Handles the event for bumper.
        /// </summary>
        /// <param name="obj">Input Callback</param>
        private void HandleOnBumper(InputAction.CallbackContext obj)
        {
            bool bumperDown = obj.ReadValueAsButton();

            Debug.Log("Bumper was released this frame: " + obj.action.WasReleasedThisFrame());
        }

        private void ToggleSnapshotPrediction(InputAction.CallbackContext obj)
        {
            enableSnapshotPrediction = !enableSnapshotPrediction;
            MLGraphicsHooks.RequestPredictedSnapshots(enableSnapshotPrediction);
        }

        private void HandleOnTrigger(InputAction.CallbackContext obj)
        {
            float triggerValue = obj.ReadValue<float>();
        }

        private void HandleOnTouchpad(InputAction.CallbackContext obj)
        {
            Vector2 triggerValue = obj.ReadValue<Vector2>();
        }

        private void HandleOnTriggerEvent(ushort controllerId, InputSubsystem.Extensions.Controller.MLInputControllerTriggerEvent triggerEvent, float depth)
        {
            Debug.Log($"Received trigger event: {triggerEvent} with trigger depth: {depth}, on controller id: {controllerId} ");
        }
    }
}

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
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.EventSystems;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class provides examples of how you can use controller haptics.
    /// </summary>
    public class HapticsExample : MonoBehaviour
    {
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        private EventTrigger[] eventTriggers;

        private InputSubsystem.Extensions.Haptics.CustomPattern customHaptics = new InputSubsystem.Extensions.Haptics.CustomPattern();

        /// <summary>
        /// Initialize variables and callbacks.
        /// </summary>
        void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Bumper.canceled += HandleOnBumper;
            controllerActions.Bumper.performed += HandleOnBumper;
            controllerActions.Trigger.performed += HandleOnTrigger;

            CreateCustomHapticsPattern();

            eventTriggers = FindObjectsOfType<EventTrigger>();
            foreach (var eventTrigger in eventTriggers)
            {
                foreach (var trigger in eventTrigger.triggers)
                {
                    if (trigger.eventID == EventTriggerType.PointerEnter)
                        trigger.callback.AddListener(HandleOnEnter);
                }
            }
        }

        private void CreateCustomHapticsPattern()
        {
            var buzz1 = InputSubsystem.Extensions.Haptics.Buzz.Create(200, 800, 2000, 50);
            var preDefinedA = InputSubsystem.Extensions.Haptics.PreDefined.Create(InputSubsystem.Extensions.Haptics.PreDefined.Type.A);
            var preDefinedC = InputSubsystem.Extensions.Haptics.PreDefined.Create(InputSubsystem.Extensions.Haptics.PreDefined.Type.C);
            var buzz2 = InputSubsystem.Extensions.Haptics.Buzz.Create(800, 200, 2000, 20);

            customHaptics.Add(in buzz1);
            customHaptics.Add(in preDefinedA, 500);
            customHaptics.Add(in preDefinedC, 500);
            customHaptics.Add(in buzz2);
        }

        private void HandleOnEnter(BaseEventData data)
        {
            var preDefined = InputSubsystem.Extensions.Haptics.PreDefined.Create(InputSubsystem.Extensions.Haptics.PreDefined.Type.A);
            preDefined.StartHaptics();
            _statusText.text = "Started predefined pattern";
        }

        private void HandleOnTrigger(InputAction.CallbackContext obj)
        {
            bool pressed = obj.ReadValueAsButton();

            if(pressed)
            {
                var buzz = InputSubsystem.Extensions.Haptics.Buzz.Create(200, 200, 400, 20);
                buzz.StartHaptics();
                _statusText.text = "Started buzz";
            }
            else
            {
                InputSubsystem.Extensions.Haptics.Stop();
                _statusText.text = "Stopped buzz";
            }

        }

        private void HandleOnBumper(InputAction.CallbackContext obj)
        {
            bool bumperDown = obj.ReadValueAsButton();
            if (bumperDown)
                customHaptics.StartHaptics();
            _statusText.text = "Started custom haptics";
        }

        /// <summary>
        /// Unregister callbacks and dispose inputs.
        /// </summary>
        void OnDestroy()
        {
            controllerActions.Bumper.canceled -= HandleOnBumper;
            controllerActions.Bumper.performed -= HandleOnBumper;
            controllerActions.TriggerButton.performed -= HandleOnTrigger;
            mlInputs.Dispose();

            foreach (var eventTrigger in eventTriggers)
            {
                foreach (var trigger in eventTrigger.triggers)
                {
                    if (trigger.eventID == EventTriggerType.PointerEnter)
                        trigger.callback.RemoveListener(HandleOnEnter);
                }
            }

        }
    }
}

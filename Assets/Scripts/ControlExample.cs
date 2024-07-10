// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    public class ControlExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text statusText;

        private readonly StringBuilder strBuilder = new();

        private void Update()
        {
            strBuilder.Clear();

            var controller = MagicLeapController.Instance;

            strBuilder.AppendLine($"<b>IsTracked:</b> {controller.IsTracked}");

            if (controller.IsTracked)
            {
                strBuilder.AppendLine();
                strBuilder.AppendLine("<b>Pointer:</b>");
                strBuilder.AppendLine($"Position: {controller.PointerPosition}");
                strBuilder.AppendLine($"Rotation: {controller.PointerRotation}");

                strBuilder.AppendLine("<b>Device</b>");
                strBuilder.AppendLine($"Position: {controller.Position}");
                strBuilder.AppendLine($"Rotation: {controller.Rotation}");
                strBuilder.AppendLine($"Velocity: {controller.Velocity}");
                strBuilder.AppendLine($"Angular Velocity: {controller.AngularVelocity}");

                strBuilder.AppendLine("<b>Buttons</b>");
                strBuilder.AppendLine($"Menu: {controller.MenuIsPressed}");
                strBuilder.AppendLine($"Trigger: {controller.TriggerIsPressed}");
                strBuilder.AppendLine($"Trigger Amount: {controller.TriggerValue}");
                strBuilder.AppendLine($"Bumper: {controller.BumperIsPressed}");

                strBuilder.AppendLine("<b>Touchpad</b>");
                strBuilder.AppendLine($"Touchpad: {controller.TouchPosition}");
                strBuilder.AppendLine($"Touchpad Pressure: {controller.TouchPressure}");
            }

            statusText.text = strBuilder.ToString();
        }
    }
}

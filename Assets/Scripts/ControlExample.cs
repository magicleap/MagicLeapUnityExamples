using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using Utils = UnityEngine.XR.OpenXR.Utils;

public class ControlExample : MonoBehaviour
{
    [SerializeField, Tooltip("The text used to display status information for the example.")]
    private Text statusText;
    
    /// <summary>
    /// Provided by Assets/MagicLeapInput.inputactions
    /// </summary>
    private MagicLeapInput inputs;

    private readonly StringBuilder strBuilder = new();

    private void Start()
    {
        inputs = new MagicLeapInput();
        inputs.Enable();
    }

    private void OnDestroy()
    {
        inputs.Dispose();
    }

    private void Update()
    {
        strBuilder.Clear();

        var isTracked = inputs.Controller.IsTracked.IsPressed();
        strBuilder.AppendLine($"<b>IsTracked:</b> {isTracked}");
        
        if (isTracked)
        {
            var pointerPos = inputs.Controller.PointerPosition.ReadValue<Vector3>();
            var pointerRot = inputs.Controller.PointerRotation.ReadValue<Quaternion>();
            var position = inputs.Controller.Position.ReadValue<Vector3>();
            var rotation = inputs.Controller.Rotation.ReadValue<Quaternion>();
            var velocity = inputs.Controller.Velocity.ReadValue<Vector3>();
            var angularVelocity = inputs.Controller.AngularVelocity.ReadValue<Vector3>();
            var menuPressed = inputs.Controller.MenuButton.IsPressed();
            var triggerAmount = inputs.Controller.TriggerValue.ReadValue<float>();
            var triggerPressed = inputs.Controller.Trigger.IsPressed();
            var bumperPressed = inputs.Controller.Bumper.IsPressed();
            var touchPosition = inputs.Controller.Trackpad.ReadValue<Vector2>();
            var pressure = inputs.Controller.TrackpadForce.ReadValue<float>();

            strBuilder.AppendLine();
            strBuilder.AppendLine("<b>Pointer:</b>");
            strBuilder.AppendLine($"Position: {pointerPos}");
            strBuilder.AppendLine($"Rotation: {pointerRot}");

            //strBuilder.AppendLine();
            strBuilder.AppendLine("<b>Device</b>");
            strBuilder.AppendLine($"Position: {position}");
            strBuilder.AppendLine($"Rotation: {rotation}");
            strBuilder.AppendLine($"Velocity: {velocity}");
            strBuilder.AppendLine($"Angular Velocity:\n {angularVelocity}");
            
            //strBuilder.AppendLine();
            strBuilder.AppendLine("<b>Buttons</b>");
            strBuilder.AppendLine($"Menu: {menuPressed}");
            strBuilder.AppendLine($"Trigger: {triggerPressed}");
            strBuilder.AppendLine($"Trigger Amount: {triggerAmount}");
            strBuilder.AppendLine($"Bumper: {bumperPressed}");

            //strBuilder.AppendLine();
            strBuilder.AppendLine("<b>Touchpad</b>");
            strBuilder.AppendLine($"Touchpad: {touchPosition}");
            strBuilder.AppendLine($"Touchpad Pressure: {pressure}");
        }

        statusText.text = strBuilder.ToString();
    }
}

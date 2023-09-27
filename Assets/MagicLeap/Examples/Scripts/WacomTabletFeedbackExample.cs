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
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This provides visual feedback for the information about the wacom tablet's last known state of the device, pen, and buttons.
    /// </summary>
    public class WacomTabletFeedbackExample : MonoBehaviour
    {
        public event Action OnTipDown;
        public event Action OnTipUp;
        public event Action OnEraserDown;
        public event Action OnEraserUp;
        public event Action OnFirstBarrelButtonDown;
        public event Action OnFirstBarrelButtonUp;
        public event Action OnSecondBarrelButtonDown;
        public event Action OnSecondBarrelButtonUp;
        public event Action OnThirdBarrelButtonDown;
        public event Action OnThirdBarrelButtonUp;
        public event Action OnFourthBarrelButtonDown;
        public event Action OnFourthBarrelButtonUp;

        [SerializeField, Tooltip("The component that adjusts the placement of the wacom tablet.")]
        private PlaceFromCamera wacomTabletPlacement = null;

        [SerializeField, Tooltip("UI text for the MLInput tablet API values.")]
        private Text statusText = null;

        // Used to get ml inputs.
        private MagicLeapInputs mlInputs;

        // Used to get controller action data.
        private MagicLeapInputs.ControllerActions controllerActions;

        /// <summary>
        /// Control representing the current position of the pointer on screen.
        /// Within player code, the coordinates are in the coordinate space of Unity's Display.
        /// </summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// Rotation of the pen around its own axis. Only supported on a limited number of pens, such as the Wacom Art Pen.
        /// </summary>
        public float PenTwist { get; private set; }

        /// <summary>
        /// Tilt of the pen relative to the surface.
        /// </summary>
        public Vector2 PenTilt { get; private set; }

        /// <summary>
        /// Whether the pen is currently in detection range of the tablet.
        /// </summary>
        public bool InRange { get; private set; }

        /// <summary>
        /// Whether the forth button on the barrel of the pen is pressed.
        /// </summary>
        public bool ForthBarrel { get; private set; }

        /// <summary>
        /// 	Whether the third button on the barrel of the pen is pressed.
        /// </summary>
        public bool ThirdBarrel { get; private set; }

        /// <summary>
        /// 	Whether the second button on the barrel of the pen is pressed.
        /// </summary>
        public bool SecondBarrel { get; private set; }

        /// <summary>
        /// Whether the first button on the barrel of the pen is pressed.
        /// </summary>
        public bool FirstBarrel { get; private set; }

        /// <summary>
        /// Whether the eraser/back end of the pen touches the surface.
        /// </summary>
        public bool Eraser { get; private set; }

        /// <summary>
        /// Whether the tip of the pen touches the surface. Same as the inherited Pointer.press.
        /// </summary>
        public bool Tip { get; private set; }

        /// <summary>
        /// The size of the area where the finger touches the surface. This is only relevant for touch input.
        /// </summary>
        public Vector2 Radius { get; private set; }

        public float Pressure { get; private set; }

        /// <summary>
        /// What this means exactly depends on the nature of the pointer. For pens (Pen), it means that the pen tip is touching the screen/tablet surface
        /// </summary>
        public bool Pressing { get; private set; }

        /// <summary>
        /// Every time a pointer is moved, it generates a motion delta. This control represents this motion.
        /// </summary>
        public Vector2 Delta { get; private set; }

        /// <summary>
        /// If pen is connected.
        /// </summary>
        public bool PenConnected => UnityEngine.InputSystem.Pen.current != null;

        /// <summary>
        /// The distance of the pen from the surface.
        /// </summary>
        public float Distance { get; private set; }

        /// <summary>
        /// The touch ring on tablet .
        /// </summary>
        public int TouchRing { get; private set; }

        /// <summary>
        /// Validates fields and subscribes to input event.
        /// </summary>
        void Start()
        {
            if (wacomTabletPlacement == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._wacomTabletPlacement is not set, disabling script.");
                enabled = false;
                return;
            }

            if (statusText == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();

            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
            controllerActions.Menu.performed += MenuOnPerformed;
        }


        /// <summary>
        /// Unregisters from input event.
        /// </summary>
        void OnDestroy()
        {
            controllerActions.Bumper.performed -= MenuOnPerformed;

            mlInputs.Disable();
            mlInputs.Dispose();
        }

        /// <summary>
        /// Updates the UI text.
        /// </summary>
        void Update()
        {
            if (PenConnected)
            {
                Position = new Vector2(Pen.current.position.x.ReadValue(), Pen.current.position.y.ReadValue());
                Delta = new Vector2(Pen.current.delta.x.ReadValue(), Pen.current.delta.y.ReadValue());
                Pressing = Pen.current.press.isPressed;
                Pressure = Pen.current.pressure.ReadValue();
                Radius = new Vector2(Pen.current.radius.x.ReadValue(), Pen.current.radius.y.ReadValue());
                Tip = Pen.current.tip.isPressed;
                if (Pen.current.tip.wasPressedThisFrame) OnTipDown?.Invoke();
                if (Pen.current.tip.wasReleasedThisFrame) OnTipUp?.Invoke();
                Eraser = Pen.current.eraser.isPressed;
                if (Pen.current.eraser.wasPressedThisFrame) OnEraserDown?.Invoke();
                if (Pen.current.eraser.wasReleasedThisFrame) OnEraserUp?.Invoke();
                FirstBarrel = Pen.current.firstBarrelButton.isPressed;
                if (Pen.current.firstBarrelButton.wasPressedThisFrame) OnFirstBarrelButtonDown?.Invoke();
                if (Pen.current.firstBarrelButton.wasReleasedThisFrame) OnFirstBarrelButtonUp?.Invoke();
                SecondBarrel = Pen.current.secondBarrelButton.isPressed;
                if (Pen.current.secondBarrelButton.wasPressedThisFrame) OnSecondBarrelButtonDown?.Invoke();
                if (Pen.current.secondBarrelButton.wasReleasedThisFrame) OnSecondBarrelButtonUp?.Invoke();
                ThirdBarrel = Pen.current.thirdBarrelButton.isPressed;
                if (Pen.current.thirdBarrelButton.wasPressedThisFrame) OnThirdBarrelButtonDown?.Invoke();
                if (Pen.current.thirdBarrelButton.wasReleasedThisFrame) OnThirdBarrelButtonUp?.Invoke();
                ForthBarrel = Pen.current.fourthBarrelButton.isPressed;
                if (Pen.current.fourthBarrelButton.wasPressedThisFrame) OnFourthBarrelButtonDown?.Invoke();
                if (Pen.current.fourthBarrelButton.wasReleasedThisFrame) OnFourthBarrelButtonUp?.Invoke();
                InRange = Pen.current.inRange.isPressed;
                PenTilt = new Vector2(Pen.current.tilt.x.ReadValue(), Pen.current.tilt.y.ReadValue());
                PenTwist = Pen.current.twist.ReadValue();
            }

            statusText.text =
                $"<color=#B7B7B8><b>Tablet Data</b></color>\nStatus: {(PenConnected ? "Connected" : "Disconnected")}\n\n";

            if (PenConnected)
            {
                statusText.text +=
                    $"Location:\n({Position.x}, {Position.y})\n" +
                    $"Delta:\n({Delta.x}, {Delta.y})\n" +
                    $"Pressure:\n{Pressure}\n" +
                    $"Tilt:\n({PenTilt.x}, {PenTilt.y})\n" +
                    $"Touching:\t\t{Pressing}\n" +
                    $"Tool Type:\t\t{(Eraser ? "Eraser" : "Tip")}\n " +
                    $"\n<b><color=#B7B7B8>Button Press Events</color></b>\n" +
                    $"\tPen Button 1:\t\t {(FirstBarrel ? "Yes" : "No")}\n" +
                    $"\tPen Button 2:\t\t {(SecondBarrel ? "Yes" : "No")}\n" +
                    $"\tPen Button 3:\t\t {(ThirdBarrel ? "Yes" : "No")}\n" +
                    $"\tPen Button 4:\t\t {(ForthBarrel ? "Yes" : "No")}";
            }
        }

        /// <summary>
        /// Handles the event for menu button down.
        /// Toggles if the wacom tablet should update it's placement to the user position.
        /// </summary>
        private void MenuOnPerformed(InputAction.CallbackContext context)
        {
            wacomTabletPlacement.PlaceOnUpdate = !wacomTabletPlacement.PlaceOnUpdate;
        }
    }
}

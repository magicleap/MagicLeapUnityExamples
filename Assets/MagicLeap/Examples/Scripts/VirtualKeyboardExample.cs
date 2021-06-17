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

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Updates the parent for the InputField & VirtualKeyboard at runtime.
    /// This is done to prevent modifying or breaking the Prefab connection of the UserInterface.
    /// The UI Status panel is also updated each frame with relative controller information.
    /// </summary>
    public class VirtualKeyboardExample : MonoBehaviour
    {
        [SerializeField, Tooltip("A reference to the controller connection handler in the scene.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("The status text field within the UserInterface.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("A reference to the example UI canvas placement script.")]
        private PlaceFromCamera _examplePlacement = null;

        [SerializeField, Tooltip("A reference to the user interface canvas.")]
        private Canvas _interfaceCanvas = null;

        [SerializeField, Tooltip("A reference to the keyboard canvas.")]
        private Canvas _keyboardCanvas = null;

        private Camera _mainCamera = null;

        private void Start()
        {
            _mainCamera = Camera.main;

            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            #endif
        }

        private void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            #endif
        }

        private void Update()
        {
            UpdateStatus();
            UpdateCanvasDepth();
        }

        /// <summary>
        /// Updates the status text.
        /// </summary>
        private void UpdateStatus()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);
        }

        /// <summary>
        /// Checks the distance from camera for each canvas and sets the correct sorting order.
        /// </summary>
        private void UpdateCanvasDepth()
        {
            float keyboardDistance = Vector3.Distance(_mainCamera.transform.position, _keyboardCanvas.transform.position);
            float interfaceDistance = Vector3.Distance(_mainCamera.transform.position, _interfaceCanvas.transform.position);

            _keyboardCanvas.sortingOrder = (keyboardDistance > interfaceDistance) ? 0 : 1;
            _interfaceCanvas.sortingOrder = (interfaceDistance > keyboardDistance) ? 0 : 1;
        }

        private void HandleOnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId))
            {
                if (button == MLInput.Controller.Button.Bumper)
                {
                    StartCoroutine("SingleFrameUpdate");
                }
            }
        }

        private IEnumerator SingleFrameUpdate()
        {
            _examplePlacement.PlaceOnUpdate = true;
            yield return new WaitForEndOfFrame();
            _examplePlacement.PlaceOnUpdate = false;
        }
    }
}

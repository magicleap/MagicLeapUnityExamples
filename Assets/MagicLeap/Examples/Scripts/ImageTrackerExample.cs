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
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// This provides an example of interacting with the image tracker visualizers using the controller.
    /// </summary>
    public class ImageTrackerExample : MonoBehaviour
    {
        /// <summary>
        /// List of all the MLImageTrackerBehaviors in the scene to use.
        /// </summary>
        [SerializeField, Tooltip("The MLImageTrackerBehaviors that will have their status tracked.")]
        private MLImageTrackerBehavior[] _imageTrackers = null;

        /// <summary>
        /// List of all the MLImageTrackerVisualizers in the scene to use.
        /// </summary>
        [SerializeField, Tooltip("The MLImageTrackerVisualizers that can be toggled.")]
        private ImageTrackerVisualizer[] _visualizers = null;
        private bool _visualizersActive = false;

        [SerializeField, Tooltip("The status text for the UI.")]
        private Text _statusText = null;

        [Space, SerializeField, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        /// <summary>
        /// Validates fields and registers for OnTriggerDown.
        /// </summary>
        void Start()
        {
            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: ImageTrackerExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_visualizers.Length < 1)
            {
                Debug.LogError("Error: ImageTrackerExample._visualizers is not set, disabling script.");
                enabled = false;
                return;
            }
            if (_statusText == null)
            {
                Debug.LogError("Error: ImageTrackerExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            #endif
        }

        /// <summary>
        /// Updates the _statusText used by the UI.
        /// </summary>
        void Update()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n\n", ControllerStatus.Text);

            foreach (MLImageTrackerBehavior imageTracker in _imageTrackers)
            {
                _statusText.text += string.Format("<color=#dbfb76><b>{0}</b></color>\nStatus: {1}\n\n",
                    imageTracker.name,
                    imageTracker.TrackingStatus.ToString()
                    );
            }

        }

        /// <summary>
        /// Unregister callbacks and stop input API.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            #endif
        }

        /// <summary>
        /// Enables and disables the visualizers.
        /// </summary>
        private void ToggleVisualizers()
        {
            foreach (ImageTrackerVisualizer visualizer in _visualizers)
            {
                foreach (Transform child in visualizer.transform)
                {
                    child.gameObject.SetActive(_visualizersActive);
                }
            }

            _visualizersActive = !_visualizersActive;
        }

        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void HandleOnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId) && button == MLInput.Controller.Button.Bumper)
            {
                ToggleVisualizers();
            }
        }
    }
}

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
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core.StarterKit;

namespace MagicLeap
{
    /// <summary>
    /// The UI displays the detected light intensity level indicated.
    /// </summary>
    public class LightTrackingExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The primary light that is used in the scene.")]
        private Light _light = null;

        [SerializeField, Tooltip("The image (filled) that is used to display the light intensity.")]
        private Image _lightIntensity = null;

        [SerializeField, Tooltip("The text used to display status information for the example..")]
        private Text _statusText = null;

        [SerializeField, Tooltip("The canvas used to diplay the light intensity meter.")]
        private GameObject _lightTrackingCanvas = null;

        [SerializeField, Space, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        private Camera _camera = null;

        private void Start()
        {
            if (_light == null)
            {
                Debug.LogError("Error: LightTrackingExample._light is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_lightIntensity == null)
            {
                Debug.LogError("Error: LightTrackingExample._lightIntensity is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: LightTrackingExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_lightTrackingCanvas == null)
            {
                Debug.LogError("Error: LightTrackingExample._lightTrackingCanvas is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: LightTrackingExample._controllerConnectionHandler not set, disabling script.");
                enabled = false;
                return;
            }

            MLResult result = MLLightingTrackingStarterKit.Start();
            #if PLATFORM_LUMIN
            if (!result.IsOk)
            {
                Debug.LogError("Error: LightTrackingExample failed to start MLLightingTrackingStarterKit, disabling script.");
                enabled = false;
                return;
            }
            #endif

            _camera = Camera.main;
            UpdateStatus();

            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += OnButtonDown;
            #endif

            StartCoroutine(UpdateLightCanvas());
        }

        void Update()
        {
                UpdateStatus();

                // Set the light intensity of the scene light.
                _light.intensity = MLLightingTrackingStarterKit.NormalizedLuminance;

                // Set the light intensity meter.
                _lightIntensity.fillAmount = MLLightingTrackingStarterKit.NormalizedLuminance;

                // Sets the color and intensity in the scene.
                RenderSettings.ambientLight = MLLightingTrackingStarterKit.TemperatureColor;
                RenderSettings.ambientIntensity = MLLightingTrackingStarterKit.NormalizedLuminance;
        }

        private void OnDestroy()
        {
            MLLightingTrackingStarterKit.Stop();
        }

        /// <summary>
        /// Updates the status text.
        /// </summary>
        private void UpdateStatus()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);

            _statusText.text += string.Format(
                "\n<color=#dbfb76><b> Light Data</b></color>\n Light Intensity: {0}", MLLightingTrackingStarterKit.NormalizedLuminance);
        }

        private void OnButtonDown(byte controller_id, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controller_id) && button == MLInput.Controller.Button.Bumper)
            {
                TranslateLightingCanvas();
            }
        }

        /// <summary>
        /// Translate light tracking canvas in camera view.
        /// </summary>
        private void TranslateLightingCanvas()
        {
            _lightTrackingCanvas.transform.position = _camera.transform.position + (_camera.transform.forward * 1.25f);
            _lightTrackingCanvas.transform.rotation = Quaternion.LookRotation(_lightTrackingCanvas.transform.position - _camera.transform.position);
        }

        /// <summary>
        /// Waits to the first frame to be rendered to get a valid camera transform.
        /// </summary>
        private IEnumerator UpdateLightCanvas()
        {
            yield return new WaitForEndOfFrame();
            TranslateLightingCanvas();
        }
    }
}

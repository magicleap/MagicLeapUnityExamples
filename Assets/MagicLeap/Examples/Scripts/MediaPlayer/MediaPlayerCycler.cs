// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

// Disabling MLMedia deprecated warning for the internal project
#pragma warning disable 618

using System;
using MagicLeap.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// Utility script to cycle through a set of media player example prefabs.
    /// </summary>
    public class MediaPlayerCycler : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        private MLMediaPlayerBehavior[] _mediaPlayerBehaviors = null;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        private int _mediaPlayerIndex = 0;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        private void Awake()
        {
            _mediaPlayerBehaviors = GetComponentsInChildren<MLMediaPlayerBehavior>(true);

            foreach (var player in _mediaPlayerBehaviors)
                player.transform.parent.gameObject.SetActive(false);
        }

        /// <summary>
        /// Initialize cycler.
        /// </summary>
        void Start()
        {
            // This particular feature is not supported in AppSim. This sample uses the ml_media_player which is not implemented in AppSim. 
#if UNITY_EDITOR
            return;
#endif
            
            MLResult result = MLPermissions.RequestPermission(UnityEngine.Android.Permission.ExternalStorageRead, permissionCallbacks);
        }

        private void StartAfterPermissions()
        {
            var activeBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
            activeBehavior.transform.parent.gameObject.SetActive(true);

            activeBehavior.Play();
        }

        /// <summary>
        /// Cycle through media players on scene. 
        /// </summary>
        private void Cycle()
        {
            var inactiveBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
            inactiveBehavior.transform.parent.gameObject.SetActive(false);

            inactiveBehavior.Pause();

            _mediaPlayerIndex = (_mediaPlayerIndex + 1) % _mediaPlayerBehaviors.Length;
            var activeBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
            activeBehavior.transform.parent.gameObject.SetActive(true);

            activeBehavior.Play();
        }

        /// <summary>
        /// Update controller status text.
        /// </summary>
        void Update()
        {
            UpdateStatusText();
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusText.text = $"<color=#B7B7B8><b>ControllerData</b></color>\nStatus: {ControllerStatus.Text}\n";

            var sourcePlayed = "None";
            if (_mediaPlayerBehaviors.Length > 0)
            {
                var activeBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
                sourcePlayed = $"Source: {activeBehavior.pathSourceType}, path: {activeBehavior.source}.";
            }

            _statusText.text += $"\n<color=#B7B7B8><b>Active MediaPlayer source</b></color>\n{sourcePlayed}\n";
        }

        /// <summary>
        /// Subscribe to button down event when enabled.
        /// </summary>
        void OnEnable()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        }

        /// <summary>
        /// Unsubscribe to button down event when enabled.
        /// </summary>
        void OnDisable()
        {
            controllerActions.Bumper.performed -= OnBumperDown;

            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
        }

        /// <summary>
        /// Handles the event for button down. Cycle through known media player examples when bumper is pressed.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnBumperDown(InputAction.CallbackContext callbackContext)
        {
            Cycle();
        }

        private void OnPermissionDenied(string permission)
        {
            MLPluginLog.Error($"{permission} denied, example won't function.");
        }

        private void OnPermissionGranted(string permission)
        {
            controllerActions.Bumper.performed += OnBumperDown;
            StartAfterPermissions();
        }
    }
}

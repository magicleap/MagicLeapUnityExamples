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
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core;

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
            MLResult result = MLPermissions.RequestPermission(UnityEngine.Android.Permission.ExternalStorageRead, permissionCallbacks);
        }

        private void StartAfterPermissions()
        {
            var activeBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
            activeBehavior.transform.parent.gameObject.SetActive(true);

#if UNITY_MAGICLEAP || UNITY_ANDROID
            activeBehavior.Play();
#endif
        }

        /// <summary>
        /// Cycle through media players on scene. 
        /// </summary>
        private void Cycle()
        {
            var inactiveBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
            inactiveBehavior.transform.parent.gameObject.SetActive(false);
#if UNITY_MAGICLEAP || UNITY_ANDROID
            inactiveBehavior.Pause();
#endif
            
            _mediaPlayerIndex = (_mediaPlayerIndex + 1) % _mediaPlayerBehaviors.Length;
            var activeBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
            activeBehavior.transform.parent.gameObject.SetActive(true);

#if UNITY_MAGICLEAP || UNITY_ANDROID
            activeBehavior.Play();
#endif
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
            _statusText.text = $"<color=#dbfb76><b>ControllerData</b></color>\nStatus: {ControllerStatus.Text}\n";

            var sourcePlayed = "None";
            if (_mediaPlayerBehaviors.Length > 0)
            {
                var activeBehavior = _mediaPlayerBehaviors[_mediaPlayerIndex];
                sourcePlayed = $"Source: {activeBehavior.pathSourceType}, path: {activeBehavior.source}.";
            }

            _statusText.text += $"\n<color=#dbfb76><b>Active MediaPlayer source</b></color>\n{sourcePlayed}\n";
        }

        /// <summary>
        /// Subscribe to button down event when enabled.
        /// </summary>
        void OnEnable()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
#endif
        }

        /// <summary>
        /// Unsubscribe to button down event when enabled.
        /// </summary>
        void OnDisable()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            controllerActions.Bumper.performed -= OnBumperDown;

            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
#endif
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
#if UNITY_ANDROID
            MLPluginLog.Error($"{permission} denied, example won't function.");
#endif
        }

        private void OnPermissionGranted(string permission)
        {
            controllerActions.Bumper.performed += OnBumperDown;
            StartAfterPermissions();
        }
    }
}

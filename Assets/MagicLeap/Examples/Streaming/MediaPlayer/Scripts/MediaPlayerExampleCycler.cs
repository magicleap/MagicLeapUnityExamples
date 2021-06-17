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

namespace MagicLeap
{
    /// <summary>
    /// Utility script to cycle through a set of media player example prefabs.
    /// </summary>
    public class MediaPlayerExampleCycler : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("MediaPlayerExample Prefabs to cycle through")]
        private GameObject[] _mediaPlayerExamplePrefabs = null;

        #if UNITY_EDITOR
        /// Unity Editor only code to cycle when no controller is in use.
        private static float _cycleTime = 10;
        #endif

        private int _mediaPlayerExamplePrefabIndex = 0;

        /// <summary>
        /// Validate parameters and initialize cycler, disable script if errors were detected.
        /// </summary>
        void Awake()
        {
            if (_mediaPlayerExamplePrefabs != null && _mediaPlayerExamplePrefabs.Length > 0)
            {
                foreach (var player in _mediaPlayerExamplePrefabs)
                {
                    if (player)
                    {
                        player.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError("Error: MediaPlayerExampleCycler null entry in MediaPlayerExamplePrefabs array, disabling script.");
                        enabled = false;
                        return;
                    }
                }

                // Make sure we start from the beginning of array.
                _mediaPlayerExamplePrefabIndex = 0;
                _mediaPlayerExamplePrefabs[_mediaPlayerExamplePrefabIndex].SetActive(true);
            }
            else
            {
                Debug.LogError("Error: MediaPlayerExampleCycler MediaPlayerExamplePrefabs array is empty, disabling script.");
                enabled = false;
            }
        }


        /// <summary>
        /// Update controller status text. Cycle through media if running in Editor.
        /// </summary>
        void Update()
        {
            UpdateStatusText();

            #if UNITY_EDITOR
            /// Unity Editor only code to cycle when no controller is in use.
            if (Time.time > _cycleTime)
            {
                OnButtonDown(0, MLInput.Controller.Button.Bumper);
                _cycleTime = Time.time + 10;
            }
            #endif
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>ControllerData</b></color>\nStatus: {0}\n", ControllerStatus.Text);

            _statusText.text += string.Format("\n<color=#dbfb76><b>Active Media Player</b></color>\n{0}\n",
                _mediaPlayerExamplePrefabs[_mediaPlayerExamplePrefabIndex].name);
        }

        /// <summary>
        /// Subscribe to button down event when enabled.
        /// </summary>
        void OnEnable()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += OnButtonDown;
            #endif
        }

        /// <summary>
        /// Unsubscribe to button down event when enabled.
        /// </summary>
        void OnDisable()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= OnButtonDown;
            #endif
        }

        /// <summary>
        /// Handles the event for button down. Cycle through known media player examples when bumper is pressed.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (MLInput.Controller.Button.Bumper == button)
            {
                if (_mediaPlayerExamplePrefabs[_mediaPlayerExamplePrefabIndex])
                {
                    _mediaPlayerExamplePrefabs[_mediaPlayerExamplePrefabIndex].SetActive(false);
                }

                _mediaPlayerExamplePrefabIndex = (_mediaPlayerExamplePrefabIndex + 1) % _mediaPlayerExamplePrefabs.Length;

                if (_mediaPlayerExamplePrefabs[_mediaPlayerExamplePrefabIndex])
                {
                    _mediaPlayerExamplePrefabs[_mediaPlayerExamplePrefabIndex].SetActive(true);
                }
            }
        }
    }
}

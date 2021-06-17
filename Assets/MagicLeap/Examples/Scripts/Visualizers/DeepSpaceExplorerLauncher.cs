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
using MagicLeap.Core;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class is responsible for creating the planet, moving the planet, and destroying the planet
    /// as well as creating the explorers.
    /// </summary>
    public class DeepSpaceExplorerLauncher : MonoBehaviour
    {
        [SerializeField, Tooltip("The MLImageTrackerBehavior that will be subscribed to.")]
        private MLImageTrackerBehavior _imageTracker = null;

        [SerializeField, Tooltip("Position offset of the explorer's target relative to Reference Transform")]
        private Vector3 _positionOffset = Vector3.zero;

        [Header("Planet")]
        [SerializeField, Tooltip("Prefab of the Planet")]
        private Animator _planetPrefabAnimator = null;
        private Transform _planetInstance = null;
        private Vector3 _planetVel = Vector3.zero;

        [Header("Explorer")]
        [SerializeField, Tooltip("Prefab of the Deep Space Explorer")]
        private GameObject _explorerPrefab = null;

        private float _timeLastLaunch = 0;
        [SerializeField, Tooltip("Time interval between instances (seconds)")]
        private float _timeInterval = 0.5f;

        [SerializeField, Tooltip("Minimum distance from the center of the planet")]
        private float _minOrbitRadius = 0.1f;
        [SerializeField, Tooltip("Maximum distance from the center of the planet")]
        private float _maxOrbitRadius = 0.2f;

        /// <summary>
        /// Validates fields and subscribes to the _imageTracker callbacks.
        /// </summary>
        void Awake()
        {
            if (_planetPrefabAnimator == null)
            {
                Debug.LogError("Error: DeepSpaceExplorerLauncher._planetPrefabAnimator is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_explorerPrefab == null)
            {
                Debug.LogError("Error: DeepSpaceExplorerLauncher._explorerPrefab is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_imageTracker == null)
            {
                Debug.LogError("Error: DeepSpaceExplorerLauncher._imageTracker is not set, disabling script.");
                enabled = false;
                return;
            }

            _planetInstance = Instantiate(_planetPrefabAnimator.transform, GetPosition(), Quaternion.identity);
            _planetInstance.gameObject.SetActive(false);
            gameObject.SetActive(false);

            #if PLATFORM_LUMIN
            _imageTracker.OnTargetFound += (MLImageTracker.Target target, MLImageTracker.Target.Result result) => { gameObject.SetActive(true); _planetInstance.gameObject.SetActive(true); _planetInstance.position = GetPosition(); };
            _imageTracker.OnTargetLost += (MLImageTracker.Target target, MLImageTracker.Target.Result result) => { gameObject.SetActive(false); _planetInstance.gameObject.SetActive(false); };
            #endif
        }

        /// <summary>
        /// Update planet position and launch explorers.
        /// </summary>
        void Update()
        {
            Vector3 position = GetPosition();

            // Update planet position.
            _planetInstance.position = Vector3.SmoothDamp(_planetInstance.position, position, ref _planetVel, 1.0f);

            // Launch explorers.
            if (Time.time - _timeInterval > _timeLastLaunch)
            {
                _timeLastLaunch = Time.time;
                GameObject explorer = Instantiate(_explorerPrefab, position, Random.rotation);
                DeepSpaceExplorerController explorerController = explorer.GetComponent<DeepSpaceExplorerController>();
                if (explorerController)
                {
                    explorer.transform.parent = _planetInstance;
                    explorerController.OrbitRadius = Random.Range(_minOrbitRadius, _maxOrbitRadius);
                }
            }
        }

        /// <summary>
        /// Calculate and return the position which the explorers should look at.
        /// </summary>
        /// <returns>The absolute position of the new target</returns>
        private Vector3 GetPosition()
        {
            return transform.position + transform.TransformDirection(_positionOffset);
        }
    }
}

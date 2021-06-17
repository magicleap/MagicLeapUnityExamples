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
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// Updates followers to face this object.
    /// </summary>
    public class DeepSeaExplorerLauncher : MonoBehaviour
    {
        #pragma warning disable 414
        [SerializeField, Tooltip("The MLImageTrackerBehavior that will be subscribed to.")]
        private MLImageTrackerBehavior _imageTracker = null;
        #pragma warning restore 414

        [SerializeField, Tooltip("Position offset of the explorer's target relative to this transform.")]
        private Vector3 _positionOffset = Vector3.zero;

        [SerializeField, Tooltip("Prefab of the Deep Sea Explorer.")]
        private GameObject _explorerPrefab = null;
        private FaceTargetPosition[] _followers = null;

        [SerializeField, Tooltip("Desired number of explorers. Each explorer will have a different mass and turning speed combination.")]
        private int _numExplorers = 3;
        private float _minMass = 4;
        private float _maxMass = 16;
        private float _minTurningSpeed = 30;
        private float _maxTurningSpeed = 90;

        /// <summary>
        /// Validates fields and creates the deep sea explorers.
        /// </summary>
        void Awake ()
        {
            if (_explorerPrefab == null)
            {
                Debug.LogError("Error: DeepSeaExplorerLauncher._deepSeaExplorer is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            _imageTracker.OnTargetFound += (MLImageTracker.Target target, MLImageTracker.Target.Result result) => { gameObject.SetActive(true); };
            _imageTracker.OnTargetLost += (MLImageTracker.Target target, MLImageTracker.Target.Result result) => { gameObject.SetActive(false); };
            #endif

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Recreates explorers if we are reenabled while a target is found.
        /// </summary>
        void OnEnable()
        {
            CreateExplorers();
        }

        /// <summary>
        /// Destroys all explorers immediately.
        /// </summary>
        void OnDisable()
        {
            if(_followers != null)
            {
                DestroyExplorers();
            }
        }

        /// <summary>
        /// Updates followers of the new position.
        /// </summary>
        void Update()
        {
            Vector3 position = GetPosition();
            foreach (FaceTargetPosition follower in _followers)
            {
                if (follower)
                {
                    follower.TargetPosition = position;
                }
            }
        }

        /// <summary>
        /// Creates the Deep Sea Explorers with unique parameters.
        /// </summary>
        private void CreateExplorers()
        {
            if (_followers == null)
            {
                _followers = new FaceTargetPosition[_numExplorers];
            }

            float massInc = (_maxMass - _minMass) / _numExplorers;
            float turningSpeedInc = (_maxTurningSpeed - _minTurningSpeed) / _numExplorers;
            Vector3 position = GetPosition();
            for (int i = 0; i < _numExplorers; ++i)
            {
                if (_followers[i])
                {
                    continue;
                }

                GameObject explorer = Instantiate(_explorerPrefab, position, Quaternion.identity);

                _followers[i] = explorer.AddComponent<FaceTargetPosition>();
                _followers[i].TurningSpeed = _minTurningSpeed + (i * turningSpeedInc);

                // Mass would be inversely proportional to turning speed (lower mass leads to lower acceleration -> needs higher turning rate).
                Rigidbody body = explorer.GetComponent<Rigidbody>();
                if (body)
                {
                    body.mass = _maxMass - (i * massInc);
                }
            }
        }

        /// <summary>
        /// Destroys all explorers.
        /// </summary>
        private void DestroyExplorers()
        {
            foreach (FaceTargetPosition follower in _followers)
            {
                if (follower)
                {
                    Destroy(follower.gameObject);
                }
            }
        }

        /// <summary>
        /// Calculates and return the position which the explorers should look at.
        /// </summary>
        private Vector3 GetPosition()
        {
            // The absolute position of the new target.
            return transform.position + transform.TransformDirection(_positionOffset);
        }
    }
}

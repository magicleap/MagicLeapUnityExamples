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
    public class HandMeshingExample : MonoBehaviour
    {
        /// <summary>
        /// Different Render Modes for the Hand Meshing Example
        /// </summary>
        public enum RenderMode : uint
        {
            Occlusion,
            Flat,
            Wireframe,
            Paused
        }

        [SerializeField, Tooltip("The Hand Meshing Behavior to control")]
        private MLHandMeshingBehavior _behavior = null;

        [SerializeField, Tooltip("Material used in Occlusion Render Mode")]
        private Material _occlusionMaterial = null;

        [SerializeField, Tooltip("Material used in Flat Render Mode")]
        private Material _flatMaterial = null;

        [SerializeField, Tooltip("Material used in Wireframe Render Mode")]
        private Material _wireframeMaterial = null;

        [SerializeField, Tooltip("Duration, in seconds, to hold key pose before changing render modes"), Min(1.0f)]
        private float _secondsBetweenModes = 2;

        [SerializeField, Tooltip("Key Pose to switch render modes")]
        private MLHandTracking.HandKeyPose _keyposeToSwitch = MLHandTracking.HandKeyPose.Ok;

        [SerializeField, Tooltip("Status Text")]
        private Text _statusText = null;

        [SerializeField, Tooltip("Switching tooltip text")]
        private TextMesh _switchTooltip = null;

        private const float _minimumConfidence = 0.8f;
        private float _timer = 0;
        private RenderMode _mode = RenderMode.Occlusion;

        /// <summary>
        /// Validate and initialize properties
        /// </summary>
        void Start()
        {
            if (_behavior == null)
            {
                Debug.LogError("Error: HandMeshingExample._behavior is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_occlusionMaterial == null)
            {
                Debug.LogError("Error: HandMeshingExample._occlusionMaterial is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_flatMaterial == null)
            {
                Debug.LogError("Error: HandMeshingExample._flatMaterial is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_wireframeMaterial == null)
            {
                Debug.LogError("Error: HandMeshingExample._wireframeMaterial is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: HandMeshingExample._status is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_switchTooltip == null)
            {
                Debug.LogError("Error: HandMeshingExample._switchTooltip is not set, disabling script.");
                enabled = false;
                return;
            }
            _switchTooltip.gameObject.SetActive(false);

#if PLATFORM_LUMIN
            MLHandTracking.KeyPoseManager.EnableKeyPoses(new MLHandTracking.HandKeyPose[] { _keyposeToSwitch }, true);
            MLHandTracking.KeyPoseManager.SetPoseFilterLevel(MLHandTracking.PoseFilterLevel.ExtraRobust);
            MLHandTracking.KeyPoseManager.SetKeyPointsFilterLevel(MLHandTracking.KeyPointFilterLevel.ExtraSmoothed);
#endif

            _timer = _secondsBetweenModes;
        }

        void OnDestroy()
        {
        }

        /// <summary>
        /// Updates timer and render mode
        /// </summary>
        void Update()
        {
            UpdateStatusText();

            if (!IsSwitchingModes())
            {
                _timer = _secondsBetweenModes;
                _switchTooltip.gameObject.SetActive(false);
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0)
            {
                _switchTooltip.gameObject.SetActive(true);
                UpdateSwitchTooltip();
                return;
            }

            _timer = _secondsBetweenModes;
            _mode = GetNextRenderMode();

            UpdateHandMeshingBehavior();
        }

        private bool IsSwitchingModes()
        {
#if PLATFORM_LUMIN
            return (MLHandTracking.Right.KeyPose == _keyposeToSwitch && MLHandTracking.Right.HandKeyPoseConfidence > _minimumConfidence);
#else
            return false;
#endif
        }

        private void UpdateStatusText()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data </b></color>\nStatus: {0}\n",
                ControllerStatus.Text);

            _statusText.text += string.Format("\n<color=#dbfb76><b>Hand Mesh Data </b></color>\n Current Render Mode: <color=green>{0}</color>", _mode.ToString());
        }

        private RenderMode GetNextRenderMode()
        {
            return (_mode == RenderMode.Paused) ? RenderMode.Occlusion : (RenderMode)((uint)_mode + 1);
        }

        private void UpdateSwitchTooltip()
        {
#if PLATFORM_LUMIN
            _switchTooltip.transform.position = MLHandTracking.Right.Thumb.KeyPoints[0].Position;
#endif

            _switchTooltip.text = string.Format("Switching to<color=yellow>{0}</color> In {1} seconds",
                GetNextRenderMode(),
                _timer.ToString("0.0"));
        }

        private void UpdateHandMeshingBehavior()
        {
            switch (_mode)
            {
                case RenderMode.Occlusion:
                    _behavior.enabled = true;
                    _behavior.MeshMaterial = _occlusionMaterial;
                    break;
                case RenderMode.Flat:
                    _behavior.MeshMaterial = _flatMaterial;
                    break;
                case RenderMode.Wireframe:
                    _behavior.MeshMaterial = _wireframeMaterial;
                    break;
                case RenderMode.Paused:
                    _behavior.enabled = false;
                    break;
            }
        }
    }
}

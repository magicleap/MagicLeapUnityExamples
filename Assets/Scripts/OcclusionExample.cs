// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using MagicLeap.OpenXR.Features.PhysicalOcclusion;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace MagicLeap.Examples
{
    public class OcclusionExample : MonoBehaviour
    {
        [SerializeField] private Toggle enableOcclusion;
        [SerializeField] private Toggle handsToggle;
        [SerializeField] private Toggle controllerToggle;
        [SerializeField] private Toggle environmentToggle;
        [SerializeField] private Toggle depthSensorToggle;
        [SerializeField] private Slider nearRangeSlider;
        [SerializeField] private Slider farRangeSlider;
        [SerializeField] private Text nearRangeLabel;
        [SerializeField] private Text farRangeLabel;
        private MagicLeapPhysicalOcclusionFeature occlusionFeature;
        private MagicLeapController Controller => MagicLeapController.Instance;

        #region UI
        public void UpdateNearRange()
        {
            nearRangeSlider.value = Mathf.Min(nearRangeSlider.value, farRangeSlider.value);
            UpdateDepthSensorValues();
        }

        public void UpdateFarRange()
        {
            farRangeSlider.value = Mathf.Max(nearRangeSlider.value, farRangeSlider.value);
            UpdateDepthSensorValues();
        }

        private void UpdateDepthSensorValues()
        {
            occlusionFeature.DepthSensorNearRange = nearRangeSlider.value;
            nearRangeLabel.text = nearRangeSlider.value.ToString("F2");
            occlusionFeature.DepthSensorFarRange = farRangeSlider.value;
            farRangeLabel.text = farRangeSlider.value.ToString("F2");
        }
        #endregion

        private void OnDestroy()
        {
            enableOcclusion.onValueChanged.RemoveAllListeners();
            Controller.BumperPressed -= ResetOcclusionSources;
            occlusionFeature.EnableOcclusion = false;
        }

        private void Start()
        {
            occlusionFeature = OpenXRSettings.Instance.GetFeature<MagicLeapPhysicalOcclusionFeature>();
            if (occlusionFeature == null || !occlusionFeature.enabled)
            {
                Debug.LogError($"{nameof(MagicLeapPhysicalOcclusionFeature)} is not enabled");
                enabled = false;
                return;
            }

            enableOcclusion.onValueChanged.AddListener((value)=> occlusionFeature.EnableOcclusion = value);
            Controller.BumperPressed += ResetOcclusionSources;
            occlusionFeature.EnableOcclusion = true;
            var (nearRange,farRange) = occlusionFeature.GetDepthSensorProperties();
            nearRangeSlider.value = nearRange.min;
            farRangeSlider.value = farRange.min;
            UpdateSources();
        }

        private void ResetOcclusionSources(InputAction.CallbackContext _)
        {
            handsToggle.isOn = true;
            controllerToggle.isOn = true;
            environmentToggle.isOn = false;
            depthSensorToggle.isOn = false;
            UpdateSources();
        }

        public void UpdateSources()
        {
            occlusionFeature.EnabledOcclusionSource = handsToggle.isOn ? MagicLeapPhysicalOcclusionFeature.OcclusionSource.Hands : 0;
            occlusionFeature.EnabledOcclusionSource |= controllerToggle.isOn ? MagicLeapPhysicalOcclusionFeature.OcclusionSource.Controller : 0;
            occlusionFeature.EnabledOcclusionSource |= environmentToggle.isOn ? MagicLeapPhysicalOcclusionFeature.OcclusionSource.Environment : 0;
            occlusionFeature.EnabledOcclusionSource |= depthSensorToggle.isOn ? MagicLeapPhysicalOcclusionFeature.OcclusionSource.DepthSensor : 0;
        }
    }
}

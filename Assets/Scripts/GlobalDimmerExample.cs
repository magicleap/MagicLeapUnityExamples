// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features;

namespace MagicLeap.Examples
{
    public class GlobalDimmerExample : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        [SerializeField]
        private Text toggleText;

        [SerializeField]
        private Text status;

        private MagicLeapRenderingExtensionsFeature renderFeature;

        private void Start()
        {
            if (!OpenXRRuntime.IsExtensionEnabled("XR_ML_global_dimmer"))
            {
                status.text = $"The OpenXR extension \"XR_ML_global_dimmer\" is not enabled so this example will not function!\n\n" +
                    $"You must enable the OpenXR Feature \"Magic Leap 2 Rendering Extensions\" in Project Settings to use the Global Dimmer.";
                gameObject.SetActive(false);
                return;
            }

            renderFeature = OpenXRSettings.Instance.GetFeature<MagicLeapRenderingExtensionsFeature>();
            renderFeature.GlobalDimmerEnabled = true;

            status.text = $"Global Dimmer Value: {renderFeature.GlobalDimmerValue:P}\n\nGlobal Dimmer Enabled: {renderFeature.GlobalDimmerEnabled}";
            slider.value = renderFeature.GlobalDimmerValue;
        }

        private void OnDestroy()
        {
            if (renderFeature != null)
                renderFeature.GlobalDimmerEnabled = false;
        }

        public void SetDimmerValue(float value)
        {
            if (renderFeature != null)
                renderFeature.GlobalDimmerValue = value;

            status.text = $"Global Dimmer Value: {renderFeature.GlobalDimmerValue:P}\n\nGlobal Dimmer Enabled: {renderFeature.GlobalDimmerEnabled}";
        }

        public void ToggleDimmer()
        {
            renderFeature.GlobalDimmerEnabled = !renderFeature.GlobalDimmerEnabled;
            string val = renderFeature.GlobalDimmerEnabled ? "On" : "Off";
            toggleText.text = $"Dimmer {val}";
            status.text = $"Global Dimmer Value: {renderFeature.GlobalDimmerValue:P}\n\nGlobal Dimmer Enabled: {renderFeature.GlobalDimmerEnabled}";
        }
    }
}

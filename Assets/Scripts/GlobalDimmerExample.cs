using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;

namespace MagicLeap.Examples
{
    public class GlobalDimmerExample : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float startingValue = 0.2f;

        [SerializeField]
        private Slider slider;

        [SerializeField]
        private Text toggleText;

        [SerializeField]
        private Text instructions;

        private MagicLeapRenderingExtensionsFeature renderFeature;

        private void Start()
        {
            if (!OpenXRRuntime.IsExtensionEnabled("XR_ML_global_dimmer"))
            {
                instructions.text = $"The OpenXR extension \"XR_ML_global_dimmer\" is not enabled so this example will not function!\n\n" +
                    $"You must enable the OpenXR Feature \"Magic Leap 2 Rendering Extensions\" in Project Settings to use the Global Dimmer.";
                gameObject.SetActive(false);
                return;
            }

            renderFeature = OpenXRSettings.Instance.GetFeature<MagicLeapRenderingExtensionsFeature>();
            slider.value = startingValue;
            renderFeature.GlobalDimmerEnabled = true;
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
        }

        public void ToggleDimmer()
        {
            renderFeature.GlobalDimmerEnabled = !renderFeature.GlobalDimmerEnabled;
            string status = renderFeature.GlobalDimmerEnabled ? "On" : "Off";
            toggleText.text = $"Dimmer {status}";
        }
    }
}

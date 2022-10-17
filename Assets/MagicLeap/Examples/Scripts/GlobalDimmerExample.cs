using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class GlobalDimmerExample : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float startingValue = 0.2f;

        [SerializeField]
        private UnityEngine.UI.Slider slider;

        private void Start()
        {
            slider.value = startingValue;
        }

        public void SetDimmerValue(float value)
        {
            MLGlobalDimmer.SetValue(value);
        }
    }
}

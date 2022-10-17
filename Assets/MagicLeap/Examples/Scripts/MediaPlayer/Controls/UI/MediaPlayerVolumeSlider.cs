using System;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    public class MediaPlayerVolumeSlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public event Action<float> OnVolumeChanged;

        public float Volume => slider.value;
        
        private void OnEnable()
        {
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(float arg0)
        {
            OnVolumeChanged?.Invoke(arg0);
        }

        public void SetValueWithoutNotifying(float startingVolume)
        {
            slider.SetValueWithoutNotify(startingVolume);
        }

        public void SetValue(float value)
        {
            slider.value = value;
        }
    }
}
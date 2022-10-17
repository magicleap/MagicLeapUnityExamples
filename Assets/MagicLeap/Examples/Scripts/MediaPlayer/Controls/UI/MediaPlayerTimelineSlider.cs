using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    public class MediaPlayerTimelineSlider : MonoBehaviour , IEndDragHandler, IBeginDragHandler
    {
        [SerializeField] private Slider slider;

        [SerializeField] private MediaPlayerTimelineHandle handle;
        private bool _isDragging;
        private float _cachedValue;

        public event Action<float> OnTimelineChanged;

        public float Timeline => slider.value;
        
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
            if (_isDragging)
                _cachedValue = arg0;
            else
                OnTimelineChanged?.Invoke(arg0);
        }

        public void SetValueWithoutNotifying(float startingVolume)
        {
            if (_isDragging)
                return;
            
            slider.SetValueWithoutNotify(startingVolume);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            OnValueChanged(_cachedValue);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }
    }
}
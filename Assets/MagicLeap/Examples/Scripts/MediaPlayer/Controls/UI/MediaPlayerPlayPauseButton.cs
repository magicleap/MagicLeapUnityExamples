using System;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    public class MediaPlayerPlayPauseButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        [SerializeField] private Image image;
        
        [SerializeField, Tooltip("Play Material")]
        private Sprite playMaterial = null;

        [SerializeField, Tooltip("Pause Material")]
        private Sprite pauseMaterial = null;

        [SerializeField] private bool isPlay;

        public event Action OnClicked;

        private void Start()
        {
            UpdateGraphic();
        }

        private void UpdateGraphic()
        {
            image.sprite = isPlay ? playMaterial : pauseMaterial;
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }
        
        private void OnClick()
        {
            UpdateGraphic();
            OnClicked?.Invoke();
        }

        public void SetValue(bool value)
        {
            isPlay = value;
            UpdateGraphic();
        }
    }
}

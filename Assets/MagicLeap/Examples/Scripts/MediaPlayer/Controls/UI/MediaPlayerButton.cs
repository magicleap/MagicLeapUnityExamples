using System;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    public class MediaPlayerButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        public event Action OnButtonClick;
        
        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            OnButtonClick?.Invoke();
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }
        
    }
}
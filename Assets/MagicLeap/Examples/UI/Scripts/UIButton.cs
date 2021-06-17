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

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicLeap
{
    [RequireComponent(typeof(EventTrigger))]
    public class UIButton : MonoBehaviour
    {
        [SerializeField, Tooltip("An array of button effects that will be applied for each button state.")]
        ButtonEffect[] _buttonEffects = null;

        [SerializeField, Tooltip("An (optional) effect that will be applied when the button is being hovered.")]
        private Image _hoverImage = null;

        [SerializeField, Tooltip("When enabled the active state will toggle when the button is pressed.")]
        private bool _activeToggle = false;

        [SerializeField, Tooltip("Ignores content and behaves as it would have it")]
        private bool ignoreContent = false;

        [SerializeField, Tooltip("An (optional) GameObject that will be shown when active.")]
        private GameObject _activeContent = null;

        protected UIButtonGroup _buttonGroup = null;
        protected bool _isActive = false;
        protected bool _isHover = false;

        // The last known state before the button was disabled.
        private bool _wasActive = false;

        /// <summary>
        /// The current active state of the button.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
        }

        /// <summary>
        /// Assign the default button effects.
        /// </summary>
        private void Start()
        {
            InitializeButtons();
            Default();
        }

        /// <summary>
        /// Close the button and disable the content, while keeping a record of the state.
        /// </summary>
        private void OnDisable()
        {
            _wasActive = _isActive;
            Default(true);
        }

        /// <summary>
        /// Check the previous state and restore the content if it was active.
        /// </summary>
        private void OnEnable()
        {
            if(_wasActive)
            {
                Pressed();
            }
        }

        /// <summary>
        /// This occurs when the button does not have any interactions.
        /// </summary>
        /// <param name="reset">When true, forces the active status of the button to be reset.</param>
        public virtual void Default(bool reset = false)
        {
            if (reset)
            {
                _isActive = false;
            }

            if (IsActiveWithContent())
            {
                return;
            }

            _isHover = false;

            for (int i = 0; i < _buttonEffects.Length; i++)
            {
                if (_buttonEffects[i].Image != null)
                {
                    _buttonEffects[i].Image.color = _buttonEffects[i].DefaultColor;
                }

                if (_buttonEffects[i].Text != null)
                {
                    _buttonEffects[i].Text.color = _buttonEffects[i].DefaultColor;
                }
            }

            ShowActiveContent(_isActive);
            ShowHoverImage(_isHover);
        }

        /// <summary>
        /// This occurs when the button is being hovered.
        /// </summary>
        public virtual void Hover()
        {
            _isHover = true;

            if (IsActiveWithContent())
            {
                return;
            }

            for (int i = 0; i < _buttonEffects.Length; i++)
            {
                if (_buttonEffects[i].Image != null)
                {
                    _buttonEffects[i].Image.color = _buttonEffects[i].HoverColor;
                }

                if (_buttonEffects[i].Text != null)
                {
                    _buttonEffects[i].Text.color = _buttonEffects[i].HoverColor;
                }
            }

            ShowHoverImage(_isHover);
        }

        /// <summary>
        /// This occurs when the button is pressed.
        /// </summary>
        public virtual void Pressed()
        {
            // If the button is already pressed, toggle the state and hide content.
            if(_isActive)
            {
                Default(true);
                return;
            }

            if (_buttonGroup != null)
            {
                _buttonGroup.Clear();
            }

            // When enabled, allow the button active state to be toggled.
            if (_activeToggle)
            {
                _isActive = !_isActive;
            }

            // When there is no content to display, reset the button state.
            if (_activeContent == null)
            {
                StartCoroutine(ResetEffect());
            }

            ShowHoverImage(_isHover);

            for (int i = 0; i < _buttonEffects.Length; i++)
            {
                if (_buttonEffects[i].Image != null)
                {
                    _buttonEffects[i].Image.color = _buttonEffects[i].PressedColor;
                }

                if (_buttonEffects[i].Text != null)
                {
                    _buttonEffects[i].Text.color = _buttonEffects[i].PressedColor;
                }
            }

            ShowActiveContent(_isActive);
        }

        /// <summary>
        /// Enables a previously inactive button and forces the state to active.
        /// </summary>
        public void ForceActive()
        {
            _isActive = false;
            _wasActive = true;

            gameObject.SetActive(true);

            Pressed();
        }

        protected void InitializeButtons()
        {
            _buttonGroup = GetComponentInParent<UIButtonGroup>();
            if (_buttonGroup != null)
            {
                _buttonGroup.AddUIButton(this);
            }
        }

        private void ShowActiveContent(bool visible)
        {
            if (_activeContent != null)
            {
                _activeContent.SetActive(visible);
            }
        }

        /// <summary>
        /// Updates the visibility of an (optional) hover image.
        /// </summary>
        /// <param name="visible">The desired visiblity of the image.</param>
        private void ShowHoverImage(bool visible)
        {
            if (_hoverImage != null)
            {
                _hoverImage.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Check if the button is active and has content assigned.
        /// </summary>
        /// <returns></returns>
        private bool IsActiveWithContent()
        {
            return _isActive && (ignoreContent || _activeContent != null);
        }

        /// <summary>
        /// After a brief duration restore the previous button state.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ResetEffect()
        {
            yield return new WaitForSeconds(0.1f);

            if(_isHover)
            {
                Hover();
            }
        }

        /// <summary>
        /// This class contains information about an individual button effect.
        /// The effect can be assigned to an image, text, or both.
        /// </summary>
        [System.Serializable]
        public class ButtonEffect
        {
            [SerializeField, Tooltip("The text element that should receive the effect.")]
            private Text _text = null;

            [SerializeField, Tooltip("The image element that should receive the effect.")]
            private Image _image = null;

            [SerializeField, Tooltip("The default color for the button effect.")]
            private Color _default = Color.white;

            [SerializeField, Tooltip("The hover color for the button effect.")]
            private Color _hover = Color.white;

            [SerializeField, Tooltip("The pressed color for the button effect.")]
            private Color _pressed = Color.white;

            /// <summary>
            /// The assigned text component for the effect.
            /// </summary>
            public Text Text
            {
                get { return _text; }
            }

            /// <summary>
            /// The assigned image component for the effect.
            /// </summary>
            public Image Image
            {
                get { return _image; }
            }

            /// <summary>
            /// The default effect color.
            /// </summary>
            public Color DefaultColor
            {
                get
                {
                    return _default;
                }
            }

            /// <summary>
            /// The hover effect color.
            /// </summary>
            public Color HoverColor
            {
                get
                {
                    return _hover;
                }
            }

            /// <summary>
            /// The pressed effect color.
            /// </summary>
            public Color PressedColor
            {
                get
                {
                    return _pressed;
                }
            }
        }
    }
}

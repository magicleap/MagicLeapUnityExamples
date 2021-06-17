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

namespace MagicLeap
{
    /// <summary>
    /// This class extends the UIButton to provide a toggle operation.
    /// </summary>
    public class UIToggleButton : UIButton
    {
        [SerializeField, Tooltip("This is the icon (GameObject) that will be enabled by default.")]
        private GameObject _defaultIcon = null;

        [SerializeField, Tooltip("This is the icon (GameObject) that will be enabled for the active state.")]
        private GameObject _activeIcon = null;

        [SerializeField, Tooltip("This is the icon (GameObject) that will be enabled when the button is disabled.")]
        private GameObject _disabledIcon = null;

        [SerializeField, Tooltip("This optional (GameObject) will be shown when the button is being hovered.")]
        private GameObject _details = null;

        [SerializeField, Tooltip("Is it allowed to switch off this toggle")]
        private bool allowSwitchOff = true;

        private bool _disabled = false;

        /// <summary>
        /// Set the default button state.
        /// </summary>
        private void Start()
        {
            InitializeButtons();
            Default(true);
        }

        /// <summary>
        /// This occurs when the button does not have any interactions.
        /// </summary>
        /// <param name="reset">When true, forces the active status of the button to be reset.</param>
        public override void Default(bool reset = false)
        {
            if (reset)
            {
                _disabled = false;
            }

            if (_disabled)
            {
                return;
            }

            base.Default(reset);

            UpdateSprites();
            ShowDetails(_isHover);
        }

        /// <summary>
        /// This occurs when the button is being hovered.
        /// </summary>
        public override void Hover()
        {
            if (_disabled)
            {
                return;
            }

            base.Hover();

            ShowDetails(_isHover);
        }

        /// <summary>
        /// This occurs when the button is pressed.
        /// </summary>
        public override void Pressed()
        {
            if (_disabled)
            {
                return;
            }
            if (IsActive && !allowSwitchOff)
            {
                return;
            }

            base.Pressed();

            UpdateSprites();
            ShowDetails(_isHover);
        }

        public void ToggleButtonEnabled()
        {
            _disabled = !_disabled;

            UpdateSprites();
        }

        /// <summary>
        /// Shows additional information about the button.
        /// </summary>
        /// <param name="active"></param>
        private void ShowDetails(bool active)
        {
            if (_details != null)
            {
                _details.SetActive(active);
            }
        }

        private void UpdateSprites()
        {
            if (_disabledIcon != null)
            {
                _disabledIcon.SetActive(_disabled);
            }

            if (_defaultIcon != null)
            {
                _defaultIcon.SetActive(!_disabled && !_isActive);
            }

            if (_activeIcon != null)
            {
                _activeIcon.SetActive(!_disabled && _isActive);
            }
        }
    }
}

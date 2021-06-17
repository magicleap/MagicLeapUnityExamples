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
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class provides callbacks and manages the state of the Virtual Keyboard.
    /// </summary>
    public class VirtualKeyboard : MonoBehaviour
    {
        [System.Serializable]
        public class KeyboardCancelEvent : UnityEvent { }

        [System.Serializable]
        public class KeyboardSubmitEvent : UnityEvent<string> { }

        #pragma warning disable 414
        [SerializeField, Tooltip("A reference to the controller connection handler in the scene.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;
        #pragma warning restore 414

        [SerializeField, Tooltip("The preview field for the typed text.")]
        private Text _inputField = null;

        [Header("Keyboard Layouts")]

        [SerializeField, Tooltip("The GameObject for the lowercase version of the keyboard.")]
        private GameObject _lowercaseKeyboard = null;

        [SerializeField, Tooltip("The GameObject for the uppercase version of the keyboard.")]
        private GameObject _uppercaseKeyboard = null;

        [SerializeField, Tooltip("The GameObject for the numeric version of the keyboard.")]
        private GameObject _numericKeyboard = null;

        [SerializeField, Tooltip("The GameObject for the numeric (shift) version of the keyboard.")]
        private GameObject _numericShiftKeyboard = null;

        [Space, Tooltip("The event that occurs when the keyboard is canceled.")]
        public KeyboardCancelEvent OnKeyboardCancel = null;

        [Space, Tooltip("The event that occurs when the keyboard is submitted.")]
        public KeyboardSubmitEvent OnKeyboardSubmit = null;

        private bool _shift = false;
        private bool _alternate = false;

        /// <summary>
        /// Appends a string to the end of the input field text.
        /// </summary>
        /// <param name="character"></param>
        public void InsertCharacter(string character)
        {
            _inputField.text += character;
        }

        /// <summary>
        /// Toggles the active keyboard between (a-Z) and (Alphanumeric)
        /// </summary>
        public void ToggleKeyboard()
        {
            _alternate = !_alternate;

            UpdateKeyboard();
        }

        /// <summary>
        /// Toggles the shift state of the currently active keyboard.
        /// </summary>
        public void ToggleShift()
        {
            _shift = !_shift;

            UpdateKeyboard();
        }

        /// <summary>
        /// If a ControllerConnectionHandler is assigned, the Bump feedback pattern will be sent to the active controller.
        /// </summary>
        public void Hover()
        {
            #if PLATFORM_LUMIN
            if (_controllerConnectionHandler != null && _controllerConnectionHandler.ConnectedController != null)
            {
                _controllerConnectionHandler.ConnectedController.StartFeedbackPatternVibe(MLInput.Controller.FeedbackPatternVibe.Bump, MLInput.Controller.FeedbackIntensity.Low);
            }
            #endif
        }

        /// <summary>
        /// Deletes the last element from the input field text.
        /// </summary>
        public void Delete()
        {
            if (_inputField.text.Length <= 0)
            {
                return;
            }

            _inputField.text = _inputField.text.Remove(_inputField.text.Length - 1);
        }

        /// <summary>
        /// Appends a space to the end of the input field text.
        /// </summary>
        public void Space()
        {
            _inputField.text += " ";
        }

        /// <summary>
        /// Adds a NewLine to the input field text.
        /// </summary>
        public void Return()
        {
            _inputField.text += System.Environment.NewLine;
        }

        public void Open()
        {
            // Clear any existing strings.
            _inputField.text = string.Empty;

            if(!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Invokes the Cancel event for the Virtual Keyboard.
        /// </summary>
        public void Cancel()
        {
            OnKeyboardCancel?.Invoke();
        }

        /// <summary>
        /// Invokes the Submit event for the Virtual Keyboard.
        /// </summary>
        public void Submit()
        {
            OnKeyboardSubmit?.Invoke(_inputField.text);
        }

        private void UpdateKeyboard()
        {
            // a-Z Keyboards
            _lowercaseKeyboard.SetActive(!_alternate && !_shift);
            _uppercaseKeyboard.SetActive(!_alternate && _shift);

            // Alphanumeric Keyboards
            _numericKeyboard.SetActive(_alternate && !_shift);
            _numericShiftKeyboard.SetActive(_alternate && _shift);
        }
    }
}

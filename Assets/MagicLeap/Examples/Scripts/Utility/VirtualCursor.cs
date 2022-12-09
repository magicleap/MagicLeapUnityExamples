// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class represents an MLA-controlled 2D cursor.
    /// This works hand-in-hand with ContactsButtonVisualizer.
    /// </summary>
    public class VirtualCursor : MonoBehaviour
    {
#pragma warning disable 414
        [SerializeField, Tooltip("Sensitivity"), Range(-1, 1)]
        private float _sensitivity = 1.0f;

        private bool _touchActiveBefore = false;
#pragma warning restore 414

        private Vector2 _prevTouchPos = Vector2.zero;
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        [SerializeField, Tooltip("Tooltip text")]
        private Text _tooltip = null;

        /// <summary>
        /// Validate inspector properties and attach event handlers.
        /// </summary>
        void Awake()
        {
            if (_tooltip == null)
            {
                Debug.LogError("Error: VirtualCursor._tooltip is not set, disabling script.");
                enabled = false;
                return;
            }

            _tooltip.text = "";
            _tooltip.gameObject.SetActive(false);
        }

        private void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
        }

        private void OnDestroy()
        {
            mlInputs.Dispose();
        }

        /// <summary>
        /// Update state of touch position and buttons.
        /// </summary>
        void LateUpdate()
        {
            if (controllerActions.IsTracked.IsPressed())
            {
                UpdateTouchPosition();
            }
        }

        /// <summary>
        /// Update cursor position based on touch
        /// </summary>
        /// <param name="controller">Controller</param>
        private void UpdateTouchPosition()
        {
            if (_touchActiveBefore)
            {
                Vector2 pos = transform.localPosition;
                Vector2 currTouchPos = controllerActions.TouchpadPosition.ReadValue<Vector2>();
                pos += (currTouchPos - _prevTouchPos) * _sensitivity;
                transform.localPosition = pos;
            }
            else
            {
                _prevTouchPos = controllerActions.TouchpadPosition.ReadValue<Vector2>();
            }

            _touchActiveBefore = true;
        }
    }
}

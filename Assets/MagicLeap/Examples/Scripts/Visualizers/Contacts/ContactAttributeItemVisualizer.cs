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

using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Behavior to represent a TaggedAttribute (phone number or email address) in the Contact Page.
    /// This offloads the coordination with the UI elements.
    /// </summary>
    public class ContactAttributeItemVisualizer : MonoBehaviour
    {
        /// <summary>
        /// The Contact Page controller.
        /// Must be set when this is instantiated.
        /// </summary>
        [HideInInspector]
        public ContactPageVisualizer ContactPageVisualizer = null;

        #if PLATFORM_LUMIN
        /// <summary>
        /// Attribute being edited.
        /// </summary>
        public MLContacts.TaggedAttribute Attribute
        {
            get
            {
                MLContacts.TaggedAttribute attr = new MLContacts.TaggedAttribute();
                attr.Tag = _tagField.Text;
                attr.Value = _valueField.Text;
                return attr;
            }
            set
            {
                _tagField.Text = value.Tag;
                _valueField.Text = value.Value;
            }
        }
        #endif

        /// <summary>
        /// Index within the list.
        /// Must be set when this is instantiated.
        /// </summary>
        [HideInInspector]
        public int ListIndex = -1;

        /// <summary>
        /// Delete operation executed when the delete button is activated.
        /// </summary>
        public Action<int> DeleteCommand = null;

        [SerializeField, Tooltip("Text field for tag")]
        private ContactsTextFieldVisualizer _tagField = null;

        [SerializeField, Tooltip("Text field for value")]
        private ContactsTextFieldVisualizer _valueField = null;

        [SerializeField, Tooltip("Button to delete attribute")]
        private ContactsButtonVisualizer _delButton = null;

        /// <summary>
        /// Validate inspector properties and attach event handlers.
        /// </summary>
        void Start()
        {
            if (ContactPageVisualizer == null)
            {
                Debug.LogError("Error: ContactAttributeItemVisualizer.ContactPageVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_tagField == null)
            {
                Debug.LogError("Error: ContactAttributeItemVisualizer._tagField is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_valueField == null)
            {
                Debug.LogError("Error: ContactAttributeItemVisualizer._valueField is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_delButton == null)
            {
                Debug.LogError("Error: ContactAttributeItemVisualizer._delButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (ListIndex < 0)
            {
                Debug.LogError("Error: ContactAttributeItemVisualizer.ListIndex is invalid, disabling script.");
                enabled = false;
                return;
            }

            if (DeleteCommand == null)
            {
                Debug.LogError("Error: ContactAttributeItemVisualizer.DeleteCommand is not set, disabling script.");
                enabled = false;
                return;
            }

            _delButton.OnTap += HandleDelete;

            enabled = ContactPageVisualizer.IsEditing;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void OnDestroy()
        {
            _delButton.OnTap -= HandleDelete;
        }

        /// <summary>
        /// Updates UI when this is enabled (view mode).
        /// </summary>
        void OnEnable()
        {
            _delButton.gameObject.SetActive(true);
            _tagField.enabled = true;
            _valueField.enabled = true;
        }

        /// <summary>
        /// Updates UI when this is disabled (view mode).
        /// </summary>
        void OnDisable()
        {
            _delButton.gameObject.SetActive(false);
            _tagField.enabled = false;
            _valueField.enabled = false;
        }

        /// <summary>
        /// Handler when user wants to delete this attribute.
        /// </summary>
        private void HandleDelete()
        {
            DeleteCommand(ListIndex);
        }
    }
}

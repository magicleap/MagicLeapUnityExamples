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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;

namespace MagicLeap
{
    /// <summary>
    /// UI controller for the Contact Page. This displays a single
    /// contact (new or existing) in view or edit mode.
    /// </summary>
    public class ContactPageVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("Contacts Visualizer component.")]
        private ContactsVisualizer _contactsVisualizer = null;

        [SerializeField, Tooltip("Status text of the Visualizer component.")]
        private Text _visualizerStatusText = null;

        [SerializeField, Tooltip("Button to enable editing Contact.")]
        private ContactsButtonVisualizer _editButton = null;

        [SerializeField, Tooltip("Button to go back to List page.")]
        private ContactsButtonVisualizer _backButton = null;

        [SerializeField, Tooltip("Button to save changes to a Contact.")]
        private ContactsButtonVisualizer _saveButton = null;

        [SerializeField, Tooltip("Button to cancel saving/editing a Contact.")]
        private ContactsButtonVisualizer _cancelButton = null;

        [SerializeField, Tooltip("Field to display/edit Name")]
        private ContactsTextFieldVisualizer _nameField = null;

        [SerializeField, Tooltip("Button to add a new Phone Number.")]
        private ContactsButtonVisualizer _addPhoneButton = null;

        [SerializeField, Tooltip("Button to add a new E-mail Address.")]
        private ContactsButtonVisualizer _addEmailButton = null;

        [SerializeField, Tooltip("Prefab of a Contact Attribute Item.")]
        private ContactAttributeItemVisualizer _attributeItem = null;

        private List<GameObject> _phoneNumberGOList = new List<GameObject>();
        private List<GameObject> _emailAddressGOList = new List<GameObject>();

        #if PLATFORM_LUMIN
        // If ID is null or empty, we are adding a new contact.
        // Otherwise, we are editing an existing contact.
        private MLContacts.Contact _contact = null;
        #endif

        /// <summary>
        /// Flag to determine if editing or viewing.
        /// </summary>
        public bool IsEditing { get; private set; }

        /// <summary>
        /// Validate inspector properties and attach event handlers.
        /// </summary>
        void Start()
        {
            if (_contactsVisualizer == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._contactsVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_visualizerStatusText == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._visualizerStatusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_editButton == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._editButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_backButton == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._backButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_saveButton == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._saveButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_cancelButton == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._cancelButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_addPhoneButton == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._addPhoneButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_addEmailButton == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._addEmailButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_nameField == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._nameField is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_attributeItem == null)
            {
                Debug.LogError("Error: ContactPageVisualizer._attributeItem is not set, disabling script.");
                enabled = false;
                return;
            }

            _addPhoneButton.OnTap += HandleAddPhone;
            _addEmailButton.OnTap += HandleAddEmail;
            _editButton.OnTap += HandleBeginEdit;
            _backButton.OnTap += HandleReturnToList;
            _saveButton.OnTap += HandleSaveEdit;
            _cancelButton.OnTap += HandleCancelEdit;
        }

        /// <summary>
        /// Clean Up.
        /// </summary>
        void OnDestroy()
        {
            _addPhoneButton.OnTap -= HandleAddPhone;
            _addEmailButton.OnTap -= HandleAddEmail;
            _editButton.OnTap -= HandleBeginEdit;
            _backButton.OnTap -= HandleReturnToList;
            _saveButton.OnTap -= HandleSaveEdit;
            _cancelButton.OnTap -= HandleCancelEdit;

            DestroyListItems();
        }

        /// <summary>
        /// Handler when the user wants to add a new phone number.
        /// </summary>
        private void HandleAddPhone()
        {
            UpdateContactFromFields();

            #if PLATFORM_LUMIN
            MLContacts.TaggedAttribute attr = new MLContacts.TaggedAttribute();

            attr.Tag = "<i>Tag</i>";
            attr.Value = "<i>Number</i>";

            _contact.PhoneNumberList.Items.Add(attr);
            #endif

            Refresh();
        }

        /// <summary>
        /// Handler when the user wants to add a new email address.
        /// </summary>
        private void HandleAddEmail()
        {
            UpdateContactFromFields();

            #if PLATFORM_LUMIN
            MLContacts.TaggedAttribute attr = new MLContacts.TaggedAttribute();
            attr.Tag = "<i>Tag</i>";
            attr.Value = "<i>E-mail</i>";

            _contact.EmailAddressList.Items.Add(attr);
            #endif

            Refresh();
        }

        /// <summary>
        /// Handler when the user wants to save the contact.
        /// </summary>
        private void HandleSaveEdit()
        {
            UpdateContactFromFields();

            #if PLATFORM_LUMIN
            MLResult contactResult = MLContacts.ValidateContact(_contact);
            if (!contactResult.IsOk)
            {
                _visualizerStatusText.text  = string.Format("<color=red>{0}</color>", contactResult.ToString());
                return;
            }
            _contactsVisualizer.Contacts.SaveContact(_contact);
            #endif

            // leave the page, the MLContactsBehavior will update the
            // page depending on the result of operation
        }

        /// <summary>
        /// Cancels current edit operation.
        /// If editing a new contact, this goes back to the list page.
        /// If editing an existing contact, this reloads this page with the contact.
        /// </summary>
        private void HandleCancelEdit()
        {
            IsEditing = false;
            UpdateHeaderButtons();

            #if PLATFORM_LUMIN
            if (string.IsNullOrEmpty(_contact.ID))
            {
                _contactsVisualizer.LoadListPage();
            }
            else
            {
                _contactsVisualizer.LoadContactPage(_contact.ID);
            }
            #endif
        }

        /// <summary>
        /// Handler when user wants to go back to list page.
        /// </summary>
        private void HandleReturnToList()
        {
            _contactsVisualizer.LoadListPage();
        }

        /// <summary>
        /// Handler when user wants to edit contact.
        /// </summary>
        private void HandleBeginEdit()
        {
            IsEditing = true;
            UpdateHeaderButtons();
            Refresh();
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Build the UI from the given contact. If the contact is new,
        /// immediately go to edit mode. Otherwise, go to view mode.
        /// Called by ContactsExample.
        /// </summary>
        /// <param name="contact">Contact to load</param>
        public void Populate(MLContacts.Contact contact)
        {
            _contact = contact;

            if (string.IsNullOrEmpty(contact.ID))
            {
                _contact.Name = "<i>Name</i>";
                HandleBeginEdit();
            }
            else
            {
                IsEditing = false;
                UpdateHeaderButtons();
                Refresh();
            }
        }
        #endif

        /// <summary>
        /// Removes the Phone Number at the given index. Called by ContactAttributeItemVisualizer.
        /// </summary>
        /// <param name="listIndex">Index of Phone Number to be removed</param>
        public void RemovePhoneNumber(int listIndex)
        {
            UpdateContactFromFields();

            #if PLATFORM_LUMIN
            _contact.PhoneNumberList.Items.RemoveAt(listIndex);
            #endif

            Refresh();
        }

        /// <summary>
        /// Removes the Email Address at the given index. Called by ContactAttributeItemVisualizer.
        /// </summary>
        /// <param name="listIndex">Index of Email Address to be removed</param>
        public void RemoveEmailAddress(int listIndex)
        {
            UpdateContactFromFields();

            #if PLATFORM_LUMIN
            _contact.EmailAddressList.Items.RemoveAt(listIndex);
            #endif

            Refresh();
        }

        /// <summary>
        /// Clears out the UI.
        /// </summary>
        private void DestroyListItems()
        {
            foreach (GameObject item in _phoneNumberGOList)
            {
                Destroy(item);
            }

            foreach (GameObject item in _emailAddressGOList)
            {
                Destroy(item);
            }

            _phoneNumberGOList.Clear();
            _emailAddressGOList.Clear();
        }

        /// <summary>
        /// Deletes all the UI fields and rebuilds them based on _contact.
        /// Any unsaved changes on the fields will be lost. Call UpdateContactFromFields()
        /// before calling Refresh() if changes needed to be saved.
        /// </summary>
        private void Refresh()
        {
            DestroyListItems();

            #if PLATFORM_LUMIN
            _nameField.Text = _contact.Name;
            #endif

            _nameField.enabled = IsEditing;

            float yOffset = 0.01f;
            const float xOffset = -0.25f;
            const float yOffsetInc = -0.03f;

            #if PLATFORM_LUMIN
            for (int i = 0; i < _contact.PhoneNumberList.Count; ++i)
            {
                MLContacts.TaggedAttribute phoneNumber = _contact.PhoneNumberList.Items[i];
                GameObject itemGO = Instantiate(_attributeItem.gameObject, transform);
                itemGO.transform.localPosition = new Vector3(xOffset, yOffset, 0);
                yOffset += yOffsetInc;

                ContactAttributeItemVisualizer item = itemGO.GetComponent<ContactAttributeItemVisualizer>();
                item.ContactPageVisualizer = this;
                item.Attribute = phoneNumber;
                item.ListIndex = i;
                item.DeleteCommand = RemovePhoneNumber;

                _phoneNumberGOList.Add(itemGO);
            }
            #endif

            _addPhoneButton.gameObject.SetActive(IsEditing);
            _addPhoneButton.transform.localPosition = new Vector3(xOffset, yOffset, 0);

            if (IsEditing)
            {
                yOffset += 2 * yOffsetInc;
            }
            #if PLATFORM_LUMIN
            else if (_contact.PhoneNumberList.Count > 0)
            {
                yOffset += yOffsetInc;
            }

            for (int i = 0; i < _contact.EmailAddressList.Count; ++i)
            {
                MLContacts.TaggedAttribute emailAddress = _contact.EmailAddressList.Items[i];
                GameObject itemGO = Instantiate(_attributeItem.gameObject, transform);
                itemGO.transform.localPosition = new Vector3(xOffset, yOffset, 0);
                yOffset += yOffsetInc;

                ContactAttributeItemVisualizer item = itemGO.GetComponent<ContactAttributeItemVisualizer>();
                item.ContactPageVisualizer = this;
                item.Attribute = emailAddress;
                item.ListIndex = i;
                item.DeleteCommand = RemoveEmailAddress;

                item.enabled = IsEditing;

                _emailAddressGOList.Add(itemGO);
            }
            #endif

            _addEmailButton.gameObject.SetActive(IsEditing);
            _addEmailButton.transform.localPosition = new Vector3(xOffset, yOffset, 0);
        }

        /// <summary>
        /// Update header buttons based on isEditing.
        /// </summary>
        private void UpdateHeaderButtons()
        {
            _editButton.gameObject.SetActive(!IsEditing);
            _backButton.gameObject.SetActive(!IsEditing);
            _saveButton.gameObject.SetActive(IsEditing);
            _cancelButton.gameObject.SetActive(IsEditing);
        }

        /// <summary>
        /// Update _contact from the UI.
        /// </summary>
        private void UpdateContactFromFields()
        {
            #if PLATFORM_LUMIN
            _contact.Name = _nameField.Text;

            _contact.PhoneNumberList.Items.Clear();
            foreach (GameObject item in _phoneNumberGOList)
            {
                ContactAttributeItemVisualizer attributeItem = item.GetComponent<ContactAttributeItemVisualizer>();
                _contact.PhoneNumberList.Items.Add(attributeItem.Attribute);
            }

            _contact.EmailAddressList.Items.Clear();
            foreach (GameObject item in _emailAddressGOList)
            {
                ContactAttributeItemVisualizer attributeItem = item.GetComponent<ContactAttributeItemVisualizer>();
                _contact.EmailAddressList.Items.Add(attributeItem.Attribute);
            }
            #endif
        }
    }
}

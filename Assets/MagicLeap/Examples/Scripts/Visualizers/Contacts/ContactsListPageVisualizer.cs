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

namespace MagicLeap
{
    /// <summary>
    /// UI controller for the List Page. This lists the contacts provided by
    /// ContactsExample and attaches the relevant event handlers.
    /// </summary>
    public class ContactsListPageVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("Contacts Visualizer component.")]
        private ContactsVisualizer _contactsVisualizer = null;

        [SerializeField, Tooltip("Button to add a new Contact.")]
        private ContactsButtonVisualizer _addContactButton = null;

        [SerializeField, Tooltip("Text field for the search criteria.")]
        private ContactsTextFieldVisualizer _searchTextField = null;

        [SerializeField, Tooltip("Prefab of a Contact List Item.")]
        private ContactItemVisualizer _contactItem = null;

        [SerializeField, Tooltip("Button to delete a non-existing Contact.")]
        private ContactsButtonVisualizer _deleteNonExistentContactButton = null;

        private List<GameObject> _contactGOList = new List<GameObject>();

        /// <summary>
        /// Validates inspector properties, initializes page, and attach event handlers.
        /// </summary>
        void Start()
        {
            if (_contactsVisualizer == null)
            {
                Debug.LogError("Error: ContactsListPageVisualizer._contactsVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_addContactButton == null)
            {
                Debug.LogError("Error: ContactsListPageVisualizer._addContactButton is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_searchTextField == null)
            {
                Debug.LogError("Error: ContactsListPageVisualizer._searchTextField is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_contactItem == null)
            {
                Debug.LogError("Error: ContactsListPageVisualizer._contactItem is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_deleteNonExistentContactButton == null)
            {
                Debug.LogError("Error: ContactsListPageVisualizer._delNonExistentContactButton is not set, disabling script.");
                enabled = false;
                return;
            }

            _addContactButton.OnTap += HandleAddContact;
            _searchTextField.OnTextUpdated += HandleSearchTextUpdated;
            _deleteNonExistentContactButton.OnTap += HandleNonExistingContactDeleteTap;
        }

        /// <summary>
        /// Clean Up.
        /// </summary>
        void OnDestroy()
        {
            _addContactButton.OnTap -= HandleAddContact;
            _searchTextField.OnTextUpdated -= HandleSearchTextUpdated;
            _deleteNonExistentContactButton.OnTap -= HandleNonExistingContactDeleteTap;

            DestroyListItems();
        }

        /// <summary>
        /// Handler when a new search query is entered.
        /// </summary>
        /// <param name="newText">New search query</param>
        private void HandleSearchTextUpdated(string newText)
        {
            _contactsVisualizer.Contacts.LoadContactsFromAPI(newText);
        }

        /// <summary>
        /// Handler when user wants to create a new contact.
        /// </summary>
        private void HandleAddContact()
        {
            _contactsVisualizer.LoadContactPage(string.Empty);
        }

        /// <summary>
        /// Special Handler when the user wants to see what happens when
        /// attempting to delete a non-existent contact.
        /// </summary>
        private void HandleNonExistingContactDeleteTap()
        {
            _contactsVisualizer.Contacts.DeleteContact("InvalidContactId");
        }

        /// <summary>
        /// Clears out list items displayed.
        /// </summary>
        public void DestroyListItems()
        {
            foreach (GameObject item in _contactGOList)
            {
                Destroy(item);
            }

            _contactGOList.Clear();
        }

        /// <summary>
        /// Views the selected contact. Called by ContactItemVisualizer.
        /// </summary>
        /// <param name="id">ID of the Contact</param>
        public void LoadContact(string id)
        {
            _contactsVisualizer.LoadContactPage(id);
        }

        /// <summary>
        /// Deletes the selected contact. Called by ContactItemVisualizer.
        /// </summary>
        /// <param name="id">ID of the Contact</param>
        public void DeleteContact(string id)
        {
            _contactsVisualizer.Contacts.DeleteContact(id);
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Rebuilds the displayed list. Called by ContactsExample.
        /// </summary>
        /// <param name="contacts">List of contacts to display</param>
        public void PopulateList(List<MLContacts.Contact> contacts)
        {
            DestroyListItems();

            for (int i = 0; i < contacts.Count; ++i)
            {
                MLContacts.Contact contact = contacts[i];
                GameObject contactItemGO = Instantiate(_contactItem.gameObject, transform);
                contactItemGO.transform.localPosition = new Vector3(-0.25f, 0.04f - (0.03f * i), 0);

                ContactItemVisualizer contactItem = contactItemGO.GetComponent<ContactItemVisualizer>();
                contactItem.ListPage = this;
                contactItem.ID = contact.ID;
                contactItem.ContactName = contact.Name;

                _contactGOList.Add(contactItemGO);
            }
        }
        #endif
    }
}

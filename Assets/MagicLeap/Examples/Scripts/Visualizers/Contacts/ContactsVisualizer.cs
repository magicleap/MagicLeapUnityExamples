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
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// This class helps visualize the management of contacts.
    /// </summary>
    public class ContactsVisualizer : MonoBehaviour
    {
        public MLContactsBehavior Contacts
        {
            get { return _contacts; }
        }

        [SerializeField, Tooltip("The MLContactsBehavior to use.")]
        private MLContactsBehavior _contacts = null;

        [SerializeField, Tooltip("Root of List Page.")]
        private ContactsListPageVisualizer _listPage = null;

        [SerializeField, Tooltip("Root of Contact Page.")]
        private ContactPageVisualizer _contactPage = null;

        private bool _needToReloadContacts = true;

        /// <summary>
        /// Registers for MLContacts events and tries to load a page.
        /// </summary>
        void Start()
        {
            if (_contacts == null)
            {
                Debug.LogError("Error: ContactsVisualizer._contacts is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_listPage == null)
            {
                Debug.LogError("Error: ContactsVisualizer._listPage is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_contactPage == null)
            {
                Debug.LogError("Error: ContactsVisualizer._contactPage is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            _contacts.OnRefreshPageList += HandleRefreshListPage;
            _contacts.OnStartupComplete += HandleStartupComplete;
            MLContacts.OnContactAdded += HandleOnContactAdded;
            MLContacts.OnContactUpdated += HandleOnContactUpdated;
            MLContacts.OnContactDeleted += HandleOnContactDeleted;
            #endif
        }

        /// <summary>
        /// Unregisters from MLContacts events.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            _contacts.OnRefreshPageList -= HandleRefreshListPage;
            _contacts.OnStartupComplete -= HandleStartupComplete;
            MLContacts.OnContactAdded -= HandleOnContactAdded;
            MLContacts.OnContactUpdated -= HandleOnContactUpdated;
            MLContacts.OnContactDeleted -= HandleOnContactDeleted;
            #endif
        }

        /// <summary>
        /// Switch to list page. May reload contacts if needed.
        /// </summary>
        public void LoadListPage()
        {
            _listPage.gameObject.SetActive(true);
            _contactPage.gameObject.SetActive(false);

            if (_needToReloadContacts)
            {
                _contacts.LoadContactsFromAPI();
            }
        }

        /// <summary>
        /// Loads an existing contact and switches to contact page.
        /// </summary>
        /// <param name="id">Id of the contact.</param>
        public void LoadContactPage(string id)
        {
            _listPage.gameObject.SetActive(false);
            _contactPage.gameObject.SetActive(true);

            #if PLATFORM_LUMIN
            if (!string.IsNullOrEmpty(id) && _contacts.loadedContacts.ContainsKey(id))
            {
                _contactPage.Populate(new MLContacts.Contact(_contacts.loadedContacts[id]));
            }
            else
            {
                _contactPage.Populate(new MLContacts.Contact());
            }
            #endif
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Handler when Contacts API successfully removes a contact.
        /// This is called from the list contacts page.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnContactDeleted(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            _contacts.LoadContactsFromAPI();
        }

        /// <summary>
        /// Handler when attempt to request privileges and start API have completed.
        /// </summary>
        /// <param name="ready">Boolean to indicate if Example is ready to start.</param>
        private void HandleStartupComplete(bool ready)
        {
            if (ready)
            {
                LoadListPage();
            }
            else
            {
                Debug.LogErrorFormat("Error: ContactsVisualizer failed to load list page because MLContacts API failed to start, disabling script.");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Handler when Contacts API successfully updates an existing contact.
        /// This is called from the add/edit contact page.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnContactUpdated(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            _contactPage.Populate(operationResult.Contact);
            _needToReloadContacts = true;
        }

        /// <summary>
        /// Handler when Contacts API successfully adds a new contact.
        /// This is called from the add/edit contact page.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnContactAdded(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            _contactPage.Populate(operationResult.Contact);
            _needToReloadContacts = true;
        }
        #endif

        /// <summary>
        /// Handler when page is retrieved successfully. Store the list of contacts in memory and
        /// fetch the next page. If this is the last page, display them.
        /// </summary>
        /// <param name="page">Page with list of contacts.</param>
        private void HandleRefreshListPage(MLContacts.ListPage page)
        {
            _needToReloadContacts = false;

            #if PLATFORM_LUMIN
            if (page.Status == MLContacts.ListPage.PageStatus.LastPage)
            {
                _listPage.PopulateList(new List<MLContacts.Contact>(_contacts.loadedContacts.Values));
            }
            #endif
        }
    }
}


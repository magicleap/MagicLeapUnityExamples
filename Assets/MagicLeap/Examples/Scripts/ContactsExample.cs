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
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This is the main controller for the Contacts Example.
    /// This directly interfaces with the Contacts API and propagates
    /// the state changes to the relevant UI elements.
    /// </summary>
    public class ContactsExample : MonoBehaviour
    {
        [SerializeField, Tooltip("Example Contacts Canvas.")]
        private GameObject _contactsVisualizer = null;

        [SerializeField, Tooltip("Status text for logs.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("Status text for logs on the contacts canvas.")]
        private Text _contactsVisualizerStatusText = null;

        [SerializeField, Tooltip("Controller input.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("MLA controller input.")]
        private MLControllerConnectionHandlerBehavior _mobileControllerConnectionHandler = null;

        private float _canvasFwdDistance = 1f;

        private bool _internetConnected = false;

        private string _lastLogMessage = "";

        /// <summary>
        /// Validates inspector properties, initializes scene, and registers event handlers.
        /// </summary>
        void Start()
        {
            if (_contactsVisualizer == null)
            {
                Debug.LogError("Error: ContactsExample._contactsVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: ContactsExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_contactsVisualizerStatusText == null)
            {
                Debug.LogError("Error: ContactsExample._contactsVisualizerStatusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: ContactsExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_mobileControllerConnectionHandler == null)
            {
                Debug.LogError("Error: ContactsExample._mobileControllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            MLContacts.OnContactAdded += HandleOnContactAdded;
            MLContacts.OnContactUpdated += HandleOnContactUpdated;
            MLContacts.OnContactDeleted += HandleOnContactDeleted;
            MLContacts.OnOperationFailed += HandleOnOperationFailed;
            #endif

            StartCoroutine("PlaceContactsVisualizer");
        }

        /// <summary>
        /// Unregisters from MLInput and MLContacts events.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            MLContacts.OnContactAdded -= HandleOnContactAdded;
            MLContacts.OnContactUpdated -= HandleOnContactUpdated;
            MLContacts.OnContactDeleted -= HandleOnContactDeleted;
            MLContacts.OnOperationFailed -= HandleOnOperationFailed;
            #endif
        }

        /// <summary>
        /// Sets the status text of the UI.
        /// </summary>
        void Update()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n\n",
                _controllerConnectionHandler.IsControllerValid() ? "Controller Connected" : "Disconnected" );

            _statusText.text += string.Format("<color=#dbfb76><b>MLAControllerData</b></color>\nStatus: {0}\n\n",
                ControllerStatus.Text);

            #if PLATFORM_LUMIN
            MLNetworking.IsInternetConnected(ref _internetConnected);
            #endif

            _statusText.text += string.Format("<color=#dbfb76><b>Internet Data</b></color>\nStatus: {0}\n\n",
                _internetConnected ? "Connected" : "Disconnected");


            if (_lastLogMessage != "")
            {
                _statusText.text += string.Format("<color=#dbfb76><b>Contacts Data</b></color>\nLastAction: {0}\n\n",
                    _lastLogMessage);
            }
        }

        /// <summary>
        /// Updates on-screen log.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        public void Log(string msg)
        {
            _lastLogMessage = msg;
            _contactsVisualizerStatusText.text = msg;
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Handler when Contacts API fails any operation.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnOperationFailed(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            string reason = "Unspecified failure";
            switch (operationResult.Status)
            {
                case MLContacts.OperationStatus.Duplicate:
                    reason = "A contact, with the same details, already exist.";
                    break;
                case MLContacts.OperationStatus.Fail:
                    reason = "An internal error has occurred.";
                    break;
                case MLContacts.OperationStatus.NotFound:
                    reason = "The contact does not exist.";
                    break;
                case MLContacts.OperationStatus.Success:
                    // this case should never be reached in this callback.
                    break;
            }

            Debug.LogErrorFormat("Error: ContactsExample encountered an error from an operation. {0}", reason);
            Log(string.Format("Operation Failed. {0}", reason));
        }

        /// <summary>
        /// Handler when Contacts API successfully removes a contact.
        /// This is called from the list contacts page.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnContactDeleted(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            Log("Contact deleted");
        }

        /// <summary>
        /// Handler when Contacts API successfully updates an existing contact.
        /// This is called from the add/edit contact page.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnContactUpdated(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            Log("Contact updated");
        }

        /// <summary>
        /// Handler when Contacts API successfully adds a new contact.
        /// This is called from the add/edit contact page.
        /// </summary>
        /// <param name="operationResult">Result of the operation.</param>
        /// <param name="requestHandle">Handle of the request.</param>
        private void HandleOnContactAdded(MLContacts.OperationResult operationResult, ulong requestHandle)
        {
            Log("Contact added");
        }
        #endif


        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void HandleOnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if ((_mobileControllerConnectionHandler.IsControllerValid(controllerId) || _controllerConnectionHandler.IsControllerValid(controllerId)) && button == MLInput.Controller.Button.Bumper)
            {
                PlaceContactsVisualizerFromCamera();
            }
        }

        /// <summary>
        /// Give time for the Camera to start and setup the visualizer in front of the camera.
        /// </summary>
        private IEnumerator PlaceContactsVisualizer()
        {
            yield return new WaitForSeconds(1);
            PlaceContactsVisualizerFromCamera();
        }

        /// <summary>
        /// Sets the contacts canvas to the correct position and orientation.
        /// </summary>
        private void PlaceContactsVisualizerFromCamera()
        {
            Camera mainCamera = Camera.main;
            _contactsVisualizer.transform.position = mainCamera.transform.position + mainCamera.transform.forward * _canvasFwdDistance;
            _contactsVisualizer.transform.rotation = Quaternion.LookRotation(_contactsVisualizer.transform.position - mainCamera.transform.position);
        }
    }
}

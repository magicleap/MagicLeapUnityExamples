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
using UnityEngine.XR.MagicLeap;
using System;
using System.Collections;
using MagicLeap.Core.StarterKit;

namespace MagicLeap
{
    /// <summary>
    /// This example uses the Location API obtain a latitude and longitude, based on the zip code.
    /// The globe is rotated and a pin placed at the geographic location.
    /// </summary>
    public class LocationExample : MonoBehaviour
    {
        private MLPrivilegeRequesterBehavior _privilegeRequester;

        [SerializeField, Tooltip("The text used to display status messages.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("The text used to display the coarse location information.")]
        private Text _coarseLocationText = null;

        [SerializeField, Tooltip("The text used to display the fine location information.")]
        private Text _fineLocationText = null;

        [SerializeField, Tooltip("The pin to place at the geographic location.")]
        private Transform _pin = null;

        [SerializeField, Tooltip("The GameObject for the globe.")]
        private GameObject _globe = null;

        [SerializeField, Tooltip("Smoothing time for the rotation of the globe.")]
        private float _rotationSmoothTime = 2.0f;

        [SerializeField, Tooltip("The distance from the camera through its forward vector.")]
        private float _distance = 1.0f;

        private Transform _mainCameraTransform;

        private Quaternion _rotationGlobeToLookCamera;
        private Quaternion _rotationGlobeToLookPin;
        private Quaternion _rotationPinToLookCamera;

        private bool _placedPin = false;
        private bool _placedGlobe = false;

        private string locationErrorText = "";

        private const float LOCATION_SYNC_RATE = 20f;
        private void Awake()
        {
            if (_statusText == null)
            {
                Debug.LogError("Error: LocationExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_coarseLocationText == null)
            {
                Debug.LogError("Error: LocationExample._coarseLocationText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_fineLocationText == null)
            {
                Debug.LogError("Error: LocationExample._fineLocationText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_pin == null)
            {
                Debug.LogError("Error: LocationExample._pin is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_globe == null)
            {
                Debug.LogError("Error: LocationExample._globe is not set, disabling script.");
                enabled = false;
                return;
            }

            _mainCameraTransform = Camera.main.transform;

            _globe.SetActive(false);
            _pin.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            MLLocationStarterKit.Stop();
        }

        void Start()
        {
            StartupAPI();
        }

        void Update()
        {
            if (_placedGlobe && _placedPin)
            {
                RotateGlobe(Time.deltaTime / _rotationSmoothTime);
            }

            UpdateStatus();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                MLLocationStarterKit.Stop();
            }
            else
            {
                #if PLATFORM_LUMIN
                MLLocationStarterKit.Start();
                #endif
            }
        }

        /// <summary>
        /// Starts the MLLocation API and polls data if needed.
        /// </summary>
        private void StartupAPI()
        {
            #if PLATFORM_LUMIN
            MLResult result = MLLocationStarterKit.Start();
            if (!result.IsOk)
            {
                Debug.LogError("Error: LocationExample failed to start MLLocation, disabling script.");
                enabled = false;
                return;
            }
            #endif

            GetLocation();
            StartCoroutine(GetFineLocationLoop());
        }

        /// <summary>
        /// Coroutine for continous polling for fine location.
        /// </summary>
        private IEnumerator GetFineLocationLoop()
        {
            while (true)
            {
                GetLocation(true);
                yield return new WaitForSeconds(20f);
            }
        }

        /// <summary>
        /// Polls current location data.
        /// </summary>
        private void GetLocation(bool useFineLocation = false)
        {
            #if PLATFORM_LUMIN
            MLResult result = MLLocationStarterKit.GetLocation(out MLLocation.Location newLocation, useFineLocation);

            if (result.IsOk)
            {
                string formattedString =
                    "{0}:\t<i>{1}</i>\n" +
                    "{2}:\t<i>{3}</i>\n" +
                    "{4}:\t<i>{5}</i>\n" +
                    "{6}:\t<i>{7}</i>\n" +
                    (useFineLocation ? "{8}:\t<i>{9}</i>" : "");

                Text locationText = useFineLocation ? _fineLocationText : _coarseLocationText;

                locationText.text = String.Format(formattedString,
                    "Latitude", newLocation.Latitude,
                    "Longitude",  newLocation.Longitude,
                    "Postal Code",  newLocation.HasPostalCode ? newLocation.PostalCode : "(unknown)",
                    "Time stamp", newLocation.Timestamp,
                    "Accuracy", newLocation.Accuracy
                );

                if (!_placedGlobe && !_placedPin)
                {
                    StartCoroutine(PlaceGlobe());
                    PlacePin(MLLocationStarterKit.GetWorldCartesianCoords(newLocation.Latitude, newLocation.Longitude, _globe.GetComponent<SphereCollider>().radius));
                }
            }
            else
            {
                if (result.Result == MLResult.Code.LocationNetworkConnection)
                {
                    locationErrorText = "<color=red>Received network error, please check the network connection and relaunch the application.</color>";
                }
                else
                {
                    locationErrorText = "<color=red>Failed to retrieve location with result: " + result.Result + "</color>";
                }

                MLLocationStarterKit.Stop();
                enabled = false;
                return;
            }
            #endif

        }

       private void UpdateStatus()
       {
           _statusText.text = string.Format("<color=#dbfb76><b>ControllerData </b></color>\nStatus: {0}\n",
                ControllerStatus.Text) + locationErrorText;

            _statusText.text += string.Format("\n<color=#dbfb76><b>{0} </b></color>:\n",
                "Location Data") +  _fineLocationText.text;
       }

        /// <summary>
        /// Create our pin and place it at the proper location.
        /// </summary>
        /// <param name="position">Vector3 of cartesian coordinates</param>
        /// <returns>The created pin's GameObject</returns>
        public void PlacePin(Vector3 position)
        {
            _placedPin = true;

            _pin.transform.parent = _globe.transform;
            _pin.transform.up = position.normalized;
            _pin.transform.position = _globe.transform.position + (position * _globe.transform.localScale.x);
            _pin.gameObject.SetActive(true);
        }

        /// <summary>
        /// Coroutine to trigger placing the globe after headpose stabliizes.
        /// </summary>
        public IEnumerator PlaceGlobe()
        {
            yield return new WaitForSeconds(1);
            _placedGlobe = true;

            PlaceGlobeFromCamera();
        }

        /// <summary>
        /// Place the globe in front of the user and activate the GameObject.
        /// </summary>
        private void PlaceGlobeFromCamera()
        {
            Vector3 targetPosition = _mainCameraTransform.position + (_mainCameraTransform.forward * _distance);

            _globe.transform.position = targetPosition;
            _globe.SetActive(true);

            RotateGlobe(1);
        }

        /// <summary>
        /// Rotate the globe so that the user can see their location pin.
        /// </summary>
        public void RotateGlobe(float rate)
        {
            _rotationGlobeToLookCamera = Quaternion.LookRotation(_mainCameraTransform.position - _globe.transform.position);
            _rotationGlobeToLookPin = Quaternion.LookRotation(_pin.localPosition);

            _rotationPinToLookCamera = _rotationGlobeToLookCamera * Quaternion.Inverse(_rotationGlobeToLookPin);

            _globe.transform.rotation = Quaternion.Slerp(_globe.transform.rotation, _rotationPinToLookCamera, rate);
        }
    }
}

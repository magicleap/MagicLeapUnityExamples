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
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// This class handles the functionality of updating the bounding box
    /// for the planes query params through input. This class also updates
    /// the UI text containing the latest useful info on the planes queries.
    /// </summary>
    public class PlanesExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The MLPlanesBehavior to subscribe to.")]
        private MLPlanesBehavior _planes = null;

        [SerializeField, Tooltip("The PlanesVisualizer to cycle visuals with.")]
        private PlanesVisualizer _planesVisualizer = null;

        [SerializeField, Tooltip("Flag specifying if plane extents are bounded.")]
        private bool _bounded = false;

        [SerializeField, Space, Tooltip("Wireframe cube to represent bounds.")]
        private GameObject _boundsWireframeCube = null;

        [SerializeField, Space, Tooltip("Text to display content status.")]
        private Text _statusText = null;

        [Space, SerializeField, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        private static readonly Vector3 _boundedExtentsSize = new Vector3(5.0f, 5.0f, 5.0f);
        // Distance close to sensor's maximum recognition distance.
        private static readonly Vector3 _boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

        private Camera _camera;

        private string _renderModeTextString = string.Empty;
        private string _boundsExtentsTextString = string.Empty;
        private string _numBoundariesTextString = string.Empty;
        private string _numPlanesTextString = string.Empty;

        /// <summary>
        /// Check editor set variables for null references.
        /// </summary>
        void Awake()
        {
            if (_planes == null)
            {
                Debug.LogError("Error: PlanesExample._planes is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_planesVisualizer == null)
            {
                Debug.LogError("Error: PlanesExample._planesVisualizer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: PlanesExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_boundsWireframeCube == null)
            {
                Debug.LogError("Error: PlanesExample._boundsWireframeCube is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: PlanesExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown += OnButtonDown;
            #endif

            _camera = Camera.main;

            #if PLATFORM_LUMIN
            _planes.OnQueryPlanesResult += OnQueriedPlanes;
            #endif
        }

        /// <summary>
        /// Start bounds based on _bounded state.
        /// </summary>
        void Start()
        {
            #if PLATFORM_LUMIN

            MLResult result = MLHeadTracking.Start();
            if (result.IsOk)
            {
                MLHeadTracking.RegisterOnHeadTrackingMapEvent(OnHeadTrackingMapEvent);
            }
            else
            {
                Debug.LogError("PlanesExample could not register to head tracking events because MLHeadTracking could not be started.");
            }
            #endif
            UpdateBounds();
        }

        /// <summary>
        /// Update position of the planes component to camera position.
        /// Planes query center is based on this position.
        /// </summary>
        void Update()
        {
            _planes.gameObject.transform.position = _camera.transform.position;
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLHeadTracking.UnregisterOnHeadTrackingMapEvent(OnHeadTrackingMapEvent);
            MLHeadTracking.Stop();
            MLInput.OnControllerButtonDown -= OnButtonDown;
            #endif
        }

        /// <summary>
        /// Update plane query bounds extents based on if the current _bounded status is true(bounded)
        /// or false(boundless).
        /// </summary>
        private void UpdateBounds()
        {
            _planes.transform.localScale = _bounded ? _boundedExtentsSize : _boundlessExtentsSize;
            _boundsWireframeCube.SetActive(_bounded);

            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n\n",
                ControllerStatus.Text);

            _renderModeTextString = string.Format("<color=#dbfb76><b>Render Mode</b></color>\n{0}\n\n",
                _planesVisualizer.RenderMode.ToString());

            _boundsExtentsTextString = string.Format("<color=#dbfb76><b>Bounds Extents</b></color>\n ({0},{1},{2})\n\n",
                _planes.transform.localScale.x,
                _planes.transform.localScale.y,
                _planes.transform.localScale.z);

            _statusText.text += _renderModeTextString + _boundsExtentsTextString + _numPlanesTextString + _numBoundariesTextString;
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Callback handler, changes text when new planes are received.
        /// </summary>
        /// <param name="planes"> Array of new planes. </param>
        /// <param name="boundaries"> Array of new boundaries. </param>
        private void OnQueriedPlanes(MLPlanes.Plane[] planes, MLPlanes.Boundaries[] boundaries)
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n\n",
                ControllerStatus.Text);

            _renderModeTextString = string.Format("<color=#dbfb76><b>Render Mode</b></color>\n{0}\n\n",
                _planesVisualizer.RenderMode.ToString());

            _boundsExtentsTextString = string.Format("<color=#dbfb76><b>Bounds Extents</b></color>\n ({0},{1},{2})\n\n",
                _planes.transform.localScale.x,
                _planes.transform.localScale.y,
                _planes.transform.localScale.z);

            _numPlanesTextString = string.Format("<color=#dbfb76><b>Planes</b></color>\n {0} / {1}\n\n", planes.Length, _planes.MaxPlaneCount);
            _numBoundariesTextString = string.Format("<color=#dbfb76><b>Boundaries</b></color>\n {0} / {1}\n\n", boundaries.Length, _planes.MaxPlaneCount);

            _statusText.text += _renderModeTextString + _boundsExtentsTextString + _numPlanesTextString + _numBoundariesTextString;
        }
        #endif

        /// <summary>
        /// Handles the event for button down. Changes from bounded to boundless and viceversa
        /// when pressing home button
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void OnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId))
            {
                switch (button)
                {
                    case MLInput.Controller.Button.HomeTap:
                        _bounded = !_bounded;
                        UpdateBounds();
                        break;

                    case MLInput.Controller.Button.Bumper:
                        _planesVisualizer.CycleMode();
                        break;
                }
            }
        }

        /// <summary>
        /// Handle if a new session occurs
        /// </summary>
        /// <param name="mapEvents"> Map Events that happened. </param>
        private void OnHeadTrackingMapEvent(MLHeadTracking.MapEvents mapEvents)
        {
            #if PLATFORM_LUMIN
            if (mapEvents.IsNewSession())
            {
                _statusText.text = "New map session";
            }
            #endif
        }
    }
}

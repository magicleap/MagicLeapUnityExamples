// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

#if UNITY_EDITOR || UNITY_MAGICLEAP || UNITY_ANDROID

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.InteractionSubsystems;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This represents all the runtime control over meshing component in order to best visualize the
    /// affect changing parameters has over the meshing API.
    /// </summary>
    public class MeshingExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The spatial mapper from which to update mesh params.")]
        private MeshingSubsystemComponent _meshingSubsystemComponent = null;

        [SerializeField, Tooltip("Visualizer for the meshing results.")]
        private MeshingVisualizer _meshingVisualizer = null;

        [SerializeField, Space, Tooltip("A visual representation of the meshing bounds.")]
        private GameObject _visualBounds = null;

        [SerializeField, Space, Tooltip("Flag specifying if mesh extents are bounded.")]
        private bool _bounded = false;

        [SerializeField, Space, Tooltip("The text to place mesh data on.")]
        private Text _statusLabel = null;

        [SerializeField, Space, Tooltip("Prefab to shoot into the scene.")]
        private GameObject _shootingPrefab = null;

        [SerializeField, Space, Tooltip("Render mode to render mesh data with.")]
        private MeshingVisualizer.RenderMode _renderMode = MeshingVisualizer.RenderMode.Wireframe;
        private int _renderModeCount;

        [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is enabled.")]
        private Vector3 _boundedExtentsSize = new Vector3(2.0f, 2.0f, 2.0f);

        [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is disabled.")]
        private Vector3 _boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

        private const float SHOOTING_FORCE = 300.0f;
        private const float MIN_BALL_SIZE = 0.2f;
        private const float MAX_BALL_SIZE = 0.5f;
        private const int BALL_LIFE_TIME = 10;

        private Camera _camera = null;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;
        private XRRayInteractor xRRayInteractor;
        private XRInputSubsystem inputSubsystem;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        /// <summary>
        /// Initializes component data and starts MLInput.
        /// </summary>
        void Awake()
        {
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            if (_meshingSubsystemComponent == null)
            {
                Debug.LogError("MeshingExample._meshingSubsystemComponent is not set. Disabling script.");
                enabled = false;
                return;
            }
            else
            {
                // disable _meshingSubsystemComponent until we have successfully requested permissions
                _meshingSubsystemComponent.enabled = false;
            }
            if (_meshingVisualizer == null)
            {
                Debug.LogError("MeshingExample._meshingVisualizer is not set. Disabling script.");
                enabled = false;
                return;
            }
            if (_visualBounds == null)
            {
                Debug.LogError("MeshingExample._visualBounds is not set. Disabling script.");
                enabled = false;
                return;
            }
            if (_statusLabel == null)
            {
                Debug.LogError("MeshingExample._statusLabel is not set. Disabling script.");
                enabled = false;
                return;
            }
            if (_shootingPrefab == null)
            {
                Debug.LogError("MeshingExample._shootingPrefab is not set. Disabling script.");
                enabled = false;
                return;
            }

#if UNITY_MAGICLEAP || UNITY_ANDROID
            MLDevice.RegisterGestureSubsystem();
            if (MLDevice.GestureSubsystemComponent == null)
            {
                Debug.LogError("MLDevice.GestureSubsystemComponent is not set. Disabling script.");
                enabled = false;
                return;
            }
#endif
            xRRayInteractor = FindObjectOfType<XRRayInteractor>();

            _renderModeCount = System.Enum.GetNames(typeof(MeshingVisualizer.RenderMode)).Length;

            _camera = Camera.main;

#if UNITY_MAGICLEAP || UNITY_ANDROID
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Trigger.performed += OnTriggerDown;
            controllerActions.Bumper.performed += OnBumperDown;
            controllerActions.Menu.performed += OnMenuDown;

            MLDevice.GestureSubsystemComponent.onTouchpadGestureChanged += OnTouchpadGestureStart;
#endif
        }

        /// <summary>
        /// Set correct render mode for meshing and update meshing settings.
        /// </summary>
        private void Start()
        {
            MLPermissions.RequestPermission(MLPermission.SpatialMapping, permissionCallbacks);

            inputSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();
            inputSubsystem.trackingOriginUpdated += OnTrackingOriginChanged;

            _meshingVisualizer.SetRenderers(_renderMode);

            _meshingSubsystemComponent.gameObject.transform.position = _camera.gameObject.transform.position;
            UpdateBounds();
        }



        /// <summary>
        /// Update mesh polling center position to camera.
        /// </summary>
        void Update()
        {
            if (_meshingVisualizer.renderMode != _renderMode)
            {
                _meshingVisualizer.SetRenderers(_renderMode);
            }

            _meshingSubsystemComponent.gameObject.transform.position = _camera.gameObject.transform.position;
            if ((_bounded && _meshingSubsystemComponent.gameObject.transform.localScale != _boundedExtentsSize) ||
                (!_bounded && _meshingSubsystemComponent.gameObject.transform.localScale != _boundlessExtentsSize))
            {
                UpdateBounds();
            }

            UpdateStatusText();
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

#if UNITY_MAGICLEAP || UNITY_ANDROID
            controllerActions.Trigger.performed -= OnTriggerDown;
            controllerActions.Bumper.performed -= OnBumperDown;
            controllerActions.Menu.performed -= OnMenuDown;
            inputSubsystem.trackingOriginUpdated -= OnTrackingOriginChanged;

            if (MLDevice.GestureSubsystemComponent != null)
                MLDevice.GestureSubsystemComponent.onTouchpadGestureChanged -= OnTouchpadGestureStart;

            mlInputs.Dispose();
#endif
        }

        private void OnPermissionGranted(string permission)
        {
            _meshingSubsystemComponent.enabled = true;
        }

        private void OnPermissionDenied(string permission)
        {
            Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
            enabled = false;
            _meshingSubsystemComponent.enabled = false;
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusLabel.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);

            _statusLabel.text += string.Format(
                "\n<color=#dbfb76><b>{0} {1}</b></color>\n{2} {3}: {4}\n{5} {6}: {7}\n{8}: {9}",
                "Meshing",
                "Data",
                "Render",
                "Mode",
                _renderMode.ToString(),
                "Bounded",
                "Extents",
                _bounded.ToString(),
                "LOD",
#if UNITY_2019_3_OR_NEWER
                MeshingSubsystemComponent.DensityToLevelOfDetail(_meshingSubsystemComponent.density).ToString()
#else
                _meshingSubsystemComponent.levelOfDetail.ToString()
#endif
                );
        }

        /// <summary>
        /// Handles the event for bumper down. Changes render mode.
        /// </summary>
        /// <param name="callbackContext"></param>
        private void OnBumperDown(InputAction.CallbackContext callbackContext)
        {
            _renderMode = (MeshingVisualizer.RenderMode)((int)(_renderMode + 1) % _renderModeCount);
            _meshingVisualizer.SetRenderers(_renderMode);
        }

        /// <summary>
        ///  Handles the event for Home down. 
        /// changes from bounded to boundless and viceversa.
        /// </summary>
        /// <param name="callbackContext"></param>
        private void OnMenuDown(InputAction.CallbackContext callbackContext)
        {
            _bounded = !_bounded;
            UpdateBounds();
        }

        /// <summary>
        /// Handles the event for trigger down. Throws a ball in the direction of
        /// the camera's forward vector.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being released.</param>
        private void OnTriggerDown(InputAction.CallbackContext callbackContext)
        {
            if (xRRayInteractor.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult result))
            {
                return;
            }

            // TODO: Use pool object instead of instantiating new object on each trigger down.
            // Create the ball and necessary components and shoot it along raycast.
            GameObject ball = Instantiate(_shootingPrefab);

            ball.SetActive(true);
            float ballsize = Random.Range(MIN_BALL_SIZE, MAX_BALL_SIZE);
            ball.transform.localScale = new Vector3(ballsize, ballsize, ballsize);
            ball.transform.position = _camera.gameObject.transform.position;

            Rigidbody rigidBody = ball.GetComponent<Rigidbody>();
            if (rigidBody == null)
            {
                rigidBody = ball.AddComponent<Rigidbody>();
            }
            rigidBody.AddForce(_camera.gameObject.transform.forward * SHOOTING_FORCE);

            Destroy(ball, BALL_LIFE_TIME);
        }

        /// <summary>
        /// Handles the event for touchpad gesture start. Changes level of detail
        /// if gesture is swipe up.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="gesture">The gesture getting started.</param>
        private void OnTouchpadGestureStart(GestureSubsystem.Extensions.TouchpadGestureEvent touchpadGestureEvent)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            if (touchpadGestureEvent.state == GestureState.Started &&
                touchpadGestureEvent.type == InputSubsystem.Extensions.TouchpadGesture.Type.Swipe &&
                touchpadGestureEvent.direction == InputSubsystem.Extensions.TouchpadGesture.Direction.Up)
            {
#if UNITY_2019_3_OR_NEWER
                _meshingSubsystemComponent.density = MLSpatialMapper.LevelOfDetailToDensity((MLSpatialMapper.DensityToLevelOfDetail(_meshingSubsystemComponent.density) == MLSpatialMapper.LevelOfDetail.Maximum) ? MLSpatialMapper.LevelOfDetail.Minimum : (MLSpatialMapper.DensityToLevelOfDetail(_meshingSubsystemComponent.density) + 1));
#else
                _mlSpatialMapper.levelOfDetail = ((_mlSpatialMapper.levelOfDetail == MLSpatialMapper.LevelOfDetail.Maximum) ? MLSpatialMapper.LevelOfDetail.Minimum : (_mlSpatialMapper.levelOfDetail + 1));
#endif
            }
#endif
        }

        /// <summary>
        /// Handle in charge of refreshing all meshes if a new session occurs
        /// </summary>
        /// <param name="inputSubsystem"> The inputSubsystem that invoked this event. </param>
        private void OnTrackingOriginChanged(XRInputSubsystem inputSubsystem)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            _meshingSubsystemComponent.DestroyAllMeshes();
            _meshingSubsystemComponent.RefreshAllMeshes();
#endif
        }

        private void UpdateBounds()
        {
            _visualBounds.SetActive(_bounded);
            _meshingSubsystemComponent.gameObject.transform.localScale = _bounded ? _boundedExtentsSize : _boundlessExtentsSize;
        }
    }
}

#endif

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
using UnityEngine.SceneManagement;

namespace MagicLeap
{
    /// <summary>
    /// This class handles setting the position and rotation of the
    /// transform to match the camera's based on input distance and height
    /// </summary>
    public class PlaceFromCamera : MonoBehaviour
    {
        public enum LookDirection
        {
            None = 0,
            LookAtCamera = 1,
            LookAwayFromCamera = 2
        }

        [SerializeField, Tooltip("The distance from the camera through its forward vector.")]
        private float _distance = 0.0f;

        [SerializeField, Tooltip("The distance on the local Y axis from the camera's forward ray.")]
        private float _heightOffset = 0.0f;

        [SerializeField, Tooltip("The distance on the local X axis from the camera's forward ray.")]
        private float _lateralOffset = 0.0f;

        [SerializeField, Tooltip("The approximate time it will take to reach the current position.")]
        private float _positionSmoothTime = 5f;
        private Vector3 _positionVelocity = Vector3.zero;

        [SerializeField, Tooltip("The approximate time it will take to reach the current rotation.")]
        private float _rotationSmoothTime = 5f;

        [SerializeField, Tooltip("The direction the transform should face.")]
        private LookDirection _lookDirection = LookDirection.LookAwayFromCamera;

        [SerializeField, Tooltip("Toggle to set position on awake.")]
        private bool _placeOnAwake = false;

        [SerializeField, Tooltip("Toggle to set position on update.")]
        private bool _placeOnUpdate = false;

        [SerializeField, Tooltip("Prevents the object from rotating around the Z axis.")]
        private bool _lockZRotation = false;

        [SerializeField, Tooltip("Prevents the object from rotating around the X axis.")]
        private bool _lockXRotation = false;

        [SerializeField, Tooltip("Places the object in front of and at the same Y position as the camera.")]
        private bool _lockToXZPlane = false;

        [SerializeField, Tooltip("When this is enabled, movement is restricted by threshold conditions.")]
        private bool _useThreshold = false;

        [SerializeField, Tooltip("When [UseThreshold] is enabled the (x, y) euler limits will force an update.")]
        private Vector2 _movementThreshold = new Vector2(10f, 5f);

        [SerializeField, Tooltip("When [UseThreshold] is enabled distance from the camera will force an update.")]
        private float _distanceThreshold = 0.15f;

        [SerializeField, Tooltip("When enabled, used local space instead of world space.")]
        private bool _useLocalSpace = true;

        private Vector3 _movePosition = Vector3.zero;
        private Vector3 _lastCameraEuler = Vector3.zero;
        private Vector3 _lastCameraPosition = Vector3.zero;

        private bool _forceUpdate = false;
        private Camera _mainCamera = null;

        private bool _shouldTryPlacementOnEnable = false;
        private bool _didPlaceOnAwake = false;

        /// <summary>
        /// When enabled automatic placement will occur on each Update cycle.
        /// </summary>
        public bool PlaceOnUpdate
        {
            get { return _placeOnUpdate; }
            set { _placeOnUpdate = value; }
        }

        public float Distance
        {
            get { return _distance; }
            set { _distance = value; }
        }

        /// <summary>
        /// Set the transform from latest position if flag is checked.
        /// </summary>
        void Awake()
        {
            _mainCamera = Camera.main;
            SceneManager.activeSceneChanged += ChangedActiveScene;

            if (_placeOnAwake)
            {
                StartCoroutine(UpdateTransformEndOfFrame());
            }
        }

        private void OnEnable()
        {
            if (_shouldTryPlacementOnEnable && _placeOnAwake)
            {
                _shouldTryPlacementOnEnable = false;
                StartCoroutine(UpdateTransformEndOfFrame());
            }
        }

        void Update()
        {
            if (!_placeOnAwake && _placeOnUpdate)
            {
                UpdateTransform(_mainCamera);
            }
        }

        void OnValidate()
        {
            _positionSmoothTime = Mathf.Max(0.01f, _positionSmoothTime);
            _rotationSmoothTime = Mathf.Max(0.01f, _rotationSmoothTime);
        }

        private void OnDisable()
        {
            // If the game object was initially awake, causing the UpdateTransformEndOfFrame()
            // coroutine to be queued, but before it could be called, the game object was disabled.
            // In that case, retry placement when the game object is enabled again.
            _shouldTryPlacementOnEnable = _placeOnAwake && !_didPlaceOnAwake;
        }

        void OnDestroy()
        {
            SceneManager.activeSceneChanged -= ChangedActiveScene;
        }

        public void ForceUpdate()
        {
            _forceUpdate = true;
        }

        public void ToggleThreshold()
        {
            _useThreshold = !_useThreshold;
        }

        private void ChangedActiveScene(Scene current, Scene next)
        {
            _mainCamera = Camera.main;
        }

        /// <summary>
        /// Reset position and rotation to match current camera values, after the end of frame.
        /// </summary>
        private IEnumerator UpdateTransformEndOfFrame()
        {
            // Wait until the camera has finished the current frame.
            yield return new WaitForEndOfFrame();
            _didPlaceOnAwake = true;
            UpdateTransform(_mainCamera);
        }

        /// <summary>
        /// Reset position and rotation to match current camera values.
        /// </summary>
        private void UpdateTransform(Camera camera)
        {
            // Move the object in front of the camera with specified offsets.
            Vector3 offsetVector = (camera.transform.up * _heightOffset) + (camera.transform.right * _lateralOffset);
            Vector3 forwardVec = (_lockToXZPlane) ? new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z).normalized : camera.transform.forward;
            Vector3 targetPosition = (_useLocalSpace ? camera.transform.localPosition : camera.transform.position) + offsetVector + (forwardVec * _distance);

            if (_forceUpdate || !_useThreshold || Vector3.Distance((_useLocalSpace ? transform.localPosition : transform.position), _movePosition) > 0.01f ||
                Quaternion.Angle(Quaternion.Euler((_useLocalSpace ? camera.transform.localEulerAngles.x : camera.transform.eulerAngles.x), 0, 0), Quaternion.Euler(_lastCameraEuler.x, 0, 0)) > _movementThreshold.y ||
                Quaternion.Angle(Quaternion.Euler(0, (_useLocalSpace ? camera.transform.localEulerAngles.y : camera.transform.eulerAngles.y), 0), Quaternion.Euler(0, _lastCameraEuler.y, 0)) > _movementThreshold.x ||
                Vector3.Distance((_useLocalSpace? camera.transform.localPosition : camera.transform.position), _lastCameraPosition) > _distanceThreshold)
            {
                if (_forceUpdate)
                    _forceUpdate = false;

                _movePosition = targetPosition;

                _lastCameraEuler = _useLocalSpace ? camera.transform.localEulerAngles : camera.transform.eulerAngles;
                _lastCameraPosition = _useLocalSpace ? camera.transform.localPosition : camera.transform.position;
            }

            if (_useLocalSpace)
            {
                transform.localPosition = _placeOnAwake ? targetPosition : Vector3.SmoothDamp(transform.localPosition, _movePosition, ref _positionVelocity, _positionSmoothTime);
            }
            else
            {
                transform.position = _placeOnAwake ? targetPosition : Vector3.SmoothDamp(transform.position, _movePosition, ref _positionVelocity, _positionSmoothTime);
            }

            Quaternion targetRotation = Quaternion.identity;

            // Rotate the object to face the camera.
            if (_lookDirection == LookDirection.LookAwayFromCamera)
            {
                targetRotation = Quaternion.LookRotation((_useLocalSpace ? transform.localPosition : transform.position) - (_useLocalSpace ? camera.transform.localPosition : camera.transform.position), camera.transform.up);
            }
            else if (_lookDirection == LookDirection.LookAtCamera)
            {
                targetRotation = Quaternion.LookRotation((_useLocalSpace ? camera.transform.localPosition : camera.transform.position) - (_useLocalSpace ? transform.localPosition : transform.position), camera.transform.up);
            }

            if (_useLocalSpace)
            {
                transform.localRotation = _placeOnAwake ? targetRotation : Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
            }
            else
            {
                transform.rotation = _placeOnAwake ? targetRotation : Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
            }

            if (_placeOnAwake)
            {
                _placeOnAwake = false;
            }

            if (_lockZRotation)
            {
                if (_useLocalSpace)
                {
                    transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, 0);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
                }
            }

            if (_lockXRotation)
            {
                if (_useLocalSpace)
                {
                    transform.localRotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                }
            }
        }
    }
}

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
using System.Collections.Generic;
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// This class displays the current world scale information
    /// and allows the user to adjust the position marker between
    /// the different markers in the ruler.
    /// </summary>
    public class WorldScaleExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The world scale scene component attached to the main camera.")]
        private MLWorldScaleBehavior _worldScale = null;

        [SerializeField, Tooltip("The reference to the controller connection handler in the scene.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("Text to display the current distance and world scale.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("Ruler object to get marker position data from.")]
        private Ruler _ruler = null;

        [SerializeField, Tooltip("The Transform of the position marker that indicates the current position in the ruler.")]
        private Transform _positionMarker = null;

        private float[] _marks = null;
        private int _currentMarkIndex = 0;

        private const float POSITION_MARKER_Y_OFFSET = 0.03f;

        /// <summary>
        /// Register callbacks, assure all required variables are set and set position marker to start position.
        /// </summary>
        void Start()
        {
            if(_worldScale == null)
            {
                Debug.LogError("Error: WorldScaleExample._worldScale is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: WorldScaleExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: WorldScaleExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_ruler == null)
            {
                Debug.LogError("Error: WorldScaleExample._ruler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_positionMarker == null)
            {
                Debug.LogError("Error: WorldScaleExample._positionMarker is not set, disabling script.");
                enabled = false;
                return;
            }

            _marks = _ruler.Marks;
            if (_marks.Length > 0)
            {
                _currentMarkIndex = _marks.Length - 1;
                _positionMarker.localPosition = new Vector3(0, POSITION_MARKER_Y_OFFSET, _marks[_currentMarkIndex]);
            }

            _worldScale.OnUpdateEvent += _ruler.OnWorldScaleUpdate;

            #if PLATFORM_LUMIN
            // Register listeners.
            MLInput.OnControllerButtonDown += HandleOnButtonDown;
            MLInput.OnControllerTouchpadGestureStart += HandleOnTouchpadGestureStart;
            #endif
        }

        /// <summary>
        /// Unregister callbacks.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            // Unregister listeners.
            MLInput.OnControllerTouchpadGestureStart -= HandleOnTouchpadGestureStart;
            MLInput.OnControllerButtonDown -= HandleOnButtonDown;
            #endif

            _worldScale.OnUpdateEvent -= _ruler.OnWorldScaleUpdate;
        }

        /// <summary>
        /// Update status data with new information.
        /// </summary>
        void Update()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n\n", ControllerStatus.Text);

            _statusText.text += string.Format(
                "<color=#dbfb76><b>World Scale Data</b></color>\nMeasurement: {0}\nScale:{1}\nDistance:{2} {3}",
                _worldScale.Measurement.ToString(),
                _worldScale.Scale,
                _positionMarker.localPosition.z * _worldScale.Scale,
                _worldScale.Units);
        }

        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controller_id">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void HandleOnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId) && button == MLInput.Controller.Button.Bumper)
            {
                switch (_worldScale.Measurement)
                {
                    case MLWorldScaleBehavior.ScaleMeasurement.Meters:
                        _worldScale.Measurement = MLWorldScaleBehavior.ScaleMeasurement.Decimeters;
                        break;
                    case MLWorldScaleBehavior.ScaleMeasurement.Decimeters:
                        _worldScale.Measurement = MLWorldScaleBehavior.ScaleMeasurement.Centimeters;
                        break;
                    case MLWorldScaleBehavior.ScaleMeasurement.Centimeters:
                        _worldScale.Measurement = MLWorldScaleBehavior.ScaleMeasurement.CustomUnits;
                        break;
                    case MLWorldScaleBehavior.ScaleMeasurement.CustomUnits:
                        _worldScale.Measurement = MLWorldScaleBehavior.ScaleMeasurement.Meters;
                        break;
                    default:
                        Debug.LogError("Error: WorldScaleExample measurement type is an invalid value, disabling script.");
                        enabled = false;
                        return;
                }

                _worldScale.UpdateWorldScale();
            }
        }

        /// <summary>
        /// Handles the event for touchpad gesture start.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="gesture">The type of gesture that started.</param>
        private void HandleOnTouchpadGestureStart(byte controllerId, MLInput.Controller.TouchpadGesture gesture)
        {
            #if PLATFORM_LUMIN
            if (_controllerConnectionHandler.IsControllerValid(controllerId) && gesture.Type == MLInput.Controller.TouchpadGesture.GestureType.Swipe)
            {
                // Increase / Decrease the marker distance based on the swipe gesture.
                if (gesture.Direction == MLInput.Controller.TouchpadGesture.GestureDirection.Up || gesture.Direction == MLInput.Controller.TouchpadGesture.GestureDirection.Down)
                {
                    if (_marks.Length > 0)
                    {
                        _currentMarkIndex = (gesture.Direction == MLInput.Controller.TouchpadGesture.GestureDirection.Up) ?
                                            Mathf.Min(_currentMarkIndex + 1, _marks.Length - 1) :
                                            Mathf.Max(_currentMarkIndex - 1, 0);
                        _positionMarker.localPosition = new Vector3(0, POSITION_MARKER_Y_OFFSET, _marks[_currentMarkIndex]);
                    }
                }
            }
            #endif
        }
    }
}

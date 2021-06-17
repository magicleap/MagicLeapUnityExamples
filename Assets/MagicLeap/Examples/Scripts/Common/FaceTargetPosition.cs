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

namespace MagicLeap
{
    /// <summary>
    /// Utility class to look at an absolute position
    /// </summary>
    public class FaceTargetPosition : MonoBehaviour
    {
        private Vector3 _targetPosition;

        [SerializeField, Tooltip("Turning Speed (degrees per sec)")]
        private float _turningSpeed = 45.0f;

        public Vector3 TargetPosition
        {
            set
            {
                _targetPosition = value;
            }
        }

        public float TurningSpeed
        {
            set
            {
                _turningSpeed = value;
            }
        }

        /// <summary>
        /// Face towards target position while maintaining global up
        /// </summary>
        void Update ()
        {
            Vector3 desiredForward = _targetPosition - transform.position;
            if (desiredForward.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }
            Quaternion desiredOrientation = Quaternion.LookRotation(desiredForward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredOrientation, _turningSpeed * Time.deltaTime);
        }
    }
}

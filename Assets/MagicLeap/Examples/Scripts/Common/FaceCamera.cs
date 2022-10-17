// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This behavior rotates the transform to always look at the Main camera
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("Rotation Offset in Euler Angles")]
        Vector3 _rotationOffset = Vector3.zero;

        /// <summary>
        /// Initialize rotation
        /// </summary>
        void Start()
        {
            transform.LookAt(Camera.main.transform);
        }

        /// <summary>
        /// Update rotation to look at main camera
        /// </summary>
        void Update ()
        {
            transform.LookAt(Camera.main.transform);
            transform.rotation *= Quaternion.Euler(_rotationOffset);
        }
    }
}

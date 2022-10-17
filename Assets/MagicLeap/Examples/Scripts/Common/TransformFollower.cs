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
    /// This class implements the functionality for the object with this component
    /// to follow an input transform.
    /// </summary>
    public class TransformFollower : MonoBehaviour
    {
        [Tooltip("The object that should be followed.")]
        public Transform ObjectToFollow;

        [Tooltip("Following should respect(local) or ignore(world) hierarchy.")]
        public bool UseLocalTransform = true;

        /// <summary>
        /// Updates the transform of the object.
        /// </summary>
        void Update()
        {
            if (ObjectToFollow != null)
            {
                if (UseLocalTransform)
                {
                    transform.localPosition = ObjectToFollow.localPosition;
                    transform.localRotation = ObjectToFollow.localRotation;
                }
                else
                {
                    transform.position = ObjectToFollow.position;
                    transform.rotation = ObjectToFollow.rotation;
                }
            }
        }
    }
}

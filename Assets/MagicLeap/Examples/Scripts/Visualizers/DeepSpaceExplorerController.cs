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
    /// This class makes it easier to set the radius of the orbit of the Deep Space Explorer.
    /// </summary>
    public class DeepSpaceExplorerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Radius of the orbit of the rockets")]
        private Transform _xOffset = null;

        public float OrbitRadius
        {
            set
            {
                _xOffset.localPosition = new Vector3(value, 0, 0);
            }
        }

        /// <summary>
        /// Validate input variables.
        /// </summary>
        void Start ()
        {
            if (null == _xOffset)
            {
                Debug.LogError("Error: DeepSpaceExplorerController._xOffset is not set, disabling script");
                enabled = false;
                return;
            }
        }
    }
}

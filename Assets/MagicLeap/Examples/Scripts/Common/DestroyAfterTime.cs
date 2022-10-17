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
    /// Utility class to destroy after a set time.
    /// Note that the count down can be cancelled by destroying this script
    /// </summary>
    public class DestroyAfterTime : MonoBehaviour
    {
        [SerializeField, Tooltip("Time delay before self-destruct")]
        private float _duration = 5;

        private float _timeStart;

        public float Duration
        {
            set
            {
                _timeStart = Time.time;
                _duration = value;
            }
        }

        /// <summary>
        /// Start the self-destruct countdown
        /// </summary>
        void Start ()
        {
            _timeStart = Time.time;
        }

        /// <summary>
        /// Count down and destruction
        /// </summary>
        void Update()
        {
            if (Time.time > _timeStart + _duration)
            {
                Destroy(gameObject);
            }
        }
    }
}

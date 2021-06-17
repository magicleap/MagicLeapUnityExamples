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

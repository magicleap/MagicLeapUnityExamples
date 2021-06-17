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
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core.StarterKit;

namespace MagicLeap
{
    /// <summary>
    /// Class for tracking a specific Keypose and handling confidence value
    /// based sprite renderer color changes.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class KeyPoseVisualizer : MonoBehaviour
    {
        private const float ROTATION_SPEED = 100.0f;
        private const float CONFIDENCE_THRESHOLD = 0.95f;

        #pragma warning disable 414
        [SerializeField, Tooltip("KeyPose to track.")]
        private MLHandTracking.HandKeyPose _keyPoseToTrack = MLHandTracking.HandKeyPose.NoPose;
        #pragma warning restore 414

        [Space, SerializeField, Tooltip("Flag to specify if left hand should be tracked.")]
        private bool _trackLeftHand = true;

        [SerializeField, Tooltip("Flag to specify id right hand should be tracked.")]
        private bool _trackRightHand = true;

        private SpriteRenderer _spriteRenderer = null;

        /// <summary>
        /// Initializes variables.
        /// </summary>
        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Updates color of sprite renderer material based on confidence of the KeyPose.
        /// </summary>
        void Update()
        {
            float confidenceLeft =  0.0f;
            float confidenceRight = 0.0f;

            if (_trackLeftHand)
            {
                #if PLATFORM_LUMIN
                confidenceLeft = GetKeyPoseConfidence(MLHandTracking.Left);
                #endif
            }

            if (_trackRightHand)
            {
                #if PLATFORM_LUMIN
                confidenceRight = GetKeyPoseConfidence(MLHandTracking.Right);
                #endif
            }

            float confidenceValue = Mathf.Max(confidenceLeft, confidenceRight);

            Color currentColor = Color.white;

            if (confidenceValue > 0.0f)
            {
                currentColor.r = 1.0f - confidenceValue;
                currentColor.g = 1.0f;
                currentColor.b = 1.0f - confidenceValue;
            }

            // When the keypose is detected for both hands, spin the image continuously.
            if (confidenceValue > 0.0f && confidenceLeft >= CONFIDENCE_THRESHOLD && confidenceRight >= CONFIDENCE_THRESHOLD)
            {
                transform.Rotate(Vector3.up, ROTATION_SPEED * Time.deltaTime, Space.Self);
            }
            else if(confidenceValue > 0.0f && confidenceRight > confidenceLeft)
            {
                // Shows Right-Hand Orientation.
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, 180, 0), ROTATION_SPEED * Time.deltaTime);
            }
            else
            {
                // Shows Left-Hand Orientation (Default).
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, 0, 0), ROTATION_SPEED * Time.deltaTime);
            }

            _spriteRenderer.material.color = currentColor;
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Gets the confidence value for the hand being tracked.
        /// </summary>
        /// <param name="hand">Hand to check the confidence value on.</param>
        private float GetKeyPoseConfidence(MLHandTracking.Hand hand)
        {
            if (hand != null)
            {
                if (hand.KeyPose == _keyPoseToTrack)
                {
                    return hand.HandKeyPoseConfidence;
                }
            }
            return 0.0f;
        }
        #endif
    }
}

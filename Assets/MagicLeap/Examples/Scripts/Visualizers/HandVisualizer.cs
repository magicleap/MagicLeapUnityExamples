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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Component used to help visualize the keypoints on a hand by attaching
    /// primitive game objects to the detected keypoint positions.
    /// </summary>
    public class HandVisualizer : MonoBehaviour
    {
        #pragma warning disable 414
        [SerializeField, Tooltip("The hand to visualize.")]
        private MLHandTracking.HandType _handType = MLHandTracking.HandType.Left;
        #pragma warning restore 414

        [SerializeField, Tooltip("The GameObject to use for the Hand Center.")]
        private Transform _center = null;

        [Header("Hand Keypoint Colors")]

        [SerializeField, Tooltip("The color assigned to the pinky finger keypoints.")]
        private Color _pinkyColor = Color.cyan;

        [SerializeField, Tooltip("The color assigned to the ring finger keypoints.")]
        private Color _ringColor = Color.red;

        [SerializeField, Tooltip("The color assigned to the middle finger keypoints.")]
        private Color _middleColor = Color.blue;

        [SerializeField, Tooltip("The color assigned to the index finger keypoints.")]
        private Color _indexColor = Color.magenta;

        [SerializeField, Tooltip("The color assigned to the thumb keypoints.")]
        private Color _thumbColor = Color.yellow;

        [SerializeField, Tooltip("The color assigned to the wrist keypoints.")]
        private Color _wristColor = Color.white;

        private List<Transform> _pinkyFinger = null;
        private List<Transform> _ringFinger = null;
        private List<Transform> _middleFinger = null;
        private List<Transform> _indexFinger = null;
        private List<Transform> _thumb = null;
        private List<Transform> _wrist = null;

        #if PLATFORM_LUMIN
        /// <summary>
        /// Returns the hand based on the hand type.
        /// </summary>
        private MLHandTracking.Hand Hand
        {
            get
            {
                if (_handType == MLHandTracking.HandType.Left)
                {
                    return MLHandTracking.Left;
                }
                else
                {
                    return MLHandTracking.Right;
                }
            }
        }
        #endif

        /// <summary>
        /// Initializes the lists of hand transforms.
        /// </summary>
        void Start()
        {
            if (_center == null)
            {
                Debug.LogError("Error: HandVisualizer._center is not set, disabling script.");
                enabled = false;
                return;
            }

            Initialize();
        }

        /// <summary>
        /// Update the keypoint positions.
        /// </summary>
        void Update()
        {
            #if PLATFORM_LUMIN
            if (MLHandTracking.IsStarted)
            {
                // Pinky
                for (int i = 0; i < Hand.Pinky.KeyPoints.Count; ++i)
                {
                    _pinkyFinger[i].localPosition = Hand.Pinky.KeyPoints[i].Position;
                    _pinkyFinger[i].gameObject.SetActive(Hand.IsVisible);
                }

                // Ring
                for (int i = 0; i < Hand.Ring.KeyPoints.Count; ++i)
                {
                    _ringFinger[i].localPosition = Hand.Ring.KeyPoints[i].Position;
                    _ringFinger[i].gameObject.SetActive(Hand.IsVisible);
                }

                // Middle
                for (int i = 0; i < Hand.Middle.KeyPoints.Count; ++i)
                {
                    _middleFinger[i].localPosition = Hand.Middle.KeyPoints[i].Position;
                    _middleFinger[i].gameObject.SetActive(Hand.IsVisible);
                }

                // Index
                for (int i = 0; i < Hand.Index.KeyPoints.Count; ++i)
                {
                    _indexFinger[i].localPosition = Hand.Index.KeyPoints[i].Position;
                    _indexFinger[i].gameObject.SetActive(Hand.IsVisible);
                }

                // Thumb
                for (int i = 0; i < Hand.Thumb.KeyPoints.Count; ++i)
                {
                    _thumb[i].localPosition = Hand.Thumb.KeyPoints[i].Position;
                    _thumb[i].gameObject.SetActive(Hand.IsVisible);
                }

                // Wrist
                for (int i = 0; i < Hand.Wrist.KeyPoints.Count; ++i)
                {
                    _wrist[i].localPosition = Hand.Wrist.KeyPoints[i].Position;
                    _wrist[i].gameObject.SetActive(Hand.IsVisible);
                }

                // Hand Center
                _center.localPosition = Hand.Center;
                _center.gameObject.SetActive(Hand.IsVisible);
            }
            #endif
        }

        /// <summary>
        /// Initialize the available KeyPoints.
        /// </summary>
        private void Initialize()
        {
            // Pinky
            _pinkyFinger = new List<Transform>();

            #if PLATFORM_LUMIN
            for (int i = 0; i < Hand.Pinky.KeyPoints.Count; ++i)
            {
                _pinkyFinger.Add(CreateKeyPoint(Hand.Pinky.KeyPoints[i], _pinkyColor).transform);
            }
            #endif

            // Ring
            _ringFinger = new List<Transform>();

            #if PLATFORM_LUMIN
            for (int i = 0; i < Hand.Ring.KeyPoints.Count; ++i)
            {
                _ringFinger.Add(CreateKeyPoint(Hand.Ring.KeyPoints[i], _ringColor).transform);
            }
            #endif

            // Middle
            _middleFinger = new List<Transform>();

            #if PLATFORM_LUMIN
            for (int i = 0; i < Hand.Middle.KeyPoints.Count; ++i)
            {
                _middleFinger.Add(CreateKeyPoint(Hand.Middle.KeyPoints[i], _middleColor).transform);
            }
            #endif

            // Index
            _indexFinger = new List<Transform>();

            #if PLATFORM_LUMIN
            for (int i = 0; i < Hand.Index.KeyPoints.Count; ++i)
            {
                _indexFinger.Add(CreateKeyPoint(Hand.Index.KeyPoints[i], _indexColor).transform);
            }
            #endif

            // Thumb
            _thumb = new List<Transform>();

            #if PLATFORM_LUMIN
            for (int i = 0; i < Hand.Thumb.KeyPoints.Count; ++i)
            {
                _thumb.Add(CreateKeyPoint(Hand.Thumb.KeyPoints[i], _thumbColor).transform);
            }
            #endif

            // Wrist
            _wrist = new List<Transform>();

            #if PLATFORM_LUMIN
            for (int i = 0; i < Hand.Wrist.KeyPoints.Count; ++i)
            {
                _wrist.Add(CreateKeyPoint(Hand.Wrist.KeyPoints[i], _wristColor).transform);
            }
            #endif
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Create a GameObject for the desired KeyPoint.
        /// </summary>
        /// <param name="keyPoint">The keypoint to reference.</param>
        /// <param name="color">The color to represent this keypoint</param>
        private GameObject CreateKeyPoint(MLHandTracking.KeyPoint keyPoint, Color color)
        {
            GameObject newObject;

            newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newObject.SetActive(false);
            newObject.transform.SetParent(transform);
            newObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            newObject.name = keyPoint.ToString();
            newObject.GetComponent<Renderer>().material.color = color;

            return newObject;
        }
        #endif
    }
}

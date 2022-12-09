// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    using HandTracking = InputSubsystem.Extensions.MLHandTracking;

    /// <summary>
    /// Component used to help visualize the keypoints on a hand by attaching
    /// primitive game objects to the detected keypoint positions.
    /// </summary>
    public class HandVisualizer : MonoBehaviour
    {
#pragma warning disable 414
        [SerializeField, Tooltip("The hand to visualize.")]
        private HandTracking.HandType _handType = HandTracking.HandType.Left;
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

        [SerializeField]
        private Material keypointMaterial = null;

        [SerializeField, Tooltip("Value to hide the Visualizer if the confidence is below.")]
        [Range(0.0f, 1.0f)]
        private float confidenceThreshold = 0.6f;

        private List<Transform> _pinkyFinger = null;
        private List<Transform> _ringFinger = null;
        private List<Transform> _middleFinger = null;
        private List<Transform> _indexFinger = null;
        private List<Transform> _thumb = null;
        private List<Transform> _wrist = null;


        private List<Bone> _pinkyFingerBones = new List<Bone>();
        private List<Bone> _ringFingerBones = new List<Bone>();
        private List<Bone> _middleFingerBones = new List<Bone>();
        private List<Bone> _indexFingerBones = new List<Bone>();
        private List<Bone> _thumbBones = new List<Bone>();
        private List<Bone> _wristBones = new List<Bone>();

        private InputDevice handDevice;

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
        }

        /// <summary>
        /// Update the keypoint positions.
        /// </summary>
        void Update()
        {
            if (!this.handDevice.isValid)
            {
                Initialize();
                return;
            }

            GetFingerBones();

            this.handDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.Confidence, out float handConfidence);

            bool highConfidence = (handConfidence > confidenceThreshold);

            // Pinky
            for (int i = 0; i < this._pinkyFingerBones.Count; ++i)
            {
                this._pinkyFingerBones[i].TryGetPosition(out Vector3 bonePosition);
                this._pinkyFinger[i].localPosition = bonePosition;
                this._pinkyFingerBones[i].TryGetRotation(out Quaternion boneRotation);
                this._pinkyFinger[i].localRotation = boneRotation;
                this._pinkyFinger[i].gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Pinky, i));
            }

            // Ring
            for (int i = 0; i < this._ringFingerBones.Count; ++i)
            {
                this._ringFingerBones[i].TryGetPosition(out Vector3 bonePosition);
                this._ringFinger[i].localPosition = bonePosition;
                this._ringFingerBones[i].TryGetRotation(out Quaternion boneRotation);
                this._ringFinger[i].localRotation = boneRotation;
                this._ringFinger[i].gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Ring, i));
            }

            // Middle
            for (int i = 0; i < this._middleFingerBones.Count; ++i)
            {
                this._middleFingerBones[i].TryGetPosition(out Vector3 bonePosition);
                this._middleFinger[i].localPosition = bonePosition;
                this._middleFingerBones[i].TryGetRotation(out Quaternion boneRotation);
                this._middleFinger[i].localRotation = boneRotation;
                this._middleFinger[i].gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Middle, i));
            }

            // Index
            for (int i = 0; i < this._indexFingerBones.Count; ++i)
            {
                this._indexFingerBones[i].TryGetPosition(out Vector3 bonePosition);
                this._indexFinger[i].localPosition = bonePosition;
                this._indexFingerBones[i].TryGetRotation(out Quaternion boneRotation);
                this._indexFinger[i].localRotation = boneRotation;
                this._indexFinger[i].gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Index, i));
            }

            // Thumb
            for (int i = 0; i < this._thumbBones.Count; ++i)
            {
                this._thumbBones[i].TryGetPosition(out Vector3 bonePosition);
                this._thumb[i].localPosition = bonePosition;
                this._thumbBones[i].TryGetRotation(out Quaternion boneRotation);
                this._thumb[i].localRotation = boneRotation;
                this._thumb[i].gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Thumb, i));
            }

            // Wrist
            for (int i = 0; i < this._wristBones.Count; ++i)
            {
                this._wristBones[i].TryGetPosition(out Vector3 bonePosition);
                this._wrist[i].localPosition = bonePosition;
                this._wristBones[i].TryGetRotation(out Quaternion boneRotation);
                this._wrist[i].localRotation = boneRotation;
                this._wrist[i].gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Wrist, i));
            }

            handDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePosition);
            handDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation);

            // Hand Center
            _center.localPosition = devicePosition;
            _center.localRotation = deviceRotation;
            // Hand Center only has one keypoint so its' index would be 0 for getting its' status.
            _center.gameObject.SetActive(highConfidence && HandTracking.GetKeyPointStatus(this.handDevice, HandTracking.KeyPointLocation.Center, 0));
        }

        private void GetFingerBones()
        {
            // Hand Key Points
            if (handDevice.TryGetFeatureValue(CommonUsages.handData, out UnityEngine.XR.Hand hand))
            {
                hand.TryGetFingerBones(UnityEngine.XR.HandFinger.Index, this._indexFingerBones);
                hand.TryGetFingerBones(UnityEngine.XR.HandFinger.Middle, this._middleFingerBones);
                hand.TryGetFingerBones(UnityEngine.XR.HandFinger.Ring, this._ringFingerBones);
                hand.TryGetFingerBones(UnityEngine.XR.HandFinger.Pinky, this._pinkyFingerBones);
                hand.TryGetFingerBones(UnityEngine.XR.HandFinger.Thumb, this._thumbBones);

                this._wristBones.Clear();
                handDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.WristCenter, out Bone wristBone);
                this._wristBones.Add(wristBone);
                handDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.WristRadial, out wristBone);
                this._wristBones.Add(wristBone);
                handDevice.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.WristUlnar, out wristBone);
                this._wristBones.Add(wristBone);
            }
        }
        /// <summary>
        /// Initialize the available KeyPoints.
        /// </summary>
        private void Initialize()
        {
            if (!this.handDevice.isValid)
            {
                this.handDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.HandTracking | (this._handType == HandTracking.HandType.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right));
            }

            GetFingerBones();

            // Pinky
            _pinkyFinger = new List<Transform>();

            for (int i = 0; i < this._pinkyFingerBones.Count; ++i)
            {
                _pinkyFinger.Add(CreateKeyPoint(_pinkyColor).transform);
            }

            // Ring
            _ringFinger = new List<Transform>();

            for (int i = 0; i < this._ringFingerBones.Count; ++i)
            {
                _ringFinger.Add(CreateKeyPoint(_ringColor).transform);
            }

            // Middle
            _middleFinger = new List<Transform>();

            for (int i = 0; i < this._middleFingerBones.Count; ++i)
            {
                _middleFinger.Add(CreateKeyPoint(_middleColor).transform);
            }

            // Index
            _indexFinger = new List<Transform>();

            for (int i = 0; i < this._indexFingerBones.Count; ++i)
            {
                _indexFinger.Add(CreateKeyPoint(_indexColor).transform);
            }

            // Thumb
            _thumb = new List<Transform>();

            for (int i = 0; i < this._thumbBones.Count; ++i)
            {
                _thumb.Add(CreateKeyPoint(_thumbColor).transform);
            }

            // Wrist
            _wrist = new List<Transform>();

            for (int i = 0; i < this._wristBones.Count; ++i)
            {
                _wrist.Add(CreateKeyPoint(_wristColor).transform);
            }
        }

        /// <summary>
        /// Create a GameObject for the desired KeyPoint.
        /// </summary>
        /// <param name="color">The color to represent this keypoint</param>
        private GameObject CreateKeyPoint(Color color)
        {
            GameObject newObject;

            newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newObject.SetActive(false);
            newObject.transform.SetParent(transform);
            newObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            if (keypointMaterial != null)
            {
                newObject.GetComponent<Renderer>().material = new Material(keypointMaterial);
            }
            newObject.GetComponent<Renderer>().material.color = color;

            return newObject;
        }
    }
}

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// Utility class that sets a ruler and markers based on specific.
    /// </summary>
    public class Ruler : MonoBehaviour
    {
        [SerializeField, Tooltip("Length of the ruler in meters.")]
        private float _length = 1.0f;

        [SerializeField, Tooltip("Length of the ruler in meters.")]
        private float[] _distanceMarks = null;

        [SerializeField, Tooltip("Prefab to set as marker on the ruler.")]
        private GameObject _distanceMarkerPrefab = null;

        private Dictionary<float, TextMesh> _markers = new Dictionary<float, TextMesh>();

        /// <summary>
        /// Contains the different meassurement marks in the ruler.
        /// </summary>
        public float[] Marks
        {
            get
            {
                return _distanceMarks;
            }
        }

        /// <summary>
        /// Ensures markers don't pass ruler length.
        /// </summary>
        void OnValidate()
        {
            for (int i = 0; i < _distanceMarks.Length; ++i)
            {
                _distanceMarks[i] = Mathf.Clamp(_distanceMarks[i], 0.0f, _length);
            }
        }

        /// <summary>
        /// Set ruler scale based on length and create the markers.
        /// </summary>
        void Awake()
        {
            // Set ruler size and texture based on length parameter.
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, _length);
            Material mat = GetComponent<Renderer>().material;
            mat.mainTextureScale = new Vector2(mat.mainTextureScale.x, _length);

            foreach (float f in _distanceMarks)
            {
                GameObject obj = Instantiate(_distanceMarkerPrefab);
                obj.transform.position = transform.position + f * transform.forward + 0.01f * transform.up;
                obj.transform.parent = transform;
                _markers.Add(f, obj.GetComponentInChildren<TextMesh>());
            }

            foreach (KeyValuePair<float, TextMesh> entry in _markers)
            {
                entry.Value.text = entry.Key.ToString();
            }
        }

        /// <summary>
        /// Updates measurement values based on new scale and measurement units.
        /// </summary>
        /// <param name="scale"> New scale. </param>
        /// <param name="units"> New measurement units. </param>
        public void OnWorldScaleUpdate(float scale, string units)
        {
            foreach (KeyValuePair<float, TextMesh> entry in _markers)
            {
                entry.Value.text = entry.Key * scale + " " + units;
            }
        }
    }
}

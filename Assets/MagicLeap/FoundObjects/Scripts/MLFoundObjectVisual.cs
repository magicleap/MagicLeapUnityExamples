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
    /// Maintains the found object visual by updating the transform and text label.
    /// </summary>
    public class MLFoundObjectVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("The transform of the bounding box outline mesh.")]
        private Transform _outline = null;

        [SerializeField, Tooltip("The text used for the object label.")]
        private TextMesh _label = null;

        public void UpdateVisual(Vector3 position, Quaternion rotation, Vector3 extents, string label = "Found Object")
        {
            transform.position = position;
            transform.rotation = rotation;
            _outline.localScale = extents;
            _label.text = label;
        }
    }
}

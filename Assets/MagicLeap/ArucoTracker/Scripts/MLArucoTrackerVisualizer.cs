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
namespace MagicLeap
{
    using UnityEngine;
    using UnityEngine.XR.MagicLeap;
    using MagicLeap.Core;

    public class MLArucoTrackerVisualizer : MonoBehaviour
    {
        [SerializeField]
        private MLArucoTrackerBehavior _trackerBehavior;

        private TextMesh _textMesh;

        #if PLATFORM_LUMIN
        void Start()
        {
            _textMesh = GetComponentInChildren<TextMesh>();
        }

        void Update()
        {
            _textMesh.text = string.Format("Id: {0} \n ReprojectionError: {1}", _trackerBehavior.Marker?.Id, _trackerBehavior.Marker?.ReprojectionError);

            bool enable = _trackerBehavior.Marker?.Status == MLArucoTracker.Marker.TrackingStatus.Tracked;
            if(gameObject.activeSelf != enable)
            {
                gameObject.SetActive(enable);
            }
        }
#endif
    }
}

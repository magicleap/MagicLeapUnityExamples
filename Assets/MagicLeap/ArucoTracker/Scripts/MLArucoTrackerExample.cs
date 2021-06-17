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
    using System.Collections.Generic;
    using MagicLeap.Core;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.XR.MagicLeap;

    public class MLArucoTrackerExample : MonoBehaviour
    {
        public MLArucoTracker.Settings trackerSettings = MLArucoTracker.Settings.Create();
        public GameObject MLArucoMarkerPrefab;
        public Text statusText;

        private HashSet<int> _arucoMarkerIds = new HashSet<int>();
        private bool _bumperReleased = true;
        private bool _triggerReleased = true;
        private const MLArucoTracker.DictionaryName MaxDictionaryEnum = MLArucoTracker.DictionaryName.DICT_ARUCO_ORIGINAL;
        private MLInput.Controller controller = null;

        void Start()
        {
#if PLATFORM_LUMIN
            MLArucoTracker.UpdateSettings(trackerSettings);
            MLArucoTracker.OnMarkerStatusChange += OnMarkerStatusChange;
            SetStatusText();
            #endif
        }

        void Update()
        {
#if PLATFORM_LUMIN

            
            if(controller == null)
            {
                controller = MLInput.GetController(0);
                return;
            }
            
            if (controller.IsBumperDown == true)
            {
                if(_bumperReleased)
                {
                    IterateTrackerDictionarySetting();
                }

                _bumperReleased = false;
            }
            else
            {
                _bumperReleased = true;
            }

            if (controller.TriggerValue >= 0.25f && !MLInputModuleBehavior.IsOverUI)
            {
                if(_triggerReleased)
                {
                    ToggleAruco();
                    _triggerReleased = false;
                }
            }
            else
            {
                _triggerReleased = true;
            }
#endif
        }

        void OnApplicationPause(bool pause)
        {
#if PLATFORM_LUMIN
            if (pause)
            {
                DisableAruco();
            }
            else
            {
                if(MLPrivileges.RequestPrivilege(MLPrivileges.Id.CameraCapture).Result == MLResult.Code.PrivilegeGranted)
                {
                    EnableAruco();
                }
            }
#endif
        }

        void OnDestroy()
        {
#if PLATFORM_LUMIN
            if (MLArucoTracker.IsStarted)
            {
                MLArucoTracker.OnMarkerStatusChange -= OnMarkerStatusChange;
            }
#endif
        }

        private void ToggleAruco()
        {
            if(trackerSettings.Enabled)
            {
                DisableAruco();
            }
            else
            {
                EnableAruco();
            }
        }

        private void DisableAruco()
        {

            trackerSettings.Enabled = false;
#if PLATFORM_LUMIN
            MLArucoTracker.UpdateSettings(trackerSettings);
#endif
        }

        private void EnableAruco()
        {
            trackerSettings.Enabled = true;
#if PLATFORM_LUMIN
            MLArucoTracker.UpdateSettings(trackerSettings);
#endif
        }

        private void SetStatusText()
        {
            statusText.text = $"Tracker Enabled: {trackerSettings.Enabled}\n\n";
            statusText.text = $"Dictionary: {trackerSettings.Dictionary}\n\n";
            statusText.text += "ArUco markers detected:\n";
            foreach (int markerId in _arucoMarkerIds)
            {
                statusText.text += string.Format("Marker {0}\n", markerId);
            }
        }

        private void OnMarkerStatusChange(MLArucoTracker.Marker marker, MLArucoTracker.Marker.TrackingStatus status)
        {
#if PLATFORM_LUMIN
            if (status == MLArucoTracker.Marker.TrackingStatus.Tracked)
            {
                if (_arucoMarkerIds.Contains(marker.Id))
                {
                    return;
                }

                GameObject arucoMarker = Instantiate(MLArucoMarkerPrefab);
                MLArucoTrackerBehavior arucoBehavior = arucoMarker.GetComponent<MLArucoTrackerBehavior>();
                arucoBehavior.MarkerId = marker.Id;
                arucoBehavior.MarkerDictionary = MLArucoTracker.TrackerSettings.Dictionary;

                _arucoMarkerIds.Add(marker.Id);
            }
            else if(_arucoMarkerIds.Contains(marker.Id))
            {
                _arucoMarkerIds.Remove(marker.Id);
            }

            SetStatusText();
#endif
        }

        private void IterateTrackerDictionarySetting()
        {
            if (trackerSettings.Dictionary == MaxDictionaryEnum)
            {
                trackerSettings.Dictionary = 0;
            }
            else
            {
                trackerSettings.Dictionary++;
            }
#if PLATFORM_LUMIN
            MLArucoTracker.UpdateSettings(trackerSettings);
#endif
            _arucoMarkerIds.Clear();
            SetStatusText();
        }

    }

}

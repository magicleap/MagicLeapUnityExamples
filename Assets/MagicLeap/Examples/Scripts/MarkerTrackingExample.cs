// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MagicLeap.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using static UnityEngine.XR.MagicLeap.MLMarkerTracker;
using MarkerSettings = UnityEngine.XR.MagicLeap.MLMarkerTracker.TrackerSettings;

namespace MagicLeap.Examples
{
    public class MarkerTrackingExample : MonoBehaviour
    {
        [Tooltip("If <c> true </c>, Marker Scanner will begin scanning on start and not require controller input.")]
        public bool AlwaysOn = false;

        private bool wasAlwaysOn = false;

        [SerializeField, Tooltip("The status text for the UI.")]
        private Text statusText = null;

        [SerializeField]
        private MarkerVisual markerVisualPrefab;

        [SerializeField, Tooltip("Wait for a given amount of time before removing unobsereved trackers.")]
        private bool removeMarkersUsingTimeStamps = false;

        [SerializeField, Tooltip("The timeout duration before removing unobserved trackers. Only used if removeMarkersUsingTimeStamps is set.")]
        private float markerTrackerTimeout = 0.5f;

        private Transform xrOrigin;

        /// <summary>
        /// The marker types that are enabled for this scanner. Enable markers by
        /// combining any number of <c> MarkerType </c> flags using '|' (bitwise 'or').
        /// </summary>
        [HideInInspector]
        public MarkerType MarkerTypes = MarkerType.All;

        /// <summary>
        ///     Aruco dictionary to use.
        /// </summary>
        [HideInInspector]
        public ArucoDictionaryName ArucoDicitonary;

        /// <summary>
        ///     Aruco marker size to use (in meters).
        /// </summary>
        [HideInInspector]
        public float ArucoMarkerSize = 0.1f;

        /// <summary>
        ///     The physical size of the QR code that shall be tracked (in meters). The physical size is
        ///     important to know, because once a QR code is detected we can only determine its
        ///     3D position when we know its correct size. The size of the QR code is given in
        ///     meters and represents the length of one side of the square code(without the
        ///     outer margin). Min size: As a rule of thumb the size of a QR code should be at
        ///     least a 10th of the distance you intend to scan it with a camera device. Higher
        ///     version markers with higher information density might need to be larger than
        ///     that to be detected reliably. Max size: Our camera needs to see the whole
        ///     marker at once. If it's too large, we won't detect it.
        /// </summary>
        [HideInInspector]
        public float QRCodeSize = 0.1f;

        /// <summary>
        ///     Represents the different tracker profiles used to optimize marker tracking in difference use cases.
        /// </summary>
        [HideInInspector]
        public Profile TrackerProfile = Profile.Default;

        /// <summary>
        ///     Obsolete: If <c> true </c>, Marker Scanner will detect markers and track QR codes.
        ///     When enabled, Marker Scanner will gain access to the camera and start
        ///     scanning markers. When disabled Marker Scanner will release the camera and
        ///     stop scanning markers. Internal state of the scanner will be maintained.
        /// </summary>
        [HideInInspector]
        public bool EnableMarkerScanning;

        /// <summary>
        ///     A hint to the back-end the max frames per second hat should be analyzed.
        /// </summary>
        [HideInInspector]
        public FPSHint FPSHint;

        /// <summary>
        ///     A hint to the back-end the resolution that should be used.
        /// </summary>
        [HideInInspector]
        public ResolutionHint ResolutionHint;

        /// <summary>
        ///     A hint to the back-end for the cameras that should be used.
        /// </summary>
        [HideInInspector]
        public CameraHint CameraHint;

        /// <summary>
        ///     In order to improve performance, the detectors don't always run on the full
        ///     frame.Full frame analysis is however necessary to detect new markers that
        ///     weren't detected before. Use this option to control how often the detector may
        ///     detect new markers and its impact on tracking performance.
        /// </summary>
        [HideInInspector]
        public FullAnalysisIntervalHint FullAnalysisIntervalHint;

        /// <summary>
        ///     This option provides control over corner refinement methods and a way to
        ///     balance detection rate, speed and pose accuracy. Always available and
        ///     applicable for Aruco and April tags.
        /// </summary>
        [HideInInspector]
        public CornerRefineMethod CornerRefineMethod;

        /// <summary>
        ///     Run refinement step that uses marker edges to generate even more accurate
        ///     corners, but slow down tracking rate overall by consuming more compute.
        ///     Aruco/April tags only.
        /// </summary>
        [HideInInspector]
        public bool UseEdgeRefinement;


        private List<KeyValuePair<string, MarkerVisual>> markers = new();
        private ASCIIEncoding asciiEncoder = new ASCIIEncoding();
        public MarkerSettings markerSettings;
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        void Awake()
        {
            wasAlwaysOn = AlwaysOn;
        }

        void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
            xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>().transform;
            EnableMarkerTrackerExample();
        }

        private void Update()
        {
            // If scanning is enabled from start don't clear flag by input.
            if (!AlwaysOn)
            {
                if (controllerActions.Bumper.IsPressed())
                    _ = MLMarkerTracker.StartScanningAsync();
                else
                    _ = MLMarkerTracker.StopScanningAsync();
            }

            UpdateVisibleTrackers();
            SetStatusText();
        }

        private void OnEnable()
        {
            AlwaysOn = wasAlwaysOn;
            MLMarkerTracker.OnMLMarkerTrackerResultsFoundArray += OnMLMarkerTrackerResultsFoundArray;
        }

        private void OnDisable()
        {
            wasAlwaysOn = AlwaysOn;
            AlwaysOn = false;
            if (MLMarkerTracker.IsScanning)
            {
                _ = MLMarkerTracker.StopScanningAsync();
            }
            MLMarkerTracker.OnMLMarkerTrackerResultsFoundArray -= OnMLMarkerTrackerResultsFoundArray;
        }

        private void OnDestroy()
        {
            AlwaysOn = false;
            if (MLMarkerTracker.IsScanning)
            {
                _ = MLMarkerTracker.StopScanningAsync();
            }
            mlInputs.Dispose();
        }

        private void OnMLMarkerTrackerResultsFoundArray(MarkerData[] dataArray)
        {
            if (!removeMarkersUsingTimeStamps)
            {
                RemoveNotVisibleTrackers(dataArray);
            }
            foreach (MarkerData data in dataArray)
            {
                ProcessSingleMarker(data);
            }
        }

        private void ProcessSingleMarker(MarkerData data)
        {
            switch (data.Type)
            {
                case MarkerType.Aruco_April:
                    {
                        string id = data.ArucoData.Id.ToString();
                        var existingMarker = markers.Find(x => x.Key == id);
                        if (!string.IsNullOrEmpty(existingMarker.Key))
                        {
                            MarkerVisual marker = existingMarker.Value;
                            marker.Set(data);
                        }
                        else
                        {
                            MarkerVisual marker = Instantiate(markerVisualPrefab, xrOrigin);
                            markers.Add(new KeyValuePair<string, MarkerVisual>(id, marker));
                            marker.Set(data);
                        }

                        break;
                    }

                case MarkerType.EAN_13:
                case MarkerType.UPC_A:
                case MarkerType.QR:
                    {
                        string id = asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
                        string markerText =
                            $"\nType: {Enum.GetName(typeof(MarkerType), data.Type)}\nReprojection Error: {data.ReprojectionError}\n Data:{id}";
                        var existingMarker = markers.Find(x => x.Key == id);
                        if (!string.IsNullOrEmpty(existingMarker.Key))
                        {
                            MarkerVisual marker = existingMarker.Value;
                            marker.Set(data, markerText);
                        }
                        else
                        {
                            MarkerVisual marker = Instantiate(markerVisualPrefab, xrOrigin);
                            markers.Add(new KeyValuePair<string, MarkerVisual>(id, marker));
                            marker.Set(data, markerText);
                        }

                        break;
                    }
            }
        }

        private async void EnableMarkerTrackerExample()
        {
            MarkerTypes = (int)MarkerTypes == -1 ? MarkerType.All : MarkerTypes;
            var customProfile = TrackerProfile == Profile.Custom ? TrackerSettings.CustomProfile.Create(FPSHint, ResolutionHint, CameraHint, FullAnalysisIntervalHint, CornerRefineMethod, UseEdgeRefinement) : default;
            markerSettings = TrackerSettings.Create(AlwaysOn, MarkerTypes, QRCodeSize, ArucoDicitonary, ArucoMarkerSize, TrackerProfile, customProfile);
            MLResult res = await SetSettingsAsync(markerSettings);
        }

        private void UpdateVisibleTrackers()
        {
            if (removeMarkersUsingTimeStamps)
            {
                UpdateVisibleTrackersByTimeStamp();
            }
        }

        private void UpdateVisibleTrackersByTimeStamp()
        {
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                MarkerVisual marker = markers[i].Value;
                if (!(marker.Timestamp - Time.time > markerTrackerTimeout))
                    continue;

                Destroy(marker.gameObject);
                markers.RemoveAt(i);
            }
        }

        private void RemoveNotVisibleTrackers(MarkerData[] dataArray)
        {
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                MarkerVisual marker = markers[i].Value;

                if (!dataArray.Any(x =>
                {
                    if (x.Type != marker.Type)
                        return false;

                    string id = default;
                    switch (marker.Type)
                    {
                        case MarkerType.Aruco_April:
                            id = x.ArucoData.Id.ToString();
                            break;
                        case MarkerType.EAN_13:
                        case MarkerType.UPC_A:
                        case MarkerType.QR:
                            id = asciiEncoder.GetString(x.BinaryData.Data, 0, x.BinaryData.Data.Length);
                            break;
                    }
                    return id == markers[i].Key;
                }))
                {
                    Destroy(marker.gameObject);
                    markers.RemoveAt(i);
                }
            }
        }

        private void SetStatusText()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"<color=#B7B7B8><b>ControllerData</b></color>\nStatus: {ControllerStatus.Text}\n\n");
            builder.Append($"<color=#B7B7B8><b>Controller Input</b></color>\nBumper status: {controllerActions.Bumper.IsPressed()}\n");

            builder.Append($"Marker Tracker running: {MLMarkerTracker.IsStarted} \n\n");
            builder.Append($"Scanning status: {AlwaysOn || controllerActions.Bumper.IsPressed()} \n\n");
            builder.Append($"<color=#B7B7B8><b>Marker Settings</b></color>\nScan Types: {MarkerTypes}\n");
            builder.Append($"Always Scanning: {AlwaysOn}\n");
            builder.Append($"QR Code Size: {QRCodeSize}\n\n");
            builder.Append($"Aruco Size: {ArucoMarkerSize}\n\n");

            foreach (var marker in markers)
            {
                builder.Append(
                        $"<color=#B7B7B8><b>{marker.Key}</b></color>" +
                        $"\nData: {marker.Value.DataString}\n\n");
            }
            statusText.text = builder.ToString();
        }
    }
}

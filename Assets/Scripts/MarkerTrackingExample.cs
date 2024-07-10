// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;

namespace MagicLeap.Examples
{
    public class MarkerTrackingExample : MonoBehaviour
    {
        [SerializeField]
        private MarkerVisualizer markerVisualPrefab;

        [SerializeField]
        private Dropdown profileDropdown;

        [SerializeField]
        private Dropdown markerDetectorTypeDropdown;

        [SerializeField]
        private Text markerTypeText;

        [SerializeField]
        private Dropdown arucoTypeDropdown;

        [SerializeField]
        private Dropdown aprilTagTypeDropdown;

        [SerializeField]
        private Toggle estimateLength;

        [SerializeField]
        private Slider markerLength;

        [SerializeField]
        private Text markerLengethText;

        [SerializeField]
        private Dropdown FPSHintDropdown;

        [SerializeField]
        private Dropdown resolutionHintDropdown;

        [SerializeField]
        private Dropdown cameraHintDropdown;

        [SerializeField]
        private Dropdown cornerRefinementDropdown;

        [SerializeField]
        private Dropdown analysisIntervalDropdown;

        [SerializeField]
        private Toggle useEdgeRefinement;

        [SerializeField]
        private Text statusTextDisplay;

        [SerializeField]
        private Button destroyAllButton;

        private Dictionary<MarkerDetector, HashSet<MarkerVisualizer>> markerVisuals = new();
        private MagicLeapMarkerUnderstandingFeature markerFeature;
        private MarkerDetectorSettings markerDetectorSettings;

        void Start()
        {
            markerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMarkerUnderstandingFeature>();
            markerDetectorTypeDropdown.onValueChanged.AddListener(OnMarkerDetectorDropdownChanged);
            markerVisuals = new Dictionary<MarkerDetector, HashSet<MarkerVisualizer>>();
            destroyAllButton.onClick.AddListener(DestroyMarkerTrackers);
            destroyAllButton.interactable = false;
        }

        void Update()
        {
            var sb = new StringBuilder($"Marker Detectors Created: {markerFeature.MarkerDetectors.Count}");

            destroyAllButton.interactable = markerFeature.MarkerDetectors.Count > 0;

            if (markerFeature.MarkerDetectors.Count == 0)
            {
                statusTextDisplay.text = sb.ToString();
                return;
            }

            sb.AppendLine("\n");

            markerFeature.UpdateMarkerDetectors();

            int trackerIndex = 0;
            foreach (var markerDetector in markerFeature.MarkerDetectors)
            {
                sb.AppendLine($"<b>#{trackerIndex} {markerDetector.Settings.MarkerType}/{markerDetector.Settings.MarkerDetectorProfile}</b>");
                sb.AppendLine($"Detected markers: {markerDetector.Data.Count}");
                sb.AppendLine();

                int expectedVisualCount = markerDetector.Data.Where(d => d.MarkerPose != null).Count();
                if (expectedVisualCount > 0 && !markerVisuals.ContainsKey(markerDetector))
                {
                    markerVisuals.Add(markerDetector, new HashSet<MarkerVisualizer>());
                }

                if (markerVisuals.TryGetValue(markerDetector, out var currentVisualSet))
                {
                    if (currentVisualSet.Count > expectedVisualCount)
                    {
                        foreach (var visual in currentVisualSet)
                            Destroy(visual.gameObject);
                        currentVisualSet.Clear();
                    }
                }

                for (int i = 0; i < markerDetector.Data.Count; i++)
                {
                    if (markerDetector.Data[i].MarkerPose != null)
                    {
                        var markerVisual = Instantiate(markerVisualPrefab);
                        if (currentVisualSet != null)
                        {
                            currentVisualSet.Add(markerVisual);
                        }
                        markerVisual.Set(markerDetector.Data[i], markerDetector.Settings.MarkerType);
                    }
                    sb.AppendLine($"<b>Marker {i}</b>");

                    if (markerDetector.Settings.MarkerType == MarkerType.Aruco || markerDetector.Settings.MarkerType == MarkerType.AprilTag)
                    {
                        sb.AppendLine($"Data: {markerDetector.Data[i].MarkerNumber}");
                    }
                    else
                    {
                        sb.AppendLine($"Data: {markerDetector.Data[i].MarkerString}");
                    }

                    sb.AppendLine($"Length: {markerDetector.Data[i].MarkerLength}");

                    if (markerDetector.Settings.MarkerType == MarkerType.QR)
                    {
                        sb.AppendLine($"Reprojection Error: {markerDetector.Data[i].ReprojectionErrorMeters}");
                    }
                }

                if (trackerIndex < markerFeature.MarkerDetectors.Count - 1)
                    sb.AppendLine("--------\n");

                trackerIndex++;
            }

            statusTextDisplay.text = sb.ToString();
        }

        void OnDestroy()
        {
            destroyAllButton.onClick.RemoveAllListeners();
            markerDetectorTypeDropdown.onValueChanged.RemoveAllListeners();
            DestroyMarkerTrackers();
        }

        public void OnSliderChanged(float value) => markerLengethText.text = $"{(int)value} mm";

        public void OnMarkerDetectorDropdownChanged(int idx)
        {
            switch ((MarkerType)idx)
            {
                case MarkerType.Aruco:
                    markerTypeText.text = "ArUco:";
                    aprilTagTypeDropdown.gameObject.SetActive(false);
                    arucoTypeDropdown.gameObject.SetActive(true);
                    break;
                case MarkerType.AprilTag:
                    markerTypeText.text = "April Tag:";
                    arucoTypeDropdown.gameObject.SetActive(false);
                    aprilTagTypeDropdown.gameObject.SetActive(true);
                    break;
                default:
                    arucoTypeDropdown.gameObject.SetActive(false);
                    aprilTagTypeDropdown.gameObject.SetActive(false);
                    markerTypeText.text = "";
                    break;
            }
        }

        public void OnMarkerProfileChanged(int idx)
        {
            bool active = (MarkerDetectorProfile)idx == MarkerDetectorProfile.Custom;
            FPSHintDropdown.gameObject.SetActive(active);
            resolutionHintDropdown.gameObject.SetActive(active);
            cameraHintDropdown.gameObject.SetActive(active);
            cornerRefinementDropdown.gameObject.SetActive(active);
            analysisIntervalDropdown.gameObject.SetActive(active);
            useEdgeRefinement.gameObject.SetActive(active);
        }

        public void OnCreateMarkerDetector()
        {
            markerDetectorSettings.MarkerDetectorProfile = (MarkerDetectorProfile)profileDropdown.value;
            markerDetectorSettings.MarkerType = (MarkerType)markerDetectorTypeDropdown.value;

            if (markerDetectorSettings.MarkerDetectorProfile == MarkerDetectorProfile.Custom)
            {
                // set custom settings
                markerDetectorSettings.CustomProfileSettings.FPSHint = (MarkerDetectorFPS)FPSHintDropdown.value;
                markerDetectorSettings.CustomProfileSettings.ResolutionHint = (MarkerDetectorResolution)resolutionHintDropdown.value;
                markerDetectorSettings.CustomProfileSettings.CameraHint = (MarkerDetectorCamera)cameraHintDropdown.value;
                markerDetectorSettings.CustomProfileSettings.CornerRefinement = (MarkerDetectorCornerRefineMethod)cornerRefinementDropdown.value;
                markerDetectorSettings.CustomProfileSettings.AnalysisInterval = (MarkerDetectorFullAnalysisInterval)analysisIntervalDropdown.value;
                markerDetectorSettings.CustomProfileSettings.UseEdgeRefinement = useEdgeRefinement.isOn;
            }

            switch ((MarkerType)markerDetectorTypeDropdown.value)
            {
                case MarkerType.Aruco:
                    markerDetectorSettings.ArucoSettings.ArucoType = (ArucoType)arucoTypeDropdown.value;
                    markerDetectorSettings.ArucoSettings.ArucoLength = markerLength.value / 1000f;
                    markerDetectorSettings.ArucoSettings.EstimateArucoLength = estimateLength.isOn;
                    break;
                case MarkerType.AprilTag:
                    markerDetectorSettings.AprilTagSettings.AprilTagType = (AprilTagType)aprilTagTypeDropdown.value;
                    markerDetectorSettings.AprilTagSettings.AprilTagLength = markerLength.value / 1000f;
                    markerDetectorSettings.AprilTagSettings.EstimateAprilTagLength = estimateLength.isOn;
                    break;
                case MarkerType.QR:
                    markerDetectorSettings.QRSettings.QRLength = markerLength.value / 1000f;
                    markerDetectorSettings.QRSettings.EstimateQRLength = estimateLength.isOn;
                    break;
            }

            markerFeature.CreateMarkerDetector(markerDetectorSettings);
        }

        private void DestroyMarkerTrackers()
        {
            foreach (var markerDetector in markerFeature.MarkerDetectors)
            {
                if (markerVisuals.TryGetValue(markerDetector, out var visuals))
                {
                    foreach (var visual in visuals)
                    {
                        Destroy(visual.gameObject);
                    }
                    visuals.Clear();
                }
            }
            markerVisuals.Clear();
            markerFeature.DestroyAllMarkerDetectors();
        }
    }
}

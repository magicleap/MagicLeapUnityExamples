using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;

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

        [SerializeField]
        private float updateIntervalSec = 0.5f;

        private Dictionary<MagicLeapMarkerUnderstandingFeature.MarkerDetector, HashSet<MarkerVisualizer>> markerVisuals = new();
        private MagicLeapMarkerUnderstandingFeature markerFeature;
        private MagicLeapMarkerUnderstandingFeature.MarkerDetectorSettings markerDetectorSettings;

        void Start()
        {
            markerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMarkerUnderstandingFeature>();
            markerDetectorTypeDropdown.onValueChanged.AddListener(OnMarkerDetectorDropdownChanged);
            markerVisuals = new Dictionary<MagicLeapMarkerUnderstandingFeature.MarkerDetector, HashSet<MarkerVisualizer>>();
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

                    if (markerDetector.Settings.MarkerType == MagicLeapMarkerUnderstandingFeature.MarkerType.Aruco || markerDetector.Settings.MarkerType == MagicLeapMarkerUnderstandingFeature.MarkerType.AprilTag)
                    {
                        sb.AppendLine($"Data: {markerDetector.Data[i].MarkerNumber}");
                    }
                    else
                    {
                        sb.AppendLine($"Data: {markerDetector.Data[i].MarkerString}");
                    }

                    sb.AppendLine($"Length: {markerDetector.Data[i].MarkerLength}");

                    if (markerDetector.Settings.MarkerType == MagicLeapMarkerUnderstandingFeature.MarkerType.QR)
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
            switch ((MagicLeapMarkerUnderstandingFeature.MarkerType)idx)
            {
                case MagicLeapMarkerUnderstandingFeature.MarkerType.Aruco:
                    markerTypeText.text = "ArUco:";
                    aprilTagTypeDropdown.gameObject.SetActive(false);
                    arucoTypeDropdown.gameObject.SetActive(true);
                    break;
                case MagicLeapMarkerUnderstandingFeature.MarkerType.AprilTag:
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
            bool active = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorProfile)idx == MagicLeapMarkerUnderstandingFeature.MarkerDetectorProfile.Custom;
            FPSHintDropdown.gameObject.SetActive(active);
            resolutionHintDropdown.gameObject.SetActive(active);
            cameraHintDropdown.gameObject.SetActive(active);
            cornerRefinementDropdown.gameObject.SetActive(active);
            analysisIntervalDropdown.gameObject.SetActive(active);
            useEdgeRefinement.gameObject.SetActive(active);
        }

        public void OnCreateMarkerDetector()
        {
            markerDetectorSettings.MarkerDetectorProfile = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorProfile)profileDropdown.value;
            markerDetectorSettings.MarkerType = (MagicLeapMarkerUnderstandingFeature.MarkerType)markerDetectorTypeDropdown.value;

            if (markerDetectorSettings.MarkerDetectorProfile == MagicLeapMarkerUnderstandingFeature.MarkerDetectorProfile.Custom)
            {
                // set custom settings
                markerDetectorSettings.CustomProfileSettings.FPSHint = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorFPS)FPSHintDropdown.value;
                markerDetectorSettings.CustomProfileSettings.ResolutionHint = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorResolution)resolutionHintDropdown.value;
                markerDetectorSettings.CustomProfileSettings.CameraHint = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorCamera)cameraHintDropdown.value;
                markerDetectorSettings.CustomProfileSettings.CornerRefinement = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorCornerRefineMethod)cornerRefinementDropdown.value;
                markerDetectorSettings.CustomProfileSettings.AnalysisInterval = (MagicLeapMarkerUnderstandingFeature.MarkerDetectorFullAnalysisInterval)analysisIntervalDropdown.value;
                markerDetectorSettings.CustomProfileSettings.UseEdgeRefinement = useEdgeRefinement.isOn;
            }

            switch ((MagicLeapMarkerUnderstandingFeature.MarkerType)markerDetectorTypeDropdown.value)
            {
                case MagicLeapMarkerUnderstandingFeature.MarkerType.Aruco:
                    markerDetectorSettings.ArucoSettings.ArucoType = (MagicLeapMarkerUnderstandingFeature.ArucoType)arucoTypeDropdown.value;
                    markerDetectorSettings.ArucoSettings.ArucoLength = markerLength.value / 1000f;
                    markerDetectorSettings.ArucoSettings.EstimateArucoLength = estimateLength.isOn;
                    break;
                case MagicLeapMarkerUnderstandingFeature.MarkerType.AprilTag:
                    markerDetectorSettings.AprilTagSettings.AprilTagType = (MagicLeapMarkerUnderstandingFeature.AprilTagType)aprilTagTypeDropdown.value;
                    markerDetectorSettings.AprilTagSettings.AprilTagLength = markerLength.value / 1000f;
                    markerDetectorSettings.AprilTagSettings.EstimateAprilTagLength = estimateLength.isOn;
                    break;
                case MagicLeapMarkerUnderstandingFeature.MarkerType.QR:
                    markerDetectorSettings.QRSettings.QRLength = markerLength.value / 1000f;
                    markerDetectorSettings.QRSettings.EstimateQRLength = estimateLength.isOn;
                    break;
            }

            markerFeature.CreateMarkerDetector(markerDetectorSettings);
        }

        private void DestroyMarkerTrackers()
        {
            foreach(var markerDetector in markerFeature.MarkerDetectors)
            {
                if (markerVisuals.TryGetValue(markerDetector, out var visuals))
                {
                    foreach(var visual in visuals)
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

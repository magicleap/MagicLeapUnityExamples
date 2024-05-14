using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;

namespace MagicLeap.Examples
{
    public class MarkerVisualizer : MonoBehaviour
    {
        [SerializeField]
        private TextMesh dataText;

        private StringBuilder stringBuilder = new StringBuilder();

        public void Set(MagicLeapMarkerUnderstandingFeature.MarkerData markerData, MagicLeapMarkerUnderstandingFeature.MarkerType currentMarkerType)
        {
            stringBuilder.Clear();

            stringBuilder.Append($"MarkerLength: {markerData.MarkerLength}\n");

            switch (currentMarkerType)
            {
                case MagicLeapMarkerUnderstandingFeature.MarkerType.QR:
                case MagicLeapMarkerUnderstandingFeature.MarkerType.Code128:
                case MagicLeapMarkerUnderstandingFeature.MarkerType.EAN13:
                case MagicLeapMarkerUnderstandingFeature.MarkerType.UPCA:
                    stringBuilder.Append($"MarkerString: {markerData.MarkerString}\n");
                    break;
                default:
                    stringBuilder.Append($"MarkerNumber: {markerData.MarkerNumber}\n");
                    stringBuilder.Append($"ReprojectionErrorMeters: {markerData.ReprojectionErrorMeters}\n");
                    break;
            }

            switch (currentMarkerType)
            {
                case MagicLeapMarkerUnderstandingFeature.MarkerType.Aruco:
                case MagicLeapMarkerUnderstandingFeature.MarkerType.QR:
                case MagicLeapMarkerUnderstandingFeature.MarkerType.AprilTag:
                    stringBuilder.Append($"Position: {markerData.MarkerPose?.position}\n");
                    stringBuilder.Append($"Rotation: {markerData.MarkerPose?.rotation}");
                    break;
            }

            dataText.text = stringBuilder.ToString();

            transform.position = markerData.MarkerPose.Value.position;
            transform.rotation = markerData.MarkerPose.Value.rotation;
        }
    }
}

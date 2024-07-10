using System.Text;
using UnityEngine;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;

namespace MagicLeap.Examples
{
    public class MarkerVisualizer : MonoBehaviour
    {
        [SerializeField]
        private TextMesh dataText;

        private StringBuilder stringBuilder = new StringBuilder();

        public void Set(MarkerData markerData, MarkerType currentMarkerType)
        {
            stringBuilder.Clear();

            stringBuilder.Append($"MarkerLength: {markerData.MarkerLength}\n");

            switch (currentMarkerType)
            {
                case MarkerType.QR:
                case MarkerType.Code128:
                case MarkerType.EAN13:
                case MarkerType.UPCA:
                    stringBuilder.Append($"MarkerString: {markerData.MarkerString}\n");
                    break;
                default:
                    stringBuilder.Append($"MarkerNumber: {markerData.MarkerNumber}\n");
                    stringBuilder.Append($"ReprojectionErrorMeters: {markerData.ReprojectionErrorMeters}\n");
                    break;
            }

            switch (currentMarkerType)
            {
                case MarkerType.Aruco:
                case MarkerType.QR:
                case MarkerType.AprilTag:
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

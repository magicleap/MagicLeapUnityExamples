using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class MarkerVisual : MonoBehaviour
    {
        [SerializeField] 
        private TextMesh dataText;

        public float Timestamp { get; private set; }
        public string DataString => dataText != null ? dataText.text : string.Empty;
        public MLMarkerTracker.MarkerType Type { get; set; }

        public void Set(MLMarkerTracker.MarkerData data, string dataString = null)
        {
            Timestamp = Time.time;

            Type = data.Type;
            transform.position = data.Pose.position;
            transform.rotation = data.Pose.rotation;
            dataText.text = dataString ?? data.ToString();
            gameObject.SetActive(true);
        }
    }
}

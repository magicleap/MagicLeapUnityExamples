using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class SegmentedDimmerExample : MonoBehaviour
    {
        [SerializeField]
        private Toggle enableToggle;

        // Start is called before the first frame update
        void Start()
        {
            if(enableToggle != null)
            {
                enableToggle.isOn = MLSegmentedDimmer.IsEnabled;
            }

            Debug.Log($"Found Segmented Dimmer: " + MLSegmentedDimmer.Exists);
        }

        public void HandleEnableToggle(bool on)
        {
            MLSegmentedDimmer.IsEnabled = on;
        }
    }
}

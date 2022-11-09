using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class SegmentedDimmerExample : MonoBehaviour
    {
        [SerializeField]
        private Toggle enableToggle;

        [SerializeField, Tooltip("Popup canvas to direct user to Segmented Dimmer settings page.")]
        private GameObject segmentedDimmerSettingsPopup = null;

        [SerializeField, Tooltip("Popup canvas to alert the user of error when Segmented Dimmer settings aren't enabled.")]
        private GameObject segmentedDimmerErrorPopup;

        [SerializeField, Tooltip("Duration in seconds the error dialog will remain on screen if not dismissed.")]
        private float errorDisplayTime = 5f;

        private bool userPromptedForSetting = false;

        // Start is called before the first frame update
        void Start()
        {
            if (!DimmerModeEnabled())
            {
                if (segmentedDimmerSettingsPopup != null)
                {
                    userPromptedForSetting = true;
                    segmentedDimmerSettingsPopup.SetActive(true);

                }
            }

            Debug.Log($"Found Segmented Dimmer: " + MLSegmentedDimmer.Exists);

            if(MLSegmentedDimmer.Exists)
            {
                MLSegmentedDimmer.Activate();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                if (!DimmerModeEnabled())
                {
                    if (!userPromptedForSetting)
                    {
                        if (segmentedDimmerSettingsPopup != null)
                        {
                            userPromptedForSetting = true;
                            segmentedDimmerSettingsPopup.SetActive(true);
                        }
                    }
                    else
                    {
                        if (segmentedDimmerErrorPopup != null)
                        {
                            Debug.LogError("Segmented Dimmer has not been enabled in system display settings. Segmented Dimmer will not be visible in this application until the setting is turned on.");
                            segmentedDimmerErrorPopup.SetActive(true);
                            StartCoroutine(DelayDismissErrorPopup());
                        }
                    }
                }
            }
        }

        public void HandleEnableToggle(bool on)
        {
            MLSegmentedDimmer.SetEnabled(on);
        }

        public bool DimmerModeEnabled()
        {
            // Get context
            using (var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");

                var dimmerMode = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "is_segmented_dimmer_enabled");

                Debug.Log("Dimmer Mode is set to : " + dimmerMode);
                return dimmerMode > 0;
            }
        }

        public void OnSegmentedDimmerSettingsPopupOpen()
        {
            UnityEngine.XR.MagicLeap.SettingsIntentsLauncher.LaunchSystemDisplaySettings();

            if (segmentedDimmerSettingsPopup != null)
            {
                segmentedDimmerSettingsPopup.SetActive(false);
            }
        }

        public void OnSegmentedDimmerSettingsPopupCancel()
        {
            if (segmentedDimmerSettingsPopup != null)
            {
                segmentedDimmerSettingsPopup.SetActive(false);
            }
        }

        private IEnumerator DelayDismissErrorPopup()
        {
            yield return new WaitForSeconds(errorDisplayTime);
            if(segmentedDimmerErrorPopup != null)
            {
                segmentedDimmerErrorPopup.SetActive(false);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class SegmentedDimmerExample : MonoBehaviour
    {
        enum DimmerMode
        {
            DepthBuffer,
            URP,
            Off
        }

        [SerializeField]
        private DimmerMode dimmerMode = DimmerMode.DepthBuffer;

        [SerializeField, Tooltip("Popup canvas to direct user to Segmented Dimmer settings page.")]
        private GameObject segmentedDimmerSettingsPopup = null;

        [SerializeField, Tooltip("Popup canvas to alert the user of error when Segmented Dimmer settings aren't enabled.")]
        private GameObject segmentedDimmerErrorPopup;

        [SerializeField]
        private TextMesh modeMessage;

        [SerializeField]
        private Text status;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        private static bool userPromptedForSetting;

        void Start()
        {
            if (!DimmerModeEnabled())
            {
               ShowDimmerDisabledPopup();
            }

            var data = Camera.main.GetUniversalAdditionalCameraData();
            data.SetRenderer(0);

            MLSegmentedDimmer.Activate();

            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.Bumper.performed += OnBumperDown;

            UpdateMessage();
        }

        private void OnDisable()
        {
            MLSegmentedDimmer.Deactivate();
            controllerActions.Bumper.performed -= OnBumperDown;
        }

        private void OnBumperDown(InputAction.CallbackContext context)
        {
            switch (dimmerMode)
            {
                case DimmerMode.DepthBuffer:
                    dimmerMode = DimmerMode.Off;
                    //dimmerMode = DimmerMode.URP;
                    break;
                case DimmerMode.URP:
                    dimmerMode = DimmerMode.Off;
                    break;
                case DimmerMode.Off:
                    dimmerMode = DimmerMode.DepthBuffer;
                    break;
            }

            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            if (dimmerMode != DimmerMode.Off)
            {
                cameraData.SetRenderer((int)dimmerMode);
                MLSegmentedDimmer.Activate();
            }
            else
            {
                cameraData.SetRenderer(0);
                MLSegmentedDimmer.Deactivate();
            }

            UpdateMessage();
        }

        private void UpdateMessage()
        {
            var val = (dimmerMode == DimmerMode.DepthBuffer) ? "On" : "Off";
            var message = "Segmented Dimmer mode: " + val;
            //var message = "Segmented Dimmer mode: " + dimmerMode.ToString();

            if (modeMessage != null)
            {
                modeMessage.text = message;
            }
            if (status != null)
            {
                status.text = message;
            }
        }

        private void ShowDimmerDisabledPopup()
        {
            segmentedDimmerErrorPopup.SetActive(false);
            segmentedDimmerSettingsPopup.SetActive(false);

            if (userPromptedForSetting)
            {
                Debug.LogError("Segmented Dimmer has not been enabled in system display settings. Segmented Dimmer will not be visible in this application until the setting is turned on.");
                segmentedDimmerErrorPopup.SetActive(true);
            }
            else
            {
                segmentedDimmerSettingsPopup.SetActive(true);
                userPromptedForSetting = true;
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                if (!DimmerModeEnabled())
                {
                    ShowDimmerDisabledPopup();
                }
            }
        }
        
        public bool DimmerModeEnabled()
        {
#if !UNITY_EDITOR
            // Get context
            using (var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");

                var dimmerMode = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "is_segmented_dimmer_enabled");

                Debug.Log("Dimmer Mode is set to : " + dimmerMode);
                return dimmerMode > 0;
            }
#endif
            return true;
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
    }
}

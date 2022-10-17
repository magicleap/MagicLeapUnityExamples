// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class CustomFitExample : MonoBehaviour
    {
        private const string appId = "com.magicleap.customfitunity";

        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text statusText = null;

        [SerializeField, Tooltip("Button that enters the 'Custom Fit' app")]
        private Button appButton;

        private MLHeadsetFit.State headsetFitState;
        private MLEyeCalibration.State eyeCalibrationState;
        private AndroidJavaClass fileProviderClass = new("android.support.v4.content.FileProvider");

        private void Awake()
        {
            appButton.onClick.AddListener(OpenApp);
        }

        // Update is called once per frame
        void Update()
        {
            UpdateStatusText();
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            statusText.text = $"<color=#dbfb76><b>Controller Data</b></color>";
            statusText.text += $"\nStatus: {ControllerStatus.Text}\n";
            var result = MLHeadsetFit.GetState(out headsetFitState);
            if (result.IsOk)
            {
                statusText.text += $"\nHead calibration: {headsetFitState.FitStatus}";
            }
            result = MLEyeCalibration.GetState(out eyeCalibrationState);
            if (result.IsOk)
            {
                statusText.text += $"\nHead calibration: {eyeCalibrationState.EyeCalibration}";
            }
        }

        private void OpenApp()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", appId);
            launchIntent.Call<AndroidJavaObject>("setAction", "android.intent.action.MAIN");
            currentActivity.Call("startActivity", launchIntent);
            unityPlayer.Dispose();
            currentActivity.Dispose();
            packageManager.Dispose();
            launchIntent.Dispose();
#else
            Debug.LogError("You need to be on Magic Leap device to run this app");
#endif
        }
    }
}

// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using MagicLeap.Examples;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.UserCalibration;

public class UserCalibrationExample : MonoBehaviour
{
    [SerializeField] private Text statusText;

    private MagicLeapUserCalibrationFeature userCalibrationFeature;
    private readonly StringBuilder userCalibrationText = new();
    private readonly Color fieldTitleColor = Color.red;
    private readonly Color fieldValueColor = Color.white;
    private bool userCalibrationEnabled;

    private MagicLeapController Controller => MagicLeapController.Instance;
    
    void Start()
    {
        userCalibrationFeature = OpenXRSettings.Instance.GetFeature<MagicLeapUserCalibrationFeature>();
        if (!userCalibrationFeature.enabled)
        { 
            statusText.text = "Feature is not enabled. Disabling example";
            enabled = false;
            return;
        }

        Controller.BumperPressed += BumperHandler;
    }

    private void BumperHandler(InputAction.CallbackContext _)
    {
        userCalibrationEnabled = !userCalibrationEnabled;
        userCalibrationFeature.EnableUserCalibrationEvents(userCalibrationEnabled);
    }

    void Update()
    {
        string GetTextWithColor(object text, Color color)
        {
            return $"<b><color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color></b>";
        }
        
        if (!userCalibrationEnabled)
        {
            statusText.text = GetTextWithColor("User calibration events disabled", fieldTitleColor);
        }
        else
        {
            userCalibrationText.Clear();
            userCalibrationFeature.GetLastHeadsetFit(out var headsetFitData);
            userCalibrationFeature.GetLastEyeCalibration(out var eyeCalibrationData);
            
            userCalibrationText.AppendLine($"{GetTextWithColor("Headset fit status", fieldTitleColor)} : {GetTextWithColor(headsetFitData.Status, fieldValueColor)}");
            userCalibrationText.AppendLine($"{GetTextWithColor("Headset fit time", fieldTitleColor)} :  {GetTextWithColor(DateTimeOffset.FromUnixTimeMilliseconds((long)(headsetFitData.Time * 1e-6)).DateTime.ToString("d/M/y HH:mm"), fieldValueColor)}");
            userCalibrationText.AppendLine($"{GetTextWithColor("Eye Calibration Status", fieldTitleColor)} : {GetTextWithColor(eyeCalibrationData.Status, fieldValueColor)}");

            statusText.text = userCalibrationText.ToString();
        }
    }

    private void OnDestroy()
    {
        if (userCalibrationFeature.enabled)
        {
            userCalibrationFeature.EnableUserCalibrationEvents(false);
        }

        Controller.BumperPressed -= BumperHandler;
    }
}

using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;

public class UserCalibrationExample : MonoBehaviour
{
    [SerializeField] private Text statusText;

    private MagicLeapUserCalibrationFeature userCalibrationFeature;
    private readonly StringBuilder userCalibrationText = new();
    private readonly Color fieldTitleColor = Color.red;
    private readonly Color fieldValueColor = Color.white;
    private MagicLeapInput mlInputs;
    private bool userCalibrationEnabled;
    
    void Start()
    {
        userCalibrationFeature = OpenXRSettings.Instance.GetFeature<MagicLeapUserCalibrationFeature>();
        if (!userCalibrationFeature.enabled)
        { 
            statusText.text = "Feature is not enabled. Disabling example";
            enabled = false;
            return;
        }

        mlInputs = new();
        mlInputs.Enable();
        mlInputs.Controller.Bumper.performed += BumperHandler;
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

        if (mlInputs == null)
        {
            return;
        }
        mlInputs.Controller.Bumper.performed -= BumperHandler;
        mlInputs.Dispose();
    }
}

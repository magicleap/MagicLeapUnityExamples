// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.PixelSensors;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;

public class PixelSensorExample : MonoBehaviour
{
    [Header("UI")] [SerializeField] private Dropdown sensorTypesDropdown;
    [SerializeField] private Text outputText;

    [SerializeField] private PixelSensorVisualizers visualizersPrefab;
    private readonly List<SensorInfo> activeSensors = new();

    private readonly HashSet<string> grantedPermissions = new();
    private readonly Dictionary<PixelSensorType, List<PixelSensorId>> sensorIdTable = new();

    private readonly StringBuilder stringBuilder = new();
    private List<PixelSensorId> availableSensors = new();
    private List<PixelSensorType> availableSensorTypes = new();
    private uint currentView;

    private bool isInitialized;

    private MagicLeapPixelSensorFeature pixelSensorFeature;
    private bool shouldStartShowing;
    private PixelSensorVisualizers visualizers;

    private void Awake()
    {
        pixelSensorFeature = OpenXRSettings.Instance.GetFeature<MagicLeapPixelSensorFeature>();
        if (pixelSensorFeature == null || !pixelSensorFeature.enabled)
        {
            enabled = false;
            Debug.LogError("PixelSensorFeature is either not enabled or is null. Check Project Settings in the Unity editor to make sure the feature is enabled");
        }

        outputText.text = "";
        grantedPermissions.Clear();
        Permissions.RequestPermission(Permission.Camera, OnPermissionGranted, OnPermissionDenied, OnPermissionDenied);
        visualizers = Instantiate(visualizersPrefab);
        visualizers.Reset();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        stringBuilder.Clear();
        if (activeSensors.Count == 0)
        {
            stringBuilder.AppendLine("No sensor selected");
        }

        foreach (var sensor in activeSensors)
        {
            if (!sensor.ShouldFetchData)
            {
                stringBuilder.AppendLine($"Waiting for {sensor.SensorId.SensorName}");
                continue;
            }

            sensor.UpdateVisualizer(stringBuilder);
        }

        outputText.text = stringBuilder.ToString();
    }

    private void Initialize()
    {
        void AddToTable(PixelSensorType key, PixelSensorId sensorId)
        {
            if (sensorIdTable.TryGetValue(key, out var sensorList))
            {
                sensorList.Add(sensorId);
            }
            else
            {
                sensorIdTable.Add(key, new List<PixelSensorId>
                {
                    sensorId
                });
            }
        }

        availableSensors = pixelSensorFeature.GetSupportedSensors();


        foreach (var sensor in availableSensors)
        {
            if (sensor.SensorName.Contains("World"))
            {
                AddToTable(PixelSensorType.World, sensor);
            }

            if (sensor.SensorName.Contains("Eye"))
            {
                AddToTable(PixelSensorType.Eye, sensor);
            }

            if (sensor.SensorName.Contains("Picture"))
            {
                AddToTable(PixelSensorType.Picture, sensor);
            }

            if (sensor.SensorName.Contains("Depth"))
            {
                AddToTable(PixelSensorType.Depth, sensor);
            }
        }

        availableSensorTypes = sensorIdTable.Keys.ToList();

        sensorTypesDropdown.ClearOptions();
        sensorTypesDropdown.options.Add(new Dropdown.OptionData("None"));
        sensorTypesDropdown.options.AddRange(availableSensorTypes.Select(sensorType => new Dropdown.OptionData(sensorType.ToString())));

        sensorTypesDropdown.SetValueWithoutNotify(0);
        sensorTypesDropdown.onValueChanged.RemoveAllListeners();
        sensorTypesDropdown.onValueChanged.AddListener(OnSensorDropdownChangedHandler);

        isInitialized = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"{permission} denied, example won't function.");
    }

    private void OnPermissionGranted(string permission)
    {
        grantedPermissions.Add(permission);
    }

    private void StopActiveSensors()
    {
        foreach (var sensor in activeSensors)
        {
            sensor.Dispose();
        }
        activeSensors.Clear();
    }

    private void OnDestroy()
    {
        StopActiveSensors();
    }

    private void OnSensorDropdownChangedHandler(int index)
    {
        StopActiveSensors();
        visualizers.Reset();
        // if None was chosen
        if (index == 0)
        {
            outputText.text = "";
            return;
        }

        var sensorIndex = index - 1;
        var newSensorType = availableSensorTypes[sensorIndex];
        //If sensors were created :
        StartCoroutine(CreateSensorAfterPermission(newSensorType));
    }

    private string NeededPermission(PixelSensorType sensorType)
    {
        var result = sensorType switch
        {
            PixelSensorType.Depth => Permissions.DepthCamera,
            PixelSensorType.World or PixelSensorType.Picture => Permission.Camera,
            PixelSensorType.Eye => Permissions.EyeCamera,
            _ => ""
        };
        return result;
    }

    private IEnumerator CreateSensorAfterPermission(PixelSensorType newSensorType)
    {
        var neededPermission = NeededPermission(newSensorType);
        if (!grantedPermissions.Contains(neededPermission))
        {
            Permissions.RequestPermission(neededPermission, OnPermissionGranted, OnPermissionDenied);
        }
        yield return new WaitUntil(() => grantedPermissions.Contains(neededPermission));
        sensorTypesDropdown.interactable = false;
        //Permission granted, so now we can create the associated sensors:
        if (!sensorIdTable.TryGetValue(newSensorType, out var sensors))
        {
            sensorTypesDropdown.interactable = true;
            Debug.LogError($"The given {newSensorType} does not have any associated sensors");
            yield break;
        }

        visualizers.Reset();
        visualizers.SetCount(sensors.Count);
        Assert.AreEqual(visualizers.ActiveVisualizers.Count, sensors.Count);
        for (var i = 0; i < sensors.Count; i++)
        {
            var visualizer = visualizers.ActiveVisualizers[i];
            var sensorInfo = new SensorInfo(pixelSensorFeature, visualizer, sensors[i]);
            activeSensors.Add(sensorInfo);
        }

        //Create each sensor
        foreach (var sensor in activeSensors)
        {
            if (!sensor.CreateSensor())
            {
                Debug.LogError($"Unable to create sensor type: {sensor.SensorId.SensorName}");
                continue;
            }

            sensor.Created = true;
            sensor.Initialize();
        }

        //Configure sensor
        foreach (var sensor in activeSensors)
        {
            if (!sensor.Created)
            {
                continue;
            }
            var configureResult = sensor.ConfigureSensorRoutine();
            yield return configureResult;
            if (!configureResult.DidOperationSucceed)
            {
                Debug.LogError($"Unable to configure {sensor.SensorId.SensorName}");
                continue;
            }

            sensor.Configured = true;
        }
        
        //Start sensor
        foreach (var sensor in activeSensors)
        {
            if (!sensor.Configured)
            {
                continue;
            }

            var startSensorResult = sensor.StartSensorRoutine();
            yield return startSensorResult;
            if (!startSensorResult.DidOperationSucceed)
            {
                continue;
            }

            sensor.Started = true;
        }
        //Wait for a brief moment to ensure the sensor is ready
        yield return new WaitForSeconds(2);
        visualizers.EnableVisualizer(true);
        foreach (var sensor in activeSensors)
        {
            if (sensor.Started)
            {
                sensor.ShouldFetchData = true;
            }
        }

        sensorTypesDropdown.interactable = true;
    }

    private class SensorInfo : IDisposable
    {
        public SensorInfo(MagicLeapPixelSensorFeature feature, PixelSensorVisualizer visualizer, PixelSensorId sensorId)
        {
            PixelSensorFeature = feature;
            Visualizer = visualizer;
            SensorId = sensorId;
            ConfiguredStream = 0;
        }

        public PixelSensorId SensorId { get; }

        private uint ConfiguredStream { get; }

        private MagicLeapPixelSensorFeature PixelSensorFeature { get; }

        private PixelSensorVisualizer Visualizer { get; }

        public bool Configured { get; set; }

        public bool Started { get; set; }

        public bool Created { get; set; }

        public bool ShouldFetchData { get; set; }

        public void Dispose()
        {
            Visualizer.Reset();
            if (!Created)
            {
                return;
            }

            PixelSensorFeature.DestroyPixelSensor(SensorId);
        }

        public void UpdateVisualizer(StringBuilder frameDataText)
        {
            if (!PixelSensorFeature.GetSensorData(SensorId, ConfiguredStream, out var frame, out _, Allocator.Temp))
            {
                return;
            }

            frameDataText.AppendLine("");
            frameDataText.AppendLine($"Sensor: {SensorId.SensorName}");
            frameDataText.AppendLine($"Frame Type: {frame.FrameType}");
            frameDataText.AppendLine($"Frame Valid: {frame.IsValid}");
            frameDataText.AppendLine($"Capture Time: {frame.CaptureTime}");
            frameDataText.AppendLine($"Frame Plane Count: {frame.Planes.Length}");
            Visualizer.ProcessFrame(in frame);
        }

        public bool CreateSensor()
        {
            return PixelSensorFeature.CreatePixelSensor(SensorId);
        }

        public PixelSensorAsyncOperationResult ConfigureSensorRoutine()
        {
            return PixelSensorFeature.ConfigureSensorWithDefaultCapabilities(SensorId, ConfiguredStream);
        }

        public PixelSensorAsyncOperationResult StartSensorRoutine()
        {
            return PixelSensorFeature.StartSensor(SensorId, new[]
            {
                ConfiguredStream
            });
        }

        public void Initialize()
        {
            Visualizer.Initialize(0, PixelSensorFeature, SensorId);
        }
    }

    private enum PixelSensorType
    {
        Depth,
        World,
        Eye,
        Picture
    }
}

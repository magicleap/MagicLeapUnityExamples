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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.NativeTypes;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.SpatialAnchors;
using MagicLeap.OpenXR.Features.LocalizationMaps;
using MagicLeap.OpenXR.Subsystems;
using MagicLeap.Examples;

public class SpatialAnchorsExample : MonoBehaviour
{
    [SerializeField] private Text statusText;
    [SerializeField] private Text localizationText;
    [SerializeField] private Dropdown mapsDropdown;
    [SerializeField] private Dropdown exportedDropdown;
    [SerializeField] private GameObject anchorPrefab;
    [SerializeField] private GameObject controllerObject;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private Button publishButton;
    private MagicLeapSpatialAnchorsFeature spatialAnchorsFeature;
    private MagicLeapLocalizationMapFeature localizationMapFeature;
    private MagicLeapSpatialAnchorsStorageFeature storageFeature;
    private LocalizationMap[] mapList = Array.Empty<LocalizationMap>();
    private LocalizationEventData mapData;

    private List<ARAnchor> localAnchors = new();
    private List<ARAnchor> storedAnchors = new();
    private Dictionary<string, byte[]> exportedMaps = new();
    private bool permissionGranted;
    private MLXrAnchorSubsystem activeSubsystem;

    private IEnumerator Start()
    {
        yield return new WaitUntil(AreSubsystemsLoaded);

        spatialAnchorsFeature = OpenXRSettings.Instance.GetFeature<MagicLeapSpatialAnchorsFeature>();
        storageFeature = OpenXRSettings.Instance.GetFeature<MagicLeapSpatialAnchorsStorageFeature>();
        localizationMapFeature = OpenXRSettings.Instance.GetFeature<MagicLeapLocalizationMapFeature>();
        if (!spatialAnchorsFeature || !localizationMapFeature || !storageFeature)
        {
            statusText.text = "Spatial Anchors, Spatial Anchors Storage, or Localization maps features not enabled; disabling";
            enabled = false;
        }

        MagicLeapController.Instance.BumperPressed += OnBumper;
        MagicLeapController.Instance.MenuPressed += OnMenu;

        mapsDropdown.ClearOptions();
        exportedDropdown.ClearOptions();
        storageFeature.OnQueryComplete += OnQueryComplete;

        anchorManager.anchorsChanged += OnAnchorsChanged;

        Permissions.RequestPermission(Permissions.SpaceImportExport, OnPermissionGranted, OnPermissionDenied);
    }

    private bool AreSubsystemsLoaded()
    {
        if (XRGeneralSettings.Instance == null) return false;
        if (XRGeneralSettings.Instance.Manager == null) return false;
        var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
        if (activeLoader == null) return false;
        activeSubsystem = activeLoader.GetLoadedSubsystem<XRAnchorSubsystem>() as MLXrAnchorSubsystem;
        return activeSubsystem != null;
    }

    private void OnPermissionDenied(string permission)
    {
        permissionGranted = false;
        Debug.LogError("Spatial anchor publishing and map localization will not work without permission.");
    }

    private void OnPermissionGranted(string permission)
    {
        permissionGranted = true;
        if (localizationMapFeature.GetLocalizationMapsList(out mapList) == XrResult.Success)
        {
            mapsDropdown.AddOptions(mapList.Select(map => map.Name).ToList());
            mapsDropdown.Hide();
        }

        XrResult res = localizationMapFeature.EnableLocalizationEvents(true);
        if (res != XrResult.Success)
            Debug.LogError("EnableLocalizationEvents failed: " + res);
    }

    private void OnBumper(InputAction.CallbackContext _)
    {
        Pose currentPose = new Pose(controllerObject.transform.position, controllerObject.transform.rotation);

        GameObject newAnchor = Instantiate(anchorPrefab, currentPose.position, currentPose.rotation);

        ARAnchor newAnchorComponent = newAnchor.AddComponent<ARAnchor>();

        newAnchorComponent.GetComponent<MeshRenderer>().material.color = Color.grey;

        localAnchors.Add(newAnchorComponent);
    }

    private void OnMenu(InputAction.CallbackContext _)
    {
        // Delete most recent local anchor first
        if (localAnchors.Count > 0)
        {
            Destroy(localAnchors[^1].gameObject);
            localAnchors.RemoveAt(localAnchors.Count - 1);
        }
        // Deleting the last published anchor.
        else if (storedAnchors.Count > 0)
        {
            storageFeature.DeleteStoredSpatialAnchors(new List<ARAnchor> { storedAnchors[^1] });
        }
    }

    private void OnQueryComplete(List<string> anchorMapPositionIds)
    {
        List<string> trackedanchorMapPositionIds = new List<string>();

        // Check for expired anchors
        foreach (ARAnchor storedAnchor in storedAnchors)
        {
            string anchorMapPositionId = activeSubsystem.GetAnchorMapPositionId(storedAnchor);

            if (!string.IsNullOrEmpty(anchorMapPositionId))
            {
                // Store the anchorMapPositionId to check for new anchors below.
                trackedanchorMapPositionIds.Add(anchorMapPositionId);

                if (!anchorMapPositionIds.Contains(anchorMapPositionId))
                {
                    Destroy(storedAnchor.gameObject);
                }
            }
        }

        // Check for new stored anchors
        IEnumerable<string> newAnchors = anchorMapPositionIds.Except(trackedanchorMapPositionIds);

        if (newAnchors.Count() > 0)
        {
            if (!storageFeature.CreateSpatialAnchorsFromStorage(newAnchors.ToList()))
            {
                Debug.LogError("SpatialAnchorsExample failed to create new anchors from query.");
            }
        }
    }

    public void PublishAnchors()
    {
        List<ARAnchor> pendingPublish = new List<ARAnchor>();

        foreach (ARAnchor anchor in localAnchors)
        {
            if (anchor.trackingState == TrackingState.Tracking)
            {
                pendingPublish.Add(anchor);
            }
        }            

        storageFeature.PublishSpatialAnchorsToStorage(pendingPublish, 0);
    }

    public void LocalizeMap()
    {
        if (permissionGranted == false || localizationMapFeature == null)
            return;

        string map = mapList.Length > 0 ? mapList[mapsDropdown.value].MapUUID : "";
        var res = localizationMapFeature.RequestMapLocalization(map);
        if (res != XrResult.Success)
        {
            Debug.LogError("Failed to request localization: " + res);
            return;
        }

        //On map change, we need to clear up present published anchors and query new ones
        foreach (ARAnchor obj in storedAnchors)
            Destroy(obj.gameObject);
        storedAnchors.Clear();

        foreach (ARAnchor anchor in localAnchors)
            Destroy(anchor.gameObject);
        localAnchors.Clear();
    }

    public void ExportMap()
    {
        if (permissionGranted == false || localizationMapFeature == null || mapList.Length == 0)
            return;
        string uuid = mapList[mapsDropdown.value].MapUUID;
        var res = localizationMapFeature.ExportLocalizationMap(uuid, out byte[] mapData);
        if (res != XrResult.Success)
        {
            Debug.LogError("Failed to export map: " + res);
            return;
        }
        exportedMaps.Add(mapList[mapsDropdown.value].Name, mapData);
        exportedDropdown.ClearOptions();
        exportedDropdown.AddOptions(exportedMaps.Keys.ToList());
        exportedDropdown.Hide();
    }

    public void ImportMap()
    {
        if (permissionGranted == false || localizationMapFeature == null || exportedMaps.Count == 0)
            return;

        var idx = exportedDropdown.value;
        var mapName = exportedDropdown.options[idx].text;
        if (exportedMaps.TryGetValue(mapName, out byte[] mapData))
        {
            var res = localizationMapFeature.ImportLocalizationMap(mapData, out _);
            if (res == XrResult.Success)
            {
                exportedMaps.Remove(mapName);
                exportedDropdown.ClearOptions();
                exportedDropdown.AddOptions(exportedMaps.Keys.ToList());
                exportedDropdown.Hide();
            }
        }
    }

    public void QueryAnchors()
    {
        if (!storageFeature.QueryStoredSpatialAnchors(controllerObject.transform.position, 10f))
        {
            Debug.LogError("Could not query stored anchors");
        }
    }

    void Update()
    {
        if (permissionGranted)
        {
            if (localizationMapFeature != null)
            {
                localizationMapFeature.GetLatestLocalizationMapData(out LocalizationEventData mapData);
                string localizationInfo = string.Format("Localization info: State:{0} Confidence:{1}", mapData.State, mapData.Confidence);
                if (mapData.State == LocalizationMapState.Localized)
                {
                    localizationInfo += string.Format("Name:{0} UUID:{1} Type:{2} Errors:{3}",
                        mapData.Map.Name, mapData.Map.MapUUID, mapData.Map.MapType, (mapData.Errors.Length > 0) ? string.Join(",", mapData.Errors) : "None");
                }
                localizationText.text = localizationInfo;
                publishButton.interactable = mapData.State == LocalizationMapState.Localized;
            }
            else
            {
                publishButton.interactable = false;
            }

            UpdateStoredAnchorTransforms();
        }
    }

    private void UpdateStoredAnchorTransforms()
    {
        if (activeSubsystem == null)
        {
            return;
        }
        foreach (var anchor in storedAnchors)
        {
            var anchorObject = anchor.gameObject;
            var pose = activeSubsystem.GetAnchorPose(anchor);
            anchorObject.transform.position = pose.position;
            anchorObject.transform.rotation = pose.rotation;
        }
    }

    private void OnAnchorsChanged(ARAnchorsChangedEventArgs anchorsChanged)
    {
        // Check for newly added Stored Anchors this Script may not yet know about.
        foreach (ARAnchor anchor in anchorsChanged.added)
        {
            if (activeSubsystem.IsStoredAnchor(anchor))
            {
                storedAnchors.Add(anchor);
            }
        }

        // Check for Local Anchors that were published to update the visuals.
        foreach (ARAnchor anchor in anchorsChanged.updated)
        {
            if (activeSubsystem.IsStoredAnchor(anchor) && localAnchors.Contains(anchor))
            {
                anchor.GetComponent<MeshRenderer>().material.color = Color.white;
                storedAnchors.Add(anchor);
                localAnchors.Remove(anchor);
            }
        }

        // Check if we are tracking a deleted anchor.
        foreach (ARAnchor anchor in anchorsChanged.removed)
        {
            if (storedAnchors.Contains(anchor))
            {
                storedAnchors.Remove(anchor);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (localizationMapFeature != null)
        {
            localizationMapFeature.EnableLocalizationEvents(false);
        }
    }

    private void OnDestroy()
    {
        MagicLeapController.Instance.BumperPressed -= OnBumper;
        MagicLeapController.Instance.MenuPressed -= OnMenu;
        storageFeature.OnQueryComplete -= OnQueryComplete;

        anchorManager.anchorsChanged -= OnAnchorsChanged;
    }
}

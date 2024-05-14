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
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using UnityEngine.XR.OpenXR.NativeTypes;
using MagicLeap.Android;

public class SpatialAnchorsExample : MonoBehaviour
{
    [SerializeField] private Text statusText;
    [SerializeField] private Text localizationText;
    [SerializeField] private Dropdown mapsDropdown;
    [SerializeField] private Dropdown exportedDropdown;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject anchorPrefab;
    [SerializeField] private GameObject controllerObject;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private Button publishButton;
    private InputActionMap controllerMap;
    private MagicLeapSpatialAnchorsFeature spatialAnchorsFeature;
    private MagicLeapLocalizationMapFeature localizationMapFeature;
    private MagicLeapSpatialAnchorsStorageFeature storageFeature;
    private MagicLeapLocalizationMapFeature.LocalizationMap[] mapList = Array.Empty<MagicLeapLocalizationMapFeature.LocalizationMap>();
    private MagicLeapLocalizationMapFeature.LocalizationEventData mapData;

    private struct PublishedAnchor
    {
        public ulong AnchorId;
        public string AnchorMapPositionId;
        public ARAnchor AnchorObject;
    }

    private List<PublishedAnchor> publishedAnchors = new();
    private List<ARAnchor> activeAnchors = new();
    private List<ARAnchor> pendingPublishedAnchors = new();
    private List<ARAnchor> localAnchors = new();
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

        if (inputActions != null)
        {
            controllerMap = inputActions.FindActionMap("Controller");
            if (controllerMap == null)
            {
                Debug.LogError("Couldn't find Controller action map");
                enabled = false;
            }
            else
            {
                controllerMap.FindAction("Bumper").performed += OnBumper;
                controllerMap.FindAction("MenuButton").performed += OnMenu;
            }
        }

        mapsDropdown.ClearOptions();
        exportedDropdown.ClearOptions();
        storageFeature.OnCreationCompleteFromStorage += OnCreateFromStorageComplete;
        storageFeature.OnPublishComplete += OnPublishComplete;
        storageFeature.OnQueryComplete += OnQueryComplete;
        storageFeature.OnDeletedComplete += OnDeletedComplete;

        Permissions.RequestPermission(MLPermission.SpaceImportExport, OnPermissionGranted, OnPermissionDenied);
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

    private void OnPublishComplete(ulong anchorId, string anchorMapPositionId)
    {
        for (int i = activeAnchors.Count - 1; i >= 0; i--)
        {
            if (activeSubsystem.GetAnchorId(activeAnchors[i]) == anchorId)
            {
                PublishedAnchor newPublishedAnchor;
                newPublishedAnchor.AnchorId = anchorId;
                newPublishedAnchor.AnchorMapPositionId = anchorMapPositionId;
                newPublishedAnchor.AnchorObject = activeAnchors[i];

                activeAnchors[i].GetComponent<MeshRenderer>().material.color = Color.white;

                publishedAnchors.Add(newPublishedAnchor);
                activeAnchors.RemoveAt(i);
                break;
            }
        }
    }

    private void OnBumper(InputAction.CallbackContext _)
    {
        Pose currentPose = new Pose(controllerObject.transform.position, controllerObject.transform.rotation);

        GameObject newAnchor = Instantiate(anchorPrefab, currentPose.position, currentPose.rotation);

        ARAnchor newAnchorComponent = newAnchor.AddComponent<ARAnchor>();

        newAnchorComponent.GetComponent<MeshRenderer>().material.color = Color.grey;

        activeAnchors.Add(newAnchorComponent);
        localAnchors.Add(newAnchorComponent);
    }

    private void OnMenu(InputAction.CallbackContext _)
    {
        // delete most recent local anchor first
        if (localAnchors.Count > 0)
        {
            Destroy(localAnchors[^1].gameObject);
            localAnchors.RemoveAt(localAnchors.Count - 1);
        }
        //Deleting the last published anchor.
        else if (publishedAnchors.Count > 0)
        {
            storageFeature.DeleteStoredSpatialAnchor(new List<string> { publishedAnchors[^1].AnchorMapPositionId });
        }
    }

    private void OnQueryComplete(List<string> anchorMapPositionIds)
    {
        if (publishedAnchors.Count == 0)
        {
            if (!storageFeature.CreateSpatialAnchorsFromStorage(anchorMapPositionIds))
                Debug.LogError("Couldn't create spatial anchors from storage");
            return;
        }

        foreach (string anchorMapPositionId in anchorMapPositionIds)
        {
            var matches = publishedAnchors.Where(p => p.AnchorMapPositionId == anchorMapPositionId);
            if (matches.Count() == 0)
            {
                if (!storageFeature.CreateSpatialAnchorsFromStorage(new List<string>() { anchorMapPositionId }))
                    Debug.LogError("Couldn't create spatial anchors from storage");
            }
        }

        for (int i = publishedAnchors.Count - 1; i >= 0; i--)
        {
            if (!anchorMapPositionIds.Contains(publishedAnchors[i].AnchorMapPositionId))
            {
                GameObject.Destroy(publishedAnchors[i].AnchorObject.gameObject);
                publishedAnchors.RemoveAt(i);
            }
        }

    }

    private void OnDeletedComplete(List<string> anchorMapPositionIds)
    {
        foreach (string anchorMapPositionId in anchorMapPositionIds)
        {
            for (int i = publishedAnchors.Count - 1; i >= 0; i--)
            {
                if (publishedAnchors[i].AnchorMapPositionId == anchorMapPositionId)
                {
                    GameObject.Destroy(publishedAnchors[i].AnchorObject.gameObject);
                    publishedAnchors.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void OnCreateFromStorageComplete(Pose pose, ulong anchorId, string anchorMapPositionId, XrResult result)
    {
        if (result != XrResult.Success)
        {
            Debug.LogError("Could not create anchor from storage: " + result);
            return;
        }

        PublishedAnchor newPublishedAnchor;
        newPublishedAnchor.AnchorId = anchorId;
        newPublishedAnchor.AnchorMapPositionId = anchorMapPositionId;

        GameObject newAnchor = Instantiate(anchorPrefab, pose.position, pose.rotation);

        ARAnchor newAnchorComponent = newAnchor.AddComponent<ARAnchor>();

        newPublishedAnchor.AnchorObject = newAnchorComponent;

        publishedAnchors.Add(newPublishedAnchor);
    }

    public void PublishAnchors()
    {
        foreach (ARAnchor anchor in localAnchors)
            pendingPublishedAnchors.Add(anchor);

        localAnchors.Clear();
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
        foreach (PublishedAnchor obj in publishedAnchors)
            Destroy(obj.AnchorObject.gameObject);
        publishedAnchors.Clear();

        foreach (ARAnchor anchor in localAnchors)
            Destroy(anchor.gameObject);
        localAnchors.Clear();

        activeAnchors.Clear();
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
            if (pendingPublishedAnchors.Count > 0)
            {
                for (int i = pendingPublishedAnchors.Count - 1; i >= 0; i--)
                {
                    if (pendingPublishedAnchors[i].trackingState == TrackingState.Tracking)
                    {
                        ulong anchorId = activeSubsystem.GetAnchorId(pendingPublishedAnchors[i]);
                        if (!storageFeature.PublishSpatialAnchorsToStorage(new List<ulong>() { anchorId }, 0))
                        {
                            Debug.LogError($"Failed to publish anchor {anchorId} at position {pendingPublishedAnchors[i].gameObject.transform.position} to storage");
                        }
                        else
                        {
                            pendingPublishedAnchors.RemoveAt(i);
                        }
                    }
                }
            }

            if (localizationMapFeature != null)
            {
                localizationMapFeature.GetLatestLocalizationMapData(out MagicLeapLocalizationMapFeature.LocalizationEventData mapData);
                string localizationInfo = string.Format("Localization info:\nState:{0}\nConfidence:{1}", mapData.State, mapData.Confidence);
                if (mapData.State == MagicLeapLocalizationMapFeature.LocalizationMapState.Localized)
                {
                    localizationInfo += string.Format("\nName:{0}\nUUID:{1}\nType:{2}\nErrors:{3}",
                        mapData.Map.Name, mapData.Map.MapUUID, mapData.Map.MapType, (mapData.Errors.Length > 0) ? string.Join(",", mapData.Errors) : "None");
                }
                localizationText.text = localizationInfo;
                publishButton.interactable = mapData.State == MagicLeapLocalizationMapFeature.LocalizationMapState.Localized;
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
        foreach (var anchor in publishedAnchors)
        {
            var anchorObject = anchor.AnchorObject.gameObject;
            var pose = activeSubsystem.GetAnchorPoseFromID(anchor.AnchorId);
            anchorObject.transform.position = pose.position;
            anchorObject.transform.rotation = pose.rotation;
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
        if (inputActions != null && controllerMap != null)
        {
            controllerMap.FindAction("Bumper").performed -= OnBumper;
            controllerMap.FindAction("MenuButton").performed -= OnMenu;
        }

        storageFeature.OnCreationCompleteFromStorage -= OnCreateFromStorageComplete;
        storageFeature.OnPublishComplete -= OnPublishComplete;
        storageFeature.OnQueryComplete -= OnQueryComplete;
        storageFeature.OnDeletedComplete -= OnDeletedComplete;
    }
}

// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using MagicLeap.Android;
using MagicLeap.Examples;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.Meshing;
using Random = UnityEngine.Random;
using Utils = MagicLeap.Examples.Utils;

public class MeshingExample : MonoBehaviour
{
    [Serializable]
    private class MeshQuerySetting
    {
        [SerializeField] public MeshingQuerySettings meshQuerySettings;
        [SerializeField] public float meshDensity;
        [SerializeField] public Vector3 meshBoundsOrigin;
        [SerializeField] public Vector3 meshBoundsRotation;
        [SerializeField] public Vector3 meshBoundsScale;
        [SerializeField] public MeshingMode renderMode;
        [SerializeField] public MeshFilter meshPrefab;
    }
    
    [SerializeField] private ARMeshManager meshManager;
    [SerializeField] private ARPointCloudManager pointCloudManager;
    [SerializeField] private MeshingProjectile projectilePrefab;
    private Camera mainCamera;

    private const float ProjectileLifetime = 5f;
    private const float ProjectileForce = 300f;
    private const float MinScale = 0.1f;
    private const float MaxScale = 0.3f;
    private MagicLeapMeshingFeature meshingFeature;
    private int currentIndex;

    [SerializeField] private MeshQuerySetting[] allSettings;

    [SerializeField] private Text updateText;

    private StringBuilder statusText = new();
    private MeshDetectorFlags[] allFlags;
    private ObjectPool<MeshingProjectile> projectilePool;
    private MeshingMode previousRenderMode;
    private MeshTexturedWireframeAdapter wireframeAdapter;

    private MagicLeapController Controller => MagicLeapController.Instance;

    private void Awake()
    {
        allFlags = (MeshDetectorFlags[])Enum.GetValues(typeof(MeshDetectorFlags));
    }

    IEnumerator Start()
    {
        mainCamera = Camera.main;
        meshManager.enabled = false;
        pointCloudManager.enabled = false;
        yield return new WaitUntil(Utils.AreSubsystemsLoaded<XRMeshSubsystem>);
        meshingFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMeshingFeature>();
        wireframeAdapter = GetComponent<MeshTexturedWireframeAdapter>();
        if (!meshingFeature.enabled)
        {
            Debug.LogError($"{nameof(MagicLeapMeshingFeature)} was not enabled. Disabling script");
            enabled = false;
        }

        projectilePool = new ObjectPool<MeshingProjectile>(() => Instantiate(projectilePrefab), (meshProjectile) =>
        {
            meshProjectile.gameObject.SetActive(true);
        }, (meshProjectile) => meshProjectile.gameObject.SetActive(false), defaultCapacity: 20);
        Controller.TriggerPressed += FireProjectile;
        Controller.BumperPressed += CycleSettings;
        Permissions.RequestPermission(Permissions.SpatialMapping, OnPermissionGranted, OnPermissionDenied);
    }

    private void Update()
    {
        ref var meshSettings = ref allSettings[currentIndex];
        ref var activeSettings = ref meshSettings.meshQuerySettings;
        //Show the status text
        statusText.Clear();
        statusText.AppendLine("Current Settings:");
        statusText.AppendLine($"Bounding Box Origin: {meshSettings.meshBoundsOrigin}");
        statusText.AppendLine($"Bounding Box Scale: {meshSettings.meshBoundsScale}");
        statusText.AppendLine($"Bounding Box Rotation: {meshSettings.meshBoundsRotation}");
        statusText.AppendLine($"Fill Hole Length: {activeSettings.fillHoleLength}");
        statusText.AppendLine($"Disconnected Areas Length: {activeSettings.appliedDisconnectedComponentArea}");
        statusText.AppendLine($"Using Ion Allocator: {activeSettings.useIonAllocator}");
        statusText.AppendLine($"Mesh Density: {meshSettings.meshDensity}");
        statusText.Append($"Render Mode: {meshSettings.renderMode}");
        statusText.AppendLine(" Flags:");
        foreach (var flag in allFlags)
        {
            statusText.AppendLine($"{flag} : {activeSettings.meshDetectorFlags.HasFlag(flag)}");
        }

        statusText.AppendLine($"Mesh Density: {meshManager.density}");
        updateText.text = statusText.ToString();
    }

    private void OnDestroy()
    {
        Controller.TriggerPressed -= FireProjectile;
        Controller.BumperPressed -= CycleSettings;
    }

    private void CycleSettings(InputAction.CallbackContext obj)
    {
        currentIndex = (currentIndex + 1) % allSettings.Length;
        UpdateSettings();
    }

    private void FireProjectile(InputAction.CallbackContext obj)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        var projectile = projectilePool.Get();
        projectile.Initialize(projectilePool, ProjectileLifetime);
        projectile.transform.position = mainCamera.transform.position;
        projectile.transform.localScale = Vector3.one * Random.Range(MinScale, MaxScale);
        projectile.rb.AddForce(mainCamera.transform.forward * ProjectileForce);
    }

    void UpdateSettings()
    {
        ref var meshSettings = ref allSettings[currentIndex];
        var currentRenderMode = meshSettings.renderMode;
        meshManager.transform.localScale = meshSettings.meshBoundsScale;
        meshManager.transform.rotation = Quaternion.Euler(meshSettings.meshBoundsRotation);
        meshManager.transform.localPosition = meshSettings.meshBoundsOrigin;
        if (currentRenderMode == MeshingMode.Triangles)
        {
            meshManager.density = meshSettings.meshDensity;
            meshManager.meshPrefab = meshSettings.meshPrefab;
            if (wireframeAdapter != null)
            {
                wireframeAdapter.ComputeConfidences = meshSettings.meshQuerySettings.meshDetectorFlags.HasFlag(MeshDetectorFlags.ComputeConfidence);
                wireframeAdapter.ComputeNormals = meshSettings.meshQuerySettings.meshDetectorFlags.HasFlag(MeshDetectorFlags.ComputeNormals);
                wireframeAdapter.enabled = currentIndex == 0;
            }
        }
        else
        {
            meshingFeature.MeshDensity = meshSettings.meshDensity;
            meshingFeature.MeshBoundsOrigin = meshSettings.meshBoundsOrigin;
            meshingFeature.MeshBoundsRotation = Quaternion.Euler(meshSettings.meshBoundsRotation);
            meshingFeature.MeshBoundsScale = meshSettings.meshBoundsScale;
        }
        meshingFeature.UpdateMeshQuerySettings(in meshSettings.meshQuerySettings);
        meshingFeature.InvalidateMeshes();
        if (previousRenderMode == currentRenderMode)
        {
            return;
        }
        meshManager.DestroyAllMeshes();
        meshManager.enabled = false;
        pointCloudManager.SetTrackablesActive(false);
        pointCloudManager.enabled = false;
        meshingFeature.MeshRenderMode = currentRenderMode;
        var isPointCloud = currentRenderMode == MeshingMode.PointCloud;
        switch (isPointCloud)
        {
            case true:
                meshManager.enabled = false;
                pointCloudManager.enabled = true;
                pointCloudManager.SetTrackablesActive(true);
                break;
            case false:
                pointCloudManager.SetTrackablesActive(false);
                pointCloudManager.enabled = false;
                meshManager.enabled = true;
                break;
        }
        previousRenderMode = currentRenderMode;
    }

    private void OnPermissionGranted(string permission)
    {
        meshManager.enabled = true;
        previousRenderMode = MeshingMode.Triangles;
        UpdateSettings();
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {permission} permission. Please add to manifest. Disabling script.");
        enabled = false;
    }
}

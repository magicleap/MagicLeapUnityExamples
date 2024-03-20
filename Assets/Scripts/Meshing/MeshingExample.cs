using MagicLeap.Android;
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
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using Random = UnityEngine.Random;
using Utils = MagicLeap.Examples.Utils;

public class MeshingExample : MonoBehaviour
{
    [Serializable]
    private class MeshQuerySetting
    {
        [SerializeField] public MagicLeapMeshingFeature.MeshingQuerySettings meshQuerySettings;
        [SerializeField] public float meshDensity;
        [SerializeField] public Vector3 meshBoundsOrigin;
        [SerializeField] public Vector3 meshBoundsRotation;
        [SerializeField] public Vector3 meshBoundsScale;
        [SerializeField] public MagicLeapMeshingFeature.MeshingMode renderMode;
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
    private MagicLeapInput mlInputs;
    private int currentIndex;

    [SerializeField] private MeshQuerySetting[] allSettings;

    [SerializeField] private Text updateText;

    private StringBuilder statusText = new();
    private MagicLeapMeshingFeature.MeshDetectorFlags[] allFlags;
    private ObjectPool<MeshingProjectile> projectilePool;
    private MagicLeapMeshingFeature.MeshingMode previousRenderMode;

    private void Awake()
    {
        allFlags = (MagicLeapMeshingFeature.MeshDetectorFlags[])Enum.GetValues(typeof(MagicLeapMeshingFeature.MeshDetectorFlags));
    }

    IEnumerator Start()
    {
        mainCamera = Camera.main;
        meshManager.enabled = false;
        pointCloudManager.enabled = false;
        yield return new WaitUntil(Utils.AreSubsystemsLoaded<XRMeshSubsystem>);
        meshingFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMeshingFeature>();
        if (!meshingFeature.enabled)
        {
            Debug.LogError($"{nameof(MagicLeapMeshingFeature)} was not enabled. Disabling script");
            enabled = false;
        }

        projectilePool = new ObjectPool<MeshingProjectile>(() => Instantiate(projectilePrefab), (meshProjectile) =>
        {
            meshProjectile.gameObject.SetActive(true);
        }, (meshProjectile) => meshProjectile.gameObject.SetActive(false), defaultCapacity: 20);
        mlInputs = new();
        mlInputs.Enable();
        mlInputs.Controller.Trigger.performed += TriggerHandler;
        mlInputs.Controller.Bumper.performed += BumperHandler;
        Permissions.RequestPermission(MLPermission.SpatialMapping, OnPermissionGranted, OnPermissionDenied);
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
        if (mlInputs == null)
        {
            return;
        }
        mlInputs.Controller.Trigger.performed -= TriggerHandler;
        mlInputs.Controller.Bumper.performed -= BumperHandler;
    }

    private void TriggerHandler(InputAction.CallbackContext obj)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        currentIndex = (currentIndex + 1) % allSettings.Length;
        UpdateSettings();
    }

    private void BumperHandler(InputAction.CallbackContext obj)
    {
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
        if (currentRenderMode == MagicLeapMeshingFeature.MeshingMode.Triangles)
        {
            meshManager.density = meshSettings.meshDensity;
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
        var isPointCloud = currentRenderMode == MagicLeapMeshingFeature.MeshingMode.PointCloud;
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
        previousRenderMode = allSettings[0].renderMode;
        UpdateSettings();
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Planes Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        enabled = false;
    }
}

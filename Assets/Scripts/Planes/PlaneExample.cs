using MagicLeap.Android;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using Utils = MagicLeap.Examples.Utils;

public class PlaneExample : MonoBehaviour
{
    private ARPlaneManager planeManager;

    [SerializeField, Tooltip("Maximum number of planes to return each query")]
    private uint maxResults = 100;

    [SerializeField, Tooltip("Minimum plane area to treat as a valid plane")]
    private float minPlaneArea = 0.25f;

    [SerializeField]
    private Text status;

    private Camera mainCamera;
    private bool permissionGranted = false;

    private IEnumerator Start()
    {
        mainCamera = Camera.main;
        yield return new WaitUntil(Utils.AreSubsystemsLoaded<XRPlaneSubsystem>);
        planeManager = FindObjectOfType<ARPlaneManager>();
        if (planeManager == null)
        {
            Debug.LogError("Failed to find ARPlaneManager in scene. Disabling Script");
            enabled = false;
        }
        else
        {
            // disable planeManager until we have successfully requested required permissions
            planeManager.enabled = false;
        }

        permissionGranted = false;
        Permissions.RequestPermission(MLPermission.SpatialMapping, OnPermissionGranted, OnPermissionDenied);
    }
    
    private void Update()
    {
        UpdateQuery();
    }

    private void UpdateQuery()
    {
        if (planeManager != null && planeManager.enabled && permissionGranted)
        {
            var newQuery = new MLXrPlaneSubsystem.PlanesQuery
            {
                Flags = planeManager.requestedDetectionMode.ToMLXrQueryFlags() | MLXrPlaneSubsystem.MLPlanesQueryFlags.SemanticAll,
                BoundsCenter = mainCamera.transform.position,
                BoundsRotation = mainCamera.transform.rotation,
                BoundsExtents = Vector3.one * 20f,
                MaxResults = maxResults,
                MinPlaneArea = minPlaneArea
            };

            MLXrPlaneSubsystem.Query = newQuery;
            status.text = $"Detection Mode:\n<B>{planeManager.requestedDetectionMode}</B>\n\n" +
                          $"Query Flags:\n<B>{newQuery.Flags.ToString().Replace(" ", "\n")}</B>\n\n" +
                          $"Query MaxResultss:\n<B>{newQuery.MaxResults}</B>\n\n" +
                          $"Query MinPlaneArea:\n<B>{newQuery.MinPlaneArea}</B>\n\n" +
                          $"Plane GameObjects:\n<B>{PlanePrefabExample.Count}</B>";
        }
    }

    private void OnPermissionGranted(string permission)
    {
        planeManager.enabled = true;
        permissionGranted = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Planes Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        enabled = false;
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using MagicLeap.Android;

public class FacialExpressionExample : MonoBehaviour
{
    [SerializeField]
    private Text statusDescription;

    private MagicLeapFacialExpressionFeature facialExpressionFeature;
    private MagicLeapFacialExpressionFeature.BlendShapeProperties[] blendShapeProperties;
    private bool permissionGranted;
    private bool isInitialized;
    private readonly StringBuilder strBuilder = new();

    private void Awake()
    {
        Permissions.RequestPermission(MLPermission.FacialExpression, OnPermissionGranted, OnPermissionDenied);
    }

    private void Update()
    {
        if (!permissionGranted)
        {
            Debug.LogWarning($"Waiting for facial expression permissions to be granted.");
            return;
        }

        if (!isInitialized)
        {
            Init();
        }

        strBuilder.Clear();

        facialExpressionFeature.GetBlendShapesInfo(ref blendShapeProperties);

        for (int i = 0; i < blendShapeProperties.Length; i++)
        {
            string facialBlendShape = FacialExpressionUtil.FacialBlendShapes[blendShapeProperties[i].FacialBlendShape];

            bool isValid = blendShapeProperties[i].Flags.HasFlag(MagicLeapFacialExpressionFeature.BlendShapePropertiesFlags.ValidBit);
            bool isTracked = blendShapeProperties[i].Flags.HasFlag(MagicLeapFacialExpressionFeature.BlendShapePropertiesFlags.TrackedBit);
            float weight = blendShapeProperties[i].Weight;

            if (isValid && isTracked && weight > 0)
            {
                int weightPercentage = Mathf.FloorToInt(100 * weight);

                strBuilder.AppendLine($"<b>{facialBlendShape}</b>: {weightPercentage}%\n");
            }
        }

        statusDescription.text = strBuilder.ToString();
    }

    private void Init()
    {
        // the client will request data for every type of possible blend shape
        var allRequestedFacialBlendShapes = Enum.GetValues(typeof(MagicLeapFacialExpressionFeature.FacialBlendShape)).Cast<MagicLeapFacialExpressionFeature.FacialBlendShape>().ToArray();

        facialExpressionFeature = OpenXRSettings.Instance.GetFeature<MagicLeapFacialExpressionFeature>();

        facialExpressionFeature.CreateClient(allRequestedFacialBlendShapes);

        blendShapeProperties = new MagicLeapFacialExpressionFeature.BlendShapeProperties[allRequestedFacialBlendShapes.Length];

        for (int i = 0; i < blendShapeProperties.Length; i++)
        {
            blendShapeProperties[i].FacialBlendShape = allRequestedFacialBlendShapes[i];
            blendShapeProperties[i].Weight = 0;
            blendShapeProperties[i].Flags = MagicLeapFacialExpressionFeature.BlendShapePropertiesFlags.None;
        }

        isInitialized = true;
    }

    private void OnPermissionDenied(string permission)
    {
        MLPluginLog.Error($"{permission} denied, example won't function.");
    }

    private void OnPermissionGranted(string permission)
    {
        permissionGranted = true;
    }
}

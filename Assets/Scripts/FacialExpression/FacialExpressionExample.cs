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
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.FacialExpressions;

public class FacialExpressionExample : MonoBehaviour
{
    [SerializeField]
    private Text statusDescription;

    private MagicLeapFacialExpressionFeature facialExpressionFeature;
    private BlendShapeProperties[] blendShapeProperties;
    private bool permissionGranted;
    private bool isInitialized;
    private readonly StringBuilder strBuilder = new();

    private void Awake()
    {
        Permissions.RequestPermission(Permissions.FacialExpression, OnPermissionGranted, OnPermissionDenied);
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

            bool isValid = blendShapeProperties[i].Flags.HasFlag(BlendShapePropertiesFlags.ValidBit);
            bool isTracked = blendShapeProperties[i].Flags.HasFlag(BlendShapePropertiesFlags.TrackedBit);
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
        var allRequestedFacialBlendShapes = Enum.GetValues(typeof(FacialBlendShape)).Cast<FacialBlendShape>().ToArray();

        facialExpressionFeature = OpenXRSettings.Instance.GetFeature<MagicLeapFacialExpressionFeature>();

        facialExpressionFeature.CreateClient(allRequestedFacialBlendShapes);

        blendShapeProperties = new BlendShapeProperties[allRequestedFacialBlendShapes.Length];

        for (int i = 0; i < blendShapeProperties.Length; i++)
        {
            blendShapeProperties[i].FacialBlendShape = allRequestedFacialBlendShapes[i];
            blendShapeProperties[i].Weight = 0;
            blendShapeProperties[i].Flags = BlendShapePropertiesFlags.None;
        }

        isInitialized = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"{permission} denied, example won't function.");
    }

    private void OnPermissionGranted(string permission)
    {
        permissionGranted = true;
    }
}

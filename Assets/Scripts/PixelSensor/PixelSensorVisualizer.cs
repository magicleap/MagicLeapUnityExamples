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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicLeap.OpenXR.Features.PixelSensors;
using UnityEngine.XR.MagicLeap;

public class PixelSensorVisualizer : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private TextMesh streamIdText;
    [SerializeField] private PixelSensorMaterialTable materialTable = new();
    
    private Texture2D targetTexture;

    private RenderTexture yuvRenderTexture;
    private YCbCrHardwareBufferRenderer hardwareBufferRenderer;

    private int maxDepthKey;
    private int minDepthKey;

    private float minDepth;
    private float maxDepth = 5;
    
    public void Reset()
    {
        if (targetTexture == null)
        {
            return;
        }
        Destroy(targetTexture);
        targetTexture =  null;
    }

    private void Start()
    {
        maxDepthKey = Shader.PropertyToID("_MaxDepth");
        minDepthKey = Shader.PropertyToID("_MinDepth");
    }

    private void OnDestroy()
    {
        Reset();
        hardwareBufferRenderer?.Dispose();
        hardwareBufferRenderer = null;
        if (yuvRenderTexture != null)
        {
            yuvRenderTexture.Release();
            Destroy(yuvRenderTexture);
            yuvRenderTexture = null;
        }
        if (targetRenderer.material != null)
        {
            Destroy(targetRenderer.material);
        }
    }

    private TextureFormat GetTextureFormat(PixelSensorFrameType frameType)
    {
        return frameType switch
        {
            PixelSensorFrameType.Grayscale => TextureFormat.R8,
            PixelSensorFrameType.Rgba8888 => TextureFormat.RGBA32,
            PixelSensorFrameType.Yuv420888 => TextureFormat.YUY2,
            PixelSensorFrameType.Jpeg => TextureFormat.RGBA32,
            PixelSensorFrameType.Depth32 or PixelSensorFrameType.DepthRaw or PixelSensorFrameType.DepthConfidence or PixelSensorFrameType.DepthFlags => TextureFormat.RFloat,
            _ => throw new ArgumentOutOfRangeException(nameof(frameType), frameType, null)
        };
    }

    public void Initialize(uint streamId, MagicLeapPixelSensorFeature pixelSensorFeature, PixelSensorId sensorType)
    {
        if (streamIdText != null)
        {
            streamIdText.text = $"{sensorType.SensorName}";
        }
        //Only get the depth values for depth sensor
        if (!sensorType.SensorName.Contains("depth", StringComparison.CurrentCultureIgnoreCase))
        {
            return;
        }

        if (!pixelSensorFeature.QueryPixelSensorCapability(sensorType, PixelSensorCapabilityType.Depth, streamId, out var range))
        {
            return;
        }

        if (range.IntRange.HasValue)
        {
            minDepth = range.IntRange.Value.Min;
            maxDepth = range.IntRange.Value.Max;
        }

        if (range.FloatRange.HasValue)
        {
            minDepth = range.FloatRange.Value.Min;
            maxDepth = range.FloatRange.Value.Max;
        }
    }

    public void ProcessFrame(in PixelSensorFrame frame)
    {
        if (!frame.IsValid || targetRenderer == null || frame.Planes.Length == 0)
        {
            return;
        }
        var frameType = frame.FrameType;
        ref var firstPlane = ref frame.Planes[0];
        switch (frameType)
        {
            case PixelSensorFrameType.Grayscale:
            case PixelSensorFrameType.Rgba8888:
            case PixelSensorFrameType.Depth32:
            case PixelSensorFrameType.DepthRaw:
            case PixelSensorFrameType.DepthConfidence:
            case PixelSensorFrameType.DepthFlags:
            case PixelSensorFrameType.Jpeg:
            {
                if (targetTexture == null)
                {
                    targetTexture = new Texture2D((int)firstPlane.Width, (int)firstPlane.Height, GetTextureFormat(frameType), false);
                    var materialToUse = materialTable.GetMaterialForFrameType(frame.FrameType);
                    targetRenderer.material = materialToUse;
                    targetRenderer.material.mainTexture = targetTexture;
                    UpdateMaterialParameters();
                }
                
                var byteArray = ArrayPool<byte>.Shared.Rent(firstPlane.ByteData.Length);
                firstPlane.ByteData.CopyTo(byteArray);
                if (frameType == PixelSensorFrameType.Jpeg)
                {                    
                    targetTexture.LoadImage(byteArray);
                }
                else
                {
                    targetTexture.LoadRawTextureData(byteArray);
                }
                ArrayPool<byte>.Shared.Return(byteArray, true);
                targetTexture.Apply();
                break;
            }
            case PixelSensorFrameType.Yuv420888:
            {
                if (hardwareBufferRenderer == null)
                {
                    if (yuvRenderTexture != null)
                    {
                        yuvRenderTexture.Release();
                    }
                    yuvRenderTexture = new RenderTexture((int)firstPlane.Width, (int)firstPlane.Height, 0, RenderTextureFormat.ARGB32);
                    hardwareBufferRenderer = new YCbCrHardwareBufferRenderer(false);
                    hardwareBufferRenderer.SetRenderBuffer(yuvRenderTexture);
                    targetRenderer.material.mainTexture = yuvRenderTexture;
                }

                for (var i = 0; i < frame.Planes.Length; i++)
                {
                    ref var plane = ref frame.Planes[i];
                    hardwareBufferRenderer.SetPlaneData( plane.Stride, plane.PixelStride, plane.ByteData);
                }
                hardwareBufferRenderer.Render();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateMaterialParameters()
    {
        targetRenderer.material.SetFloat(maxDepthKey, maxDepth);
        targetRenderer.material.SetFloat(minDepthKey, minDepth);
    }

    [Serializable]
    public class PixelSensorMaterialTable
    {
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private List<PixelSensorMaterialPair> materialPairs = new();

        private Dictionary<PixelSensorFrameType, Material> materialTable;

        public Material GetMaterialForFrameType(PixelSensorFrameType frameType)
        {
            materialTable ??= materialPairs.ToDictionary(mp => mp.frameType, mp => mp.frameTypeMaterial);
            return materialTable.GetValueOrDefault(frameType, defaultMaterial);
        }

        [Serializable]
        public struct PixelSensorMaterialPair
        {
            [SerializeField] public PixelSensorFrameType frameType;
            [SerializeField] public Material frameTypeMaterial;
        }
    }
}

// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.ARSubsystems;
using MagicLeap.OpenXR.Features.Meshing;
using UnityEngine.XR;

/// <summary>
/// Adapts and prepares meshes from ARMeshManager to use the TexturedWireframe material and shader.
/// <para>Somewhat based on the texture-based wireframe technique described in
/// http://sibgrapi.sid.inpe.br/col/sid.inpe.br/sibgrapi/2010/09.15.18.18/doc/texture-based_wireframe_rendering.pdf
/// See Figure 5c and related description</para>
/// </summary>
public class MeshTexturedWireframeAdapter : MonoBehaviour
{
    [SerializeField, Tooltip("The ARMeshManager in the scene.")]
    private ARMeshManager meshManager = null;

    [SerializeField, Tooltip("The textured wireframe material.")]
    private Material wireframeMaterial = null;

    private Texture2D proceduralTexture = null;
    private int lineTextureWidth = 2048;       // Overall width of texture used for the line (will be 1px high)
    private int linePixelWidth = 24;           // Line fill pixel width (left side) representing line, over background
    private int lineEdgeGradientWidth = 4;     // Falloff gradient pixel size to smooth line edge

    float[] confidenceData = new float[0];
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> indices = new List<int>();
    private List<Vector3> uvs = new List<Vector3>();

    private MagicLeapMeshingFeature meshingFeature;
    private Mesh meshReference;

    public MagicLeapMeshingFeature MeshingFeature
    {
        get
        {
            meshingFeature ??= OpenXRSettings.Instance.GetFeature<MagicLeapMeshingFeature>();
            return meshingFeature;
        }
    }

    public bool ComputeConfidences { get; set; }

    public bool ComputeNormals { get; set; }

    void Awake()
    {
        if (wireframeMaterial != null)
        {
            // Create procedural texture used to render the line (more control this way over mip-map levels)
            proceduralTexture = new Texture2D(lineTextureWidth, 1, TextureFormat.ARGB32, 7, true);
            int w = linePixelWidth - (lineEdgeGradientWidth / 2);
            for (int i = 0; i < lineTextureWidth; i++)
            {
                var color = i <= w ? Color.white :
                            i > w + lineEdgeGradientWidth ? Color.clear :
                            Color.Lerp(Color.white, Color.clear, (i - w) / (float)lineEdgeGradientWidth);
                proceduralTexture.SetPixel(i, 0, color);
            }
            proceduralTexture.wrapMode = TextureWrapMode.Clamp;
            proceduralTexture.Apply();

            wireframeMaterial.mainTexture = proceduralTexture;
        } 
    }

    private void OnEnable()
    {
        // Subscribe to meshing changes
        if (meshManager != null)
        {
            meshManager.meshesChanged += OnMeshUpdatedOrAdded;
        }
    }

    private void OnDisable()
    {
        if (meshManager != null)
        {
            meshManager.meshesChanged -= OnMeshUpdatedOrAdded;
        }
    }

    void OnDestroy()
    {
        if (proceduralTexture != null)
        {
            Destroy(proceduralTexture);
            proceduralTexture = null;
        }
    }

    private MeshId GetMeshIdForMesh(Mesh mesh)
    {
        var meshName = mesh.name;
        var splitResult = meshName.Split(" ").LastOrDefault();
        if (string.IsNullOrEmpty(splitResult))
        {
            return MeshId.InvalidId;
        }
        return MeshingFeature.CreateMeshId(splitResult);
    }

    private void OnMeshUpdatedOrAdded(ARMeshesChangedEventArgs args)
    {
        foreach (var meshFilter in args.added)
            ConfigureTrianglesForMesh(meshFilter);

        foreach (var meshFilter in args.updated)
            ConfigureTrianglesForMesh(meshFilter);
    }

    private void ConfigureTrianglesForMesh(MeshFilter meshFilter)
    {
        // Adapt the mesh for the textured wireframe shader.
        if (meshFilter != null)
        {
            meshReference = meshFilter.mesh;

            var meshId = GetMeshIdForMesh(meshReference);
            if (meshId == MeshId.InvalidId)
                return;

            meshReference.GetVertices(vertices);
            uvs.Clear();
            for (int i = 0; i < vertices.Count; i++)
            {
                uvs.Add(Vector3.forward);
            }
            meshReference.GetTriangles(indices, 0);

            bool validConfidences = ComputeConfidences && MeshingFeature != null
                && MeshingFeature.GetMeshData(in meshId, out _, out _, out confidenceData)
                && uvs.Count == confidenceData.Length;

            // Encode confidence in uv.z
            for (int i = 0; i < uvs.Count; i++)
            {
                var uv = uvs[i];
                uv.z = validConfidences ? confidenceData[i] : 1;
                uvs[i] = uv;
            }

            int indicesOrigCount = indices.Count;
            for (int i = 0; i < indicesOrigCount; i += 3)
            {
                var i1 = indices[i];
                var i2 = indices[i + 1];
                var i3 = indices[i + 2];

                var v1 = vertices[i1];
                var v2 = vertices[i2];
                var v3 = vertices[i3];

                var uv1 = uvs[i1];
                var uv2 = uvs[i2];
                var uv3 = uvs[i3];

                // Create a new center vertex of each triangle, adjusting indices and add new triangles
                // Will use Incenter of Triangle (center that is equidistant to edges).
                // This allows the line width to be consistent regardless of triangle size.
                // Also allows line width to be adjusted dynamically.
                // Calculate position of incenter vertex
                var a = Vector3.Distance(v2, v3);
                var b = Vector3.Distance(v1, v3);
                var c = Vector3.Distance(v1, v2);
                var sum = a + b + c;
                var vIntercenter = new Vector3((a * v1.x + b * v2.x + c * v3.x) / sum,
                                               (a * v1.y + b * v2.y + c * v3.y) / sum,
                                               (a * v1.z + b * v2.z + c * v3.z) / sum);
                vertices.Add(vIntercenter);
                int iC = vertices.Count - 1;

                // Distance to edge, or radius of incircle
                var s = sum / 2.0f;
                var r = Mathf.Sqrt(((s - a) * (s - b) * (s - c)) / s);

                // Calculate UV for the incenter vertex for a 1mm target line width
                // Half of each line is rendered on the edges of each triangle, so .001/2 = .0005
                // Can be adjusted in shader to vary line width dynamically.
                float lineWidth = .0005f;
                float segmentPixels = (r / lineWidth) * linePixelWidth;
                float segmentUV = segmentPixels / lineTextureWidth;

                Vector3 centerUV = Vector3.one * segmentUV;
                centerUV.z = validConfidences ? (a * uv1.z + b * uv2.z + c * uv3.z) / sum : 1;
                uvs.Add(centerUV);

                // Modify triangle to emanate from new center vertex, along with 2 new triangles
                indices[i + 2] = iC;

                indices.Add(i1);
                indices.Add(iC);
                indices.Add(i3);

                indices.Add(i2);
                indices.Add(i3);
                indices.Add(iC);
            }

            meshReference.SetVertices(vertices);
            meshReference.SetUVs(0, uvs);
            meshReference.SetTriangles(indices, 0);
            if (ComputeNormals)
            {
                meshReference.RecalculateNormals();
            }
        }
    }
}

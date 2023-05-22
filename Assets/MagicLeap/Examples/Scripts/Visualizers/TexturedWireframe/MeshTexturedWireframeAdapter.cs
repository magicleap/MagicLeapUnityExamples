using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System.Linq;

// Somewhat based on the texture-based wireframe technique described in
// http://sibgrapi.sid.inpe.br/col/sid.inpe.br/sibgrapi/2010/09.15.18.18/doc/texture-based_wireframe_rendering.pdf
// See Figure 5c and related description


/// <summary>
/// Adapts and prepares meshes from MeshingSubsytemComponent to use the TexturedWireframe material and shader.
/// </summary>
public class MeshTexturedWireframeAdapter : MonoBehaviour
{
    public Material wireframeMaterial
    {
        get { return _wireframeMaterial; }
    }

    [SerializeField, Tooltip("The MeshingSubsystemComponent from which to get updates on mesh generation.")]
    private MeshingSubsystemComponent _meshingSubsystemComponent = null;

    [SerializeField, Tooltip("The textured wireframe material.")]
    private Material _wireframeMaterial = null;

    private Texture2D _proceduralTexture = null;
    private int _lineTextureWidth = 2048;       // Overall width of texture used for the line (will be 1px high)
    private int _linePixelWidth = 24;           // Line fill pixel width (left side) representing line, over background
    private int _lineEdgeGradientWidth = 4;     // Falloff gradient pixel size to smooth line edge
    
    private List<float> confidences = new List<float>();
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> indices = new List<int>();
    private List<Vector3> uvs = new List<Vector3>();

    private Mesh meshReference;

    void Awake()
    {
        if (_wireframeMaterial != null)
        {
            // Create procedural texture used to render the line (more control this way over mip-map levels)
            _proceduralTexture = new Texture2D(_lineTextureWidth, 1, TextureFormat.ARGB32, 7, true);
            int linePixelWidth = _linePixelWidth - (_lineEdgeGradientWidth / 2);
            for (int i = 0; i < _lineTextureWidth; i++)
            {
                var color = i <= linePixelWidth ? Color.white :
                            i > linePixelWidth + _lineEdgeGradientWidth ? Color.clear :
                            Color.Lerp(Color.white, Color.clear, (float)(i - linePixelWidth) / (float)_lineEdgeGradientWidth);
                _proceduralTexture.SetPixel(i, 0, color);
            }
            _proceduralTexture.wrapMode = TextureWrapMode.Clamp;
            _proceduralTexture.Apply();

            _wireframeMaterial.mainTexture = _proceduralTexture;
        }

        // Subscribe to meshing changes
        if (_meshingSubsystemComponent != null)
        {
            _meshingSubsystemComponent.meshAdded += MeshAddedOrUpdated;
            _meshingSubsystemComponent.meshUpdated += MeshAddedOrUpdated;
        }
    }

    void OnDestroy()
    {
        if (_proceduralTexture != null)
        {
            Destroy(_proceduralTexture);
            _proceduralTexture = null;
        }

        if (_meshingSubsystemComponent != null)
        {
            _meshingSubsystemComponent.meshAdded -= MeshAddedOrUpdated;
            _meshingSubsystemComponent.meshUpdated -= MeshAddedOrUpdated;
        }
    }

    private void MeshAddedOrUpdated(UnityEngine.XR.MeshId meshId)
    {
        if (_meshingSubsystemComponent.requestedMeshType != MeshingSubsystemComponent.MeshType.Triangles)
        {
            return;
        }

        if (_meshingSubsystemComponent.meshIdToGameObjectMap.TryGetValue(meshId, out var meshGameObject))
        {
            // Check that the mesh is using the wireframe material before proceeding
            var meshRenderer = meshGameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.sharedMaterial != _wireframeMaterial)
            {
                return;
            }

            // Adapt the mesh for the textured wireframe shader.
            var meshFilter = meshGameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshReference = meshFilter.mesh;

                bool validConfidences = _meshingSubsystemComponent.requestVertexConfidence &&
                                        _meshingSubsystemComponent.TryGetConfidence(meshId, confidences);

                meshReference.GetVertices(vertices);
                uvs.Clear();
                for (int i = 0; i < vertices.Count; i++)
                {
                    uvs.Add(Vector3.forward);
                }
                meshReference.GetTriangles(indices, 0);

                // Encode confidence in uv.z
                if (validConfidences)
                {
                    for (int i = 0; i < uvs.Count; i++)
                    {
                        var uv = uvs[i];
                        uv.z = confidences[i];
                        uvs[i] = uv;
                    }
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
                    float segmentPixels = (r / lineWidth) * (float)_linePixelWidth;
                    float segmentUV = segmentPixels / (float)_lineTextureWidth;

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
                if (_meshingSubsystemComponent.computeNormals)
                {
                    meshReference.RecalculateNormals();
                }
            }
        }
    }
}

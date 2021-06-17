// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// Manages plane rendering based on plane detection from MLPlanesBehavior.
    /// </summary>
    public class PlanesVisualizer : MonoBehaviour
    {
        /// <summary>
        /// Caches the results of Boundaries by Id, for the current session.
        /// </summary>
        private class BoundariesCache
        {
            public ulong Id { get; set; }
            public List<PlanesCache> Planes { get; set; }

            /// <summary>
            /// Initialize the boundaries cache with the provided id.
            /// </summary>
            /// <param name="id">The id of the boundaries.</param>
            public BoundariesCache(ulong id)
            {
                Id = id;
                Planes = new List<PlanesCache>();
            }

            /// <summary>
            /// Invoke the hide method on all planes.
            /// </summary>
            public void Hide()
            {
                if (Planes != null)
                {
                    Planes.ForEach(x => x.Hide());
                }
            }

            /// <summary>
            /// Invokes the clear method on all planes.
            /// </summary>
            public void Clear()
            {
                if (Planes != null)
                {
                    Planes.ForEach(x => x.Clear());
                }
            }
        }

        /// <summary>
        /// Caches the results of the Planes, including the holes.
        /// </summary>
        private class PlanesCache
        {
            public GameObject Plane { get; set; }
            public List<GameObject> Holes { get; set; }

            /// <summary>
            /// Initialize the plane cache with the provided visual plane GameObject.
            /// </summary>
            /// <param name="plane">The visual plane GameObject.</param>
            public PlanesCache(GameObject plane)
            {
                Plane = plane;
                Holes = new List<GameObject>();
            }

            /// <summary>
            /// Make all the planes and holes invisible.
            /// </summary>
            public void Hide()
            {
                if(Plane != null)
                {
                    Holes.ForEach(x => x.SetActive(false));
                    Plane.SetActive(false);
                }
            }

            /// <summary>
            /// Clear and destroy all the planes and holes.
            /// </summary>
            public void Clear()
            {
                Holes.ForEach(x => Destroy(x));
                Holes.Clear();

                if (Plane != null)
                {
                    Destroy(Plane);
                }
            }
        }

        /// <summary>
        /// The different ways to visualize the planes.
        /// </summary>
        public enum VisualizerRenderMode
        {
            Border,
            Texture,
            Polygon
        }

        [SerializeField, Tooltip("The MLPlanesBehavior to subscribe to.")]
        private MLPlanesBehavior _planesBehavior = null;

        [SerializeField, Tooltip("Object prefab to use for plane visualization.")]
        private GameObject planeVisualPrefab = null;

        [Header("Materials")]
        [Tooltip("Material used for wall planes.")]
        public Material wallMaterial = null;
        [Tooltip("Material used for floor planes.")]
        public Material floorMaterial = null;
        [Tooltip("Material used for ceiling planes.")]
        public Material ceilingMaterial = null;
        [Tooltip("Material used for other types of planes.")]
        public Material defaultMaterial = null;
        [Tooltip("Material used to show the planes")]
        public Material borderMaterial = null;

        public VisualizerRenderMode RenderMode
        {
            get;
            private set;
        }

        private GameObject _planesParent = null;
        private GameObject _boundariesParent = null;

        // List of all the planes being rendered..
        private List<GameObject> _planeCache = null;
        private List<uint> _planeFlags = null;

        // List of all boundaries and holes being rendered.
        private List<BoundariesCache> _boundariesCache = null;


        /// <summary>
        /// Initializes all variables and makes sure needed components exist.
        /// </summary>
        void Awake()
        {
            if(_planesBehavior == null)
            {
                Debug.LogError("Error: PlanesVisualizer._planesBehavior is not set, disabling script.");
                enabled = false;
                return;
            }

            if (planeVisualPrefab == null)
            {
                Debug.LogError("Error: PlanesVisualizer.planeVisualPrefab is not set, disabling script.");
                enabled = false;
                return;
            }

            if (wallMaterial == null || floorMaterial == null || ceilingMaterial == null || defaultMaterial == null || borderMaterial == null)
            {
                Debug.LogError("Error: PlanesVisualizer.Materials is not set, disabling script.");
                enabled = false;
                return;
            }

            MeshRenderer planeRenderer = planeVisualPrefab.GetComponent<MeshRenderer>();
            if (planeRenderer == null)
            {
                Debug.LogError("Error: PlanesVisualizer MeshRenderer component not found, disabling script.");
                enabled = false;
                return;
            }

            _planesParent = new GameObject();
            _planeCache = new List<GameObject>();
            _planeFlags = new List<uint>();

            _boundariesParent = new GameObject();
            _boundariesCache = new List<BoundariesCache>();

            #if PLATFORM_LUMIN
            _planesBehavior.OnQueryPlanesResult += HandleOnQueriedPlanes;
            #endif
        }

        private void Start()
        {
            #if PLATFORM_LUMIN
            MLResult result = MLHeadTracking.Start();
            if (result.IsOk)
            {
                MLHeadTracking.RegisterOnHeadTrackingMapEvent(HandleOnHeadTrackingMapEvent);
            }
            else
            {
                Debug.LogError("PlanesVisualizer could not register to head tracking events because MLHeadTracking could not be started.");
            }
            #endif
        }
        /// <summary>
        /// Clean up.
        /// Destroys all planes instances created.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            _planesBehavior.OnQueryPlanesResult -= HandleOnQueriedPlanes;

            MLHeadTracking.UnregisterOnHeadTrackingMapEvent(HandleOnHeadTrackingMapEvent);
            MLHeadTracking.Stop();
            #endif

            DestroyPlanes();
            DestroyBoundaries();
        }

        /// <summary>
        /// Toggle showing of borders and refresh all plane materials.
        /// </summary>
        public void CycleMode()
        {
            RenderMode = (RenderMode != VisualizerRenderMode.Polygon) ? (VisualizerRenderMode)((int)RenderMode + 1) : VisualizerRenderMode.Border;

            // Hide the parent GameObject based on the active type.
            if (RenderMode == VisualizerRenderMode.Border)
            {
                _boundariesParent.SetActive(false);
                _planesParent.SetActive(true);
            }
            else if (RenderMode == VisualizerRenderMode.Polygon)
            {
                _planesParent.SetActive(false);
                _boundariesParent.SetActive(true);
            }

            RefreshAllPlaneMaterials();
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Creates or updates the visual representations of the planes
        /// </summary>
        /// <param name="planes"> Array of planes to visualize.</param>
        private void VisualizePlanes(MLPlanes.Plane[] planes)
        {
            if (planes == null)
                return;

            // Hide the unused plane cache.
            int index = planes.Length > 0 ? planes.Length - 1 : 0;
            for (int i = index; i < _planeCache.Count; ++i)
            {
                _planeCache[i].SetActive(false);
            }

            // Update the active planes.
            for (int i = 0; i < planes.Length; ++i)
            {
                GameObject planeVisual;
                if (i < _planeCache.Count)
                {
                    planeVisual = _planeCache[i];
                    planeVisual.SetActive(true);
                }
                else
                {
                    planeVisual = Instantiate(planeVisualPrefab);
                    planeVisual.transform.SetParent(_planesParent.transform);

                    _planeCache.Add(planeVisual);
                    _planeFlags.Add(0);
                }

                planeVisual.transform.position = planes[i].Center;
                planeVisual.transform.rotation = planes[i].Rotation;
                planeVisual.transform.localScale = new Vector3(planes[i].Width, planes[i].Height, 1f);

                _planeFlags[i] = planes[i].Flags;
            }

            RefreshAllPlaneMaterials();
        }

        /// <summary>
        /// Creates or updates the visual representations of the boundaries.
        /// </summary>
        /// <param name="boundaries"> Array of boundaries to visualize.</param>
        private void VisualizeBoundaries(MLPlanes.Boundaries[] boundaries)
        {
            if (boundaries == null)
                return;

            // Cleanup the cache.
            foreach (var cache in _boundariesCache)
            {
                MLPlanes.Boundaries? bounds = boundaries.FirstOrDefault(x => x.Id == cache.Id);
                if (bounds == null)
                {
                    // The boundaries no longer exist, destroy.
                    _boundariesCache.Remove(cache);
                    cache.Clear();
                }
                else
                {
                    // Hide the boundaries.
                    cache.Hide();
                }
            }

            // Update the active boundaries.
            foreach (MLPlanes.Boundaries worldBoundary in boundaries)
            {
                BoundariesCache cache = _boundariesCache.Find(x => x.Id == worldBoundary.Id);
                if (cache == null)
                {
                    cache = new BoundariesCache(worldBoundary.Id);
                    _boundariesCache.Add(cache);
                }

                // Obtain the list of mesh filters.
                List<PlanesCache> planesCache = cache.Planes;

                // Enable or create a new GameObject to render the plane boundary.
                for (int i = 0; i < worldBoundary.PlaneBoundaries.Length; ++i)
                {
                    MeshFilter meshFilter;
                    if (i < planesCache.Count)
                    {
                        // Obtain the reference and enable it.
                        meshFilter = planesCache[i].Plane.GetComponent<MeshFilter>();
                        meshFilter.mesh.Clear();

                        meshFilter.gameObject.SetActive(true);
                    }
                    else
                    {
                        GameObject planeVisual = new GameObject();
                        planeVisual.transform.SetParent(_boundariesParent.transform);

                        planeVisual.transform.position = Vector3.zero;
                        planeVisual.transform.rotation = Quaternion.identity;

                        // Assign standard shader and pick a random color for the polygon surface.
                        Renderer renderer = planeVisual.AddComponent<MeshRenderer>();
                        renderer.material.shader = Shader.Find("Standard");
                        renderer.material.color = new Color(Random.Range(0f, 0.5f), Random.Range(0f, 1f), Random.Range(0f, 1f));

                        meshFilter = planeVisual.AddComponent<MeshFilter>();
                        meshFilter.mesh = new Mesh();

                        // Add the reference to the cache.
                        planesCache.Add(new PlanesCache(meshFilter.gameObject));
                    }

                    // Calculate the centroid and update the vertices to include it.
                    Vector3 centroid = FindCentroid(worldBoundary.PlaneBoundaries[i].Polygon.Vertices);
                    Vector3[] vertices = worldBoundary.PlaneBoundaries[i].Polygon.Vertices;

                    // Increase the size by 1, to hold the last element the centroid.
                    System.Array.Resize(ref vertices, worldBoundary.PlaneBoundaries[i].Polygon.Vertices.Length + 1);
                    vertices[vertices.Length - 1] = centroid;

                    // Assign the vertices and triangles to the mesh filter.
                    meshFilter.mesh.vertices = vertices;
                    meshFilter.mesh.triangles = GenerateTriangles(vertices);

                    VisualizeHoles(meshFilter.transform, planesCache[i], worldBoundary.PlaneBoundaries[i].Holes);
                }
            }
        }

        /// <summary>
        /// Creates or updates the visual representations of the holes inside planes.
        /// </summary>
        /// <param name="parent"> The parent plane to put the holes mesh under.</param>
        /// <param name="planesCache"> Holds the last queries planes.</param>
        /// <param name="holes"> Array of holes to visualize.</param>
        private void VisualizeHoles(Transform parent, PlanesCache planesCache, MLPlanes.Polygon[] holes)
        {
            if (holes == null)
                return;

            // Enable or create a new GameObject to render the plane boundary.
            for (int i = 0; i < holes.Length; ++i)
            {
                MeshFilter meshFilter;
                if (i < planesCache.Holes.Count)
                {
                    // Obtain the reference and enable it.
                    meshFilter = planesCache.Holes[i].GetComponent<MeshFilter>();
                    meshFilter.mesh.Clear();

                    // Re-parent and offset slightly to prevent z fighting.
                    meshFilter.transform.SetParent(parent);
                    meshFilter.transform.position = parent.forward * 0.01f;
                    meshFilter.transform.rotation = Quaternion.identity;

                    meshFilter.gameObject.SetActive(true);
                }
                else
                {
                    GameObject holeVisual = new GameObject();
                    holeVisual.name = "Hole";

                    // Parent and offset slightly to prevent z fighting.
                    holeVisual.transform.SetParent(parent);
                    holeVisual.transform.position = parent.forward * 0.01f;
                    holeVisual.transform.rotation = Quaternion.identity;

                    // Assign standard shader and pick a random color for the polygon surface.
                    Renderer renderer = holeVisual.AddComponent<MeshRenderer>();
                    renderer.material.shader = Shader.Find("Standard");
                    renderer.material.color = Color.red;

                    meshFilter = holeVisual.AddComponent<MeshFilter>();
                    meshFilter.mesh = new Mesh();

                    // Add the reference to the cache.
                    planesCache.Holes.Add(meshFilter.gameObject);
                }

                // Calculate the centroid and update the vertices to include it.
                Vector3 centroid = FindCentroid(holes[i].Vertices);
                Vector3[] vertices = holes[i].Vertices;

                // Increase the size by 1, to hold the last element the centroid.
                System.Array.Resize(ref vertices, holes[i].Vertices.Length + 1);
                vertices[vertices.Length - 1] = centroid;

                // Assign the vertices and triangles to the mesh filter.
                meshFilter.mesh.vertices = vertices;
                meshFilter.mesh.triangles = GenerateTriangles(vertices, true);
            }
        }
        #endif

        /// <summary>
        /// Refreshes all the plane materials.
        /// </summary>
        private void RefreshAllPlaneMaterials()
        {
            for (int i = 0; i < _planeCache.Count; ++i)
            {
                if (!_planeCache[i].activeSelf)
                {
                    continue;
                }

                Renderer planeRenderer = _planeCache[i].GetComponent<Renderer>();
                if (planeRenderer != null)
                {
                    SetRenderTexture(planeRenderer, _planeFlags[i]);
                }
            }
        }

        /// <summary>
        /// Sets correct texture to plane based on surface type.
        /// </summary>
        /// <param name="renderer">The renderer component.</param>
        /// <param name="flags">The flags of the plane containing the surface type.</param>
        private void SetRenderTexture(Renderer renderer, uint flags)
        {
            if (RenderMode == VisualizerRenderMode.Border)
            {
                renderer.sharedMaterial = borderMaterial;
            }
            else if (RenderMode == VisualizerRenderMode.Texture)
            {
                if ((flags & (uint)MLPlanes.QueryFlags.SemanticWall) != 0)
                {
                    renderer.sharedMaterial = wallMaterial;
                }
                else if ((flags & (uint)MLPlanes.QueryFlags.SemanticFloor) != 0)
                {
                    renderer.sharedMaterial = floorMaterial;
                }
                else if ((flags & (uint)MLPlanes.QueryFlags.SemanticCeiling) != 0)
                {
                    renderer.sharedMaterial = ceilingMaterial;
                }
                else
                {
                    renderer.sharedMaterial = defaultMaterial;
                }
            }
        }

        /// <summary>
        /// Create triangles for the supplied vertices, the last vertex is the center.
        /// </summary>
        /// <param name="vertices">Vertices in linear order, with the last vertex being the center.</param>
        /// <param name="clockwise">Winding order.</param>
        /// <returns></returns>
        private int[] GenerateTriangles(Vector3[] vertices, bool clockwise = false)
        {
            List<int> triangles = new List<int>();

            if (vertices.Length > 3)
            {
                // The last element is the centroid.
                for (int i = 0; i < vertices.Length - 2; i++)
                {
                    triangles.AddRange(new int[]
                    {
                    i + ((!clockwise) ? 1 : 0),
                    i + ((clockwise)? 1 : 0),
                    vertices.Length - 1
                    });
                }

                // Close the last face.
                triangles.AddRange(new int[]
                {
                    0,
                    vertices.Length - ((clockwise) ? 1 : 2),
                    vertices.Length - ((clockwise) ? 2 : 1)
                });
            }

            return triangles.ToArray();
        }

        /// <summary>
        /// Locates the center point of all the combined vertices.
        /// </summary>
        /// <param name="vertices"> Array of vertices to use</param>
        private Vector3 FindCentroid(Vector3[] vertices)
        {
            Vector3 center;
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];

            for (int i = 1; i < vertices.Length; i++)
            {
                Vector3 pos = vertices[i];

                // X
                if (pos.x < min.x)
                {
                    min.x = pos.x;
                }
                else if (pos.x > max.x)
                {
                    max.x = pos.x;
                }

                // Y
                if (pos.y < min.y)
                {
                    min.y = pos.y;
                }
                else if (pos.y > max.y)
                {
                    max.y = pos.y;
                }

                // Z
                if (pos.z < min.z)
                {
                    min.z = pos.z;
                }
                else if (pos.z > max.z)
                {
                    max.z = pos.z;
                }
            }

            center = min + 0.5f * (max - min);

            return center;
        }

        /// <summary>
        /// Destroys the GameObjects and clear the plane cache.
        /// </summary>
        private void DestroyPlanes()
        {
            _planeCache.ForEach(x => Destroy(x));
            _planeCache.Clear();
            _planeFlags.Clear();
        }

        /// <summary>
        /// Destroy the GameObjects and clear the boundaries cache.
        /// </summary>
        private void DestroyBoundaries()
        {
            _boundariesCache.ForEach(boundaries => boundaries.Clear());
            _boundariesCache.Clear();
        }

        /// <summary>
        /// Handle in charge of clearing all planes/boundaries if a new session occurs.
        /// </summary>
        /// <param name="mapEvents"> Map Events that happened.</param>
        private void HandleOnHeadTrackingMapEvent(MLHeadTracking.MapEvents mapEvents)
        {
            #if PLATFORM_LUMIN
            if (mapEvents.IsNewSession())
            {
                DestroyPlanes();
                DestroyBoundaries();
            }
            #endif
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Updates and creates new planes based on the query result.
        /// This function reuses previously allocated memory to convert all planes
        /// to the new ones by changing their transforms, it allocates new objects
        /// if the current result ammount is bigger than the ones already stored.
        /// </summary>
        public void HandleOnQueriedPlanes(MLPlanes.Plane[] planes, MLPlanes.Boundaries[] boundaries)
        {
            if (RenderMode == VisualizerRenderMode.Border || RenderMode == VisualizerRenderMode.Texture)
            {
                VisualizePlanes(planes);
            }
            else
            {
                VisualizeBoundaries(boundaries);
            }
        }
        #endif
    }
}

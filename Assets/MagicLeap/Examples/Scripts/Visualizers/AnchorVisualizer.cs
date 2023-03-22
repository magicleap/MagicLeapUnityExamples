using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class AnchorVisualizer : MonoBehaviour
    {
        public GameObject anchorPrefab;
        public MLAnchors.Request query;
        private Transform mainCamera;
        private Transform xrOrigin;
        private Dictionary<string, AnchorVisual> map = new Dictionary<string, AnchorVisual>();

        void Start()
        {
            mainCamera = Camera.main.transform;
            xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>().transform;
            query = new MLAnchors.Request();
        }

        void Update()
        {
            if (query == null)
                return;

            var mlResultStart = query.Start(new MLAnchors.Request.Params(mainCamera.position, 0, 0, true));
            var mlResultGet = query.TryGetResult(out MLAnchors.Request.Result result);

            if (mlResultStart.IsOk && mlResultGet.IsOk)
            {
                foreach (var anchor in result.anchors)
                {
                    string id = anchor.Id;
                    if (map.ContainsKey(id) == false)
                    {
                        GameObject anchorGO = Instantiate(anchorPrefab, xrOrigin);
                        map.Add(id, anchorGO.AddComponent<AnchorVisual>());
                    }
                    map[id].Set(anchor);
                }
            }
        }
    }
}



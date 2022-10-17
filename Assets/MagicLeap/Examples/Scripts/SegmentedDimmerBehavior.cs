using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    [RequireComponent(typeof(MeshRenderer))]
    public class SegmentedDimmerBehavior : MonoBehaviour
    {
        private MeshRenderer meshRenderer;

        private float opacity;

        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer.material != null)
            {
                opacity = meshRenderer.material.GetFloat("_DimmingValue");
            }
        }    
        
        private void Update()
        {
            if(meshRenderer.material != null)
            {
                opacity = Mathf.PingPong(Time.time / (1 + transform.GetSiblingIndex()), 1);
                meshRenderer.material.SetFloat("_DimmingValue", opacity);
            }
        }
    }    
}

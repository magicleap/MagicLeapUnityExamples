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

using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// A Button template that works with VirtualRaycastController
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class MediaPlayerButton : MonoBehaviour
    {
        public System.Action<Vector3> OnRaycastEnter;
        public System.Action<Vector3> OnRaycastContinue;
        public System.Action<Vector3> OnRaycastExit;

        public System.Action<MLInput.Controller.Button> OnControllerButtonDown;
        public System.Action<MLInput.Controller.Button> OnControllerButtonUp;

        public System.Action<float> OnControllerTriggerDown;
        public System.Action<float> OnControllerTriggerUp;

        public System.Action<MLInput.Controller> OnControllerDrag;

        public Color EnabledColor = Color.white;
        public Color DisabledColor = Color.red;

        public Renderer[] EnableDisableColorList;

        private Renderer _meshRenderer;

        public Material Material
        {
            get
            {
                if (_meshRenderer != null)
                {
                    return _meshRenderer.material;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_meshRenderer != null)
                {
                    _meshRenderer.material = value;
                    if (enabled)
                    {
                        _meshRenderer.material.color = EnabledColor;
                    }
                    else
                    {
                        _meshRenderer.material.color = DisabledColor;
                    }
                }
            }
        }

        private void Awake()
        {
            _meshRenderer = GetComponent<Renderer>();
        }
        protected virtual void OnDisable()
        {
            Collider buttonCollider = GetComponent<Collider>();
            if (buttonCollider != null)
            {
                buttonCollider.enabled = false;
            }

            foreach (Renderer renderer in EnableDisableColorList)
            {
                renderer.material.SetColor("_Color", DisabledColor);
            }
        }

        protected virtual void OnEnable()
        {
            Collider buttonCollider = GetComponent<Collider>();
            if (buttonCollider != null)
            {
                buttonCollider.enabled = true;
            }

            foreach (Renderer renderer in EnableDisableColorList)
            {
                renderer.material.SetColor("_Color", EnabledColor);
            }
        }
    }
}

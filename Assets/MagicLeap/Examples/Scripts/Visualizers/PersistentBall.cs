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

using MagicLeap.Core;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class utilizes TransformBinding as well as interactive and visual elements for demonstrating persistent content. This class displays
    /// feedback for when the user touches this content, displays the id of the content,
    /// and displays which PCF this content is bound to.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(ContentTap))]
    public class PersistentBall : MonoBehaviour
    {
        #if PLATFORM_LUMIN
        public TransformBinding BallTransformBinding = null;
        #endif

        [SerializeField, Tooltip("Destroyed content effect.")]
        private GameObject _destroyedContentEffect = null;

        [SerializeField, Tooltip("Text to display name.")]
        private TextMesh _nameText = null;

        [SerializeField, Tooltip("The object that creates the highlight effect.")]
        private GameObject _highlightEffect = null;

        [SerializeField, Tooltip("LineRenderer that will point to the bound PCF.")]
        private LineRenderer _lineToPCF = null;

        private Renderer[] _renderers = null;
        private Collider _collider = null;

        private ContentDragController _controllerDrag = null;

        /// <summary>
        /// Validates parameters, initializes renderers, and listens to events.
        /// </summary>
        void Start()
        {
            if (_destroyedContentEffect == null)
            {
                Debug.LogError("Error: PersistentBall._destroyedContentEffect is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_nameText == null)
            {
                Debug.LogError("Error: PersistentBall._nameText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_highlightEffect == null)
            {
                Debug.LogError("Error: PersistentBall._highlightEffect is not set, disabling script.");
                enabled = false;
                return;
            }
            _highlightEffect.SetActive(false);

            if (_lineToPCF == null)
            {
                Debug.LogError("Error: PersistentBall._lineToPCF is not set, disabling script.");
                enabled = false;
                return;
            }

            _lineToPCF.positionCount = 2;
            _lineToPCF.enabled = false;

            _renderers = GetComponentsInChildren<Renderer>();
            _collider = GetComponent<Collider>();

            ContentTap contentTap = GetComponent<ContentTap>();
            contentTap.OnContentTap += DestroyContent;

            _nameText.transform.position = transform.position + new Vector3(0, 0.25f, 0);
            _nameText.text = "Object ID:" + gameObject.GetInstanceID();

           #if PLATFORM_LUMIN
            MLResult result = MLPersistentCoordinateFrames.Start();
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: PersistentBall failed starting MLPersistentCoordinateFrames, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }
            #endif
        }

        /// <summary>
        /// Stops the MLPersistentCoordinateFrames api and unregisters from events.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLPersistentCoordinateFrames.Stop();
            #endif

            if (_controllerDrag != null)
            {
                _controllerDrag.OnDrag -= HandleOnDrag;
                _controllerDrag.OnEndDrag -= HandleOnEndDrag;
            }
        }

        /// <summary>
        /// Controller touches this content.
        /// </summary>
        /// <param name="other">Collider of Controller</param>
        void OnTriggerEnter(Collider other)
        {
            ContentDragController controllerDrag = other.GetComponent<ContentDragController>();
            if (controllerDrag == null)
            {
                return;
            }

            _controllerDrag = controllerDrag;
            _controllerDrag.OnDrag += HandleOnDrag;
            _controllerDrag.OnEndDrag += HandleOnEndDrag;
            Highlight();
        }

        /// <summary>
        /// Controller leaves this content.
        /// </summary>
        /// <param name="other">Collider of Controller</param>
        void OnTriggerExit(Collider other)
        {
            ContentDragController controllerDrag = other.GetComponent<ContentDragController>();

            if (controllerDrag != null && _controllerDrag == controllerDrag)
            {
                _controllerDrag.OnDrag -= HandleOnDrag;
                _controllerDrag.OnEndDrag -= HandleOnEndDrag;
                _controllerDrag = null;
                Unhighlight();
            }
        }

        /// <summary>
        /// Destroys the given content and spawns another gameObject as a particle system effect.
        /// </summary>
        /// <param name="content">Content to destroy, assumed to be this content.</param>
        public void DestroyContent(GameObject content)
        {
            Instantiate(_destroyedContentEffect, transform.position, Quaternion.identity);
            #if PLATFORM_LUMIN
            BallTransformBinding.UnBind();
            #endif
            Destroy(gameObject);
        }

        /// <summary>
        /// Show visual effect when highlighting.
        /// </summary>
        private void Highlight()
        {
            _highlightEffect.SetActive(true);

            #if PLATFORM_LUMIN
            if (BallTransformBinding?.PCF != null && BallTransformBinding.PCF.CurrentResultCode == MLResult.Code.Ok)
            {
                _lineToPCF.SetPosition(0, transform.position);
                _lineToPCF.SetPosition(1, BallTransformBinding.PCF.Position);
                _lineToPCF.enabled = PCFVisualizer.IsVisualizing;
            }
            #endif
        }

        /// <summary>
        /// Remove highlight visual effects.
        /// </summary>
        private void Unhighlight()
        {
            _highlightEffect.SetActive(false);
            _lineToPCF.enabled = false;
        }

        /// <summary>
        /// Enable/Disable Renderers.
        /// </summary>
        /// <param name="enable">Toggle value</param>
        private void EnableRenderers(bool enable)
        {
            foreach (Renderer r in _renderers)
            {
                r.enabled = enable;
            }
        }

        /// <summary>
        /// Handle for OnDrag event.
        /// </summary>
        private void HandleOnDrag()
        {
            _lineToPCF.SetPosition(0, transform.position);
        }

        /// <summary>
        /// Handle for OnEndDrag event.
        /// </summary>
        private void HandleOnEndDrag()
        {
            #if PLATFORM_LUMIN
            if (MLPersistentCoordinateFrames.IsLocalized)
            {
                BallTransformBinding.Update();
            }
            #endif
        }
    }
}

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
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// Utility Component to allow users to drag persistent content around.
    /// </summary>
    public class ContentDragHandler : MonoBehaviour
    {
        Vector3 _controllerPositionOffset;
        Quaternion _controllerOrientationOffset;
        ContentDragController _controllerDrag;
        bool _dragStarted = false;

        /// <summary>
        /// Register for events when a ContentDragController enters the trigger area
        /// </summary>
        /// <param name="other">Collider of ContentDragController</param>
        void OnTriggerEnter(Collider other)
        {
            ContentDragController controllerDrag = other.GetComponent<ContentDragController>();
            if (controllerDrag == null)
            {
                return;
            }

            _controllerDrag = controllerDrag;
            _controllerDrag.OnBeginDrag += HandleBeginDrag;
            _controllerDrag.OnDrag += HandleDrag;
            _controllerDrag.OnEndDrag += HandleEndDrag;
        }

        /// <summary>
        /// Unregister for events when a ContentDragController leaves the trigger area
        /// </summary>
        /// <param name="other">Collider of ContentDragController</param>
        void OnTriggerExit(Collider other)
        {
            ContentDragController controllerDrag = other.GetComponent<ContentDragController>();
            if (controllerDrag == null)
            {
                return;
            }

            if (_controllerDrag == controllerDrag)
            {
                _controllerDrag.OnBeginDrag -= HandleBeginDrag;
                _controllerDrag.OnDrag -= HandleDrag;
                _controllerDrag.OnEndDrag -= HandleEndDrag;
                _controllerDrag = null;
            }
        }

        /// <summary>
        /// Unregister for events in case this component gets destroyed while being dragged
        /// </summary>
        void OnDestroy()
        {
            if (_controllerDrag != null)
            {
                _controllerDrag.OnBeginDrag -= HandleBeginDrag;
                _controllerDrag.OnDrag -= HandleDrag;
                _controllerDrag.OnEndDrag -= HandleEndDrag;
                _controllerDrag = null;
            }
        }

        /// <summary>
        /// Set up offsets when dragging begins
        /// </summary>
        private void HandleBeginDrag()
        {
            Vector3 relativeDirection = transform.position - _controllerDrag.transform.position;
            _controllerPositionOffset = transform.InverseTransformDirection(relativeDirection);
            _controllerOrientationOffset = Quaternion.Inverse(_controllerDrag.transform.rotation) * transform.rotation;

            _dragStarted = true;
        }

        /// <summary>
        /// Update transform while dragging
        /// </summary>
        private void HandleDrag()
        {
            if (_dragStarted)
            {
                transform.position = _controllerDrag.transform.position + transform.TransformDirection(_controllerPositionOffset);
                transform.rotation = _controllerDrag.transform.rotation * _controllerOrientationOffset;
            }
        }

        /// <summary>
        /// Save binding when dragging ends
        /// </summary>
        private void HandleEndDrag()
        {
            _dragStarted = false;
        }
    }
}

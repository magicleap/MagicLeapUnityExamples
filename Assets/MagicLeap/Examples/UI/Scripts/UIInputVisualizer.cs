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
    [RequireComponent(typeof(MLInputModuleBehavior))]
    [RequireComponent(typeof(LineRenderer))]
    public class UIInputVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The prefab that will represent a visual cursor.")]
        private GameObject _cursorPrefab = null;

        private MLInputModuleBehavior _inputModule = null;
        private LineRenderer _beam = null;
        private GameObject _cursor = null;
        private Vector3 _cursorOffset = Vector3.zero;

        private void Start()
        {
            _inputModule = GetComponent<MLInputModuleBehavior>();
            _beam = GetComponent<LineRenderer>();

            _cursor = Instantiate(_cursorPrefab);
            _cursor.name = "Cursor";
        }

        private void Update()
        {
            if (_inputModule.PointerLineSegment.End.HasValue)
            {
                _beam.enabled = (_inputModule.PointerInput == MLInputModuleBehavior.PointerInputType.Controller);
                _beam.SetPosition(0, _inputModule.PointerLineSegment.Start);
                _beam.SetPosition(1, _inputModule.PointerLineSegment.End.Value);

#if PLATFORM_LUMIN
                _beam.widthMultiplier = MLDevice.WorldScale;
#endif

                _cursor.SetActive(true);

                _cursorOffset = ((_inputModule.PointerLineSegment.Start - _inputModule.PointerLineSegment.End.Value).normalized / 100);
                _cursor.transform.position = _inputModule.PointerLineSegment.End.Value + _cursorOffset;

#if PLATFORM_LUMIN
                _cursor.transform.localScale = new Vector3(MLDevice.WorldScale, MLDevice.WorldScale, MLDevice.WorldScale);
#endif
            }
            else
            {
                _cursor.SetActive(false);
                _beam.enabled = false;
            }
        }
    }
}

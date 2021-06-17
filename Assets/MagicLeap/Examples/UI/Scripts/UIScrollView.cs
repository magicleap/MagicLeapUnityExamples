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
using UnityEngine.UI;

namespace MagicLeap
{
    /// <summary>
    /// This class allows for smooth vertical scrolling of a ScrollRect directly through code.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class UIScrollView : MonoBehaviour
    {
        private ScrollRect _scrollRect = null;

        private float _lastValue = 0;
        private float _targetValue = 0;
        private bool _scroll = false;
        private float _elapsedTime = 0;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void Update()
        {
            if (_scroll)
            {
                _elapsedTime += Time.deltaTime;
                _scrollRect.verticalScrollbar.value = Mathf.Lerp(_lastValue, _targetValue, _elapsedTime);

                if (_scrollRect.verticalScrollbar.value == _targetValue)
                {
                    _scroll = false;
                }
            }
        }

        /// <summary>
        /// Scroll up smoothly based on delta time.
        /// </summary>
        public void ScrollUp()
        {
            _lastValue = _scrollRect.verticalScrollbar.value;

            _targetValue =
                Mathf.Clamp(_lastValue + _scrollRect.verticalScrollbar.size, 0, 1);

            _scroll = true;
            _elapsedTime = 0;
        }

        /// <summary>
        /// Scroll down smoothly based on delta time.
        /// </summary>
        public void ScrollDown()
        {
            _lastValue = _scrollRect.verticalScrollbar.value;

            _targetValue =
                Mathf.Clamp(_lastValue - _scrollRect.verticalScrollbar.size, 0, 1);

            _scroll = true;
            _elapsedTime = 0;
        }
    }
}

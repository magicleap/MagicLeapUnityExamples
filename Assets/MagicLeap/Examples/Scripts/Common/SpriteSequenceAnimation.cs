// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    /// <summary>
    /// A utility behavior to draw a sequence of sprites unto a sprite renderer
    /// </summary>
    public class SpriteSequenceAnimation : MonoBehaviour
    {
        [SerializeField, Tooltip("Sprite Renderer")]
        private Image _spriteRenderer = null;

        [SerializeField, Tooltip("Sprites to cycle through")]
        private Sprite[] _sprites = null;

        [SerializeField, Tooltip("Duration of a frame in seconds (1/fps)"), Min(0.01f)]
        private float _frameDuration = 0.033f;

        private int _currIndex = 0;
        private float _currDuration = 0;

        void Awake()
        {
            if (_spriteRenderer == null)
            {
                Debug.LogError("Error: SpriteSequenceAnimation._spriteRenderer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_sprites == null)
            {
                Debug.LogError("Error: SpriteSequenceAnimation._sprites is not set, disabling script.");
                enabled = false;
                return;
            }
        }

        void Update()
        {
            _currDuration += Time.deltaTime;
            if (_currDuration >= _frameDuration)
            {
                _currDuration -= _frameDuration;
                _currIndex++;
                _currIndex %= _sprites.Length;

                _spriteRenderer.sprite = _sprites[_currIndex];
            }
        }
    }
}

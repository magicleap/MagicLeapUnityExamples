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

namespace MagicLeap
{
    /// <summary>
    /// A Toggle Button template that works with VirtualRaycastController
    /// </summary>
    [DisallowMultipleComponent]
    public class MediaPlayerToggle : MediaPlayerButton
    {
        public event System.Action OnToggle;

        protected override void OnEnable()
        {
            OnControllerTriggerDown += HandleTriggerDown;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            OnControllerTriggerDown -= HandleTriggerDown;

            base.OnDisable();
        }

        private void HandleTriggerDown(float triggerValue)
        {
            OnToggle?.Invoke();
        }
    }
}

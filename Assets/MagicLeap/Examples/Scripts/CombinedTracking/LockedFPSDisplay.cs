// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MagicLeap.Examples
{
    public class LockedFPSDisplay : MonoBehaviour
    {
        public Text textMesh;
        [Tooltip("Time in seconds for every refresh cycle on FPS display.")]
        public float refreshRate = 0.5f;

        /// <summary>
        /// Time that passed since last refresh of the FPS value on display
        /// </summary>
        private float timeSinceLastRefresh = 0f;

        /// <summary>
        /// Target Frames per Second based on refresh rate of the ML display
        /// </summary>
        private const int targetFPS = 60;

        /// <summary>
        /// Stored history of times needed to load last frames
        /// </summary>
        private readonly float[] frames = new float[targetFPS];

        /// <summary>
        /// A pointer to specific frame in the table;
        /// </summary>
        private int framePointer = targetFPS;

        /// <summary>
        /// Current Frames Per Second
        /// </summary>
        private float fps;

        void Update()
        {
            // Point to new period of time in history
            framePointer = (framePointer + 1) % targetFPS;
            // Apply time needed to load last frame to history
            frames[framePointer] = Time.unscaledDeltaTime;
            // Count Frames Per Second based on total time to load latest frames in history
            fps = Mathf.RoundToInt(Mathf.Clamp(targetFPS / frames.Sum(), 0f, targetFPS));
            // Add time since last refresh
            timeSinceLastRefresh += Time.unscaledDeltaTime;
            // Refresh Display if it's time for it
            if (timeSinceLastRefresh > refreshRate)
            {
                timeSinceLastRefresh = 0;
                textMesh.text = fps.ToString();
            }
        }
    }
}

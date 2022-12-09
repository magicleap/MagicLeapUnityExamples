// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2021-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class QRCodeVisual : MonoBehaviour
    {
        [SerializeField]
        private TextMesh dataText;
        private Timer disableTimer;

        void Awake()
        {
            disableTimer = new Timer(3f);
        }

        void Update()
        {
            if (gameObject.activeSelf && disableTimer.LimitPassed)
            {
                gameObject.SetActive(false);
            }
        }

        public void Set(MLMarkerTracker.MarkerData data)
        {
            disableTimer?.Reset();
            transform.position = data.Pose.position;
            transform.rotation = data.Pose.rotation;
            dataText.text = data.ToString();
            gameObject.SetActive(true);
        }


    }
}

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
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    public class MLMRCameraExample : MonoBehaviour
    {
        [SerializeField]
        private GameObject visualizer;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private MLControllerConnectionHandlerBehavior controllerConnection;

        private bool toggleCapture = true;

        private void OnEnable()
        {
            if(visualizer == null)
            {
                visualizer = FindObjectOfType<MLMRCameraVisualizer>().gameObject;
                if(visualizer != null)
                {
                    visualizer.SetActive(false);
                }
            }
            MLMRCamera.OnFrameCapture += HandleOnFrameCapture;
            MLMRCamera.OnCaptureComplete += HandleOnCaptureComplete;
            MLMRCamera.OnError += HandleOnError;
        }

        private void OnDisable()
        {
            MLMRCamera.OnFrameCapture -= HandleOnFrameCapture;
            MLMRCamera.OnCaptureComplete -= HandleOnCaptureComplete;
            MLMRCamera.OnError -= HandleOnError;
        }

        private void Update()
        {
#if PLATFORM_LUMIN
            if(controllerConnection.ConnectedController.IsBumperDown)
            {
                if(toggleCapture)
                {
                    MLMRCamera.StopCapture();
                }
                else
                {
                    MLMRCamera.StartCapture();
                }

                toggleCapture = !toggleCapture;
            }
#endif
        }

        private void HandleOnFrameCapture(MLMRCamera.Frame frame)
        {
            if(!visualizer.activeSelf)
            {
                visualizer.SetActive(true);
            }

            statusText.text = "<b>Current Frame</b>:\n";
            statusText.text += frame.ToString();
            foreach(MLMRCamera.Frame.ImagePlane imagePlane in frame.ImagePlanes)
            {
                statusText.text += "\n\n<b>Image Plane</b>:\n";
                statusText.text += imagePlane.ToString();
            }
        }

        private void HandleOnError(MLResult result)
        {
            if (visualizer.activeSelf)
            {
                visualizer.SetActive(false);
            }

            statusText.text = "<b>Error</b>:" + result;
        }

        private void HandleOnCaptureComplete()
        {
            if (visualizer.activeSelf)
            {
                visualizer.SetActive(false);
            }

            statusText.text = "<b>Capture Complete</b>";
        }
    }
}

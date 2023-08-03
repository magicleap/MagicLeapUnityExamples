// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class provides examples of how you can use haptics
    /// on the Control.
    /// </summary>
    public class WorldCameraExample : MonoBehaviour
    {

        [SerializeField]
        private Text cameraAndFrameType;

        [SerializeField]
        private Text frameInfo;

        [SerializeField]
        private WorldCameraVisualizer worldCamVisualizer;
         
        [SerializeField]
        private MLWorldCamera.Settings Settings;

        private MLWorldCamera worldCamera;

        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();
        private bool permissionGranted = false;

        private int activeCombinationIndex = 0;
        private List<(MLWorldCamera.CameraId, MLWorldCamera.Frame.Type)> cameraAndFrameTypes = new List<(MLWorldCamera.CameraId, MLWorldCamera.Frame.Type)>();

        /// <summary>
        /// Initialize variables, callbacks and check null references.
        /// </summary>
        void OnEnable()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
            // canceled event used to detect when bumper button is released
            controllerActions.Bumper.performed += HandleOnBumper;
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
            worldCamera = new MLWorldCamera();
            GetCameraAndFrameTypes();
            MLPermissions.RequestPermission(MLPermission.Camera, permissionCallbacks);
        }

        void Update()
        {
            if (worldCamera.IsConnected)
                GetCameraData();
        }

        private void GetCameraAndFrameTypes()
        {
            foreach (MLWorldCamera.CameraId camId in Enum.GetValues(typeof(MLWorldCamera.CameraId)))
            {
                if (camId == MLWorldCamera.CameraId.All)
                    continue;

                if (Settings.Cameras.HasFlag(camId))
                {
                    foreach (MLWorldCamera.Mode mode in Enum.GetValues(typeof(MLWorldCamera.Mode)))
                    {
                        if (mode == MLWorldCamera.Mode.Unknown)
                            continue;
                        
                        if (Settings.Mode.HasFlag(mode))
                        {
                            MLWorldCamera.Frame.Type type = (MLWorldCamera.Frame.Type)mode;
                            cameraAndFrameTypes.Add((camId, type));
                        }
                    }
                }
            }
        }

        private void OnPermissionGranted(string permission)
        {
            if (!permissionGranted)
            {
                if (permission == MLPermission.Camera)
                {
                    permissionGranted = true;
                    ConnectCamera();
                }
            }
        }

        private void OnPermissionDenied(string permission)
        {
            if (permission == MLPermission.Camera)
            {
                permissionGranted = false;
                enabled = false;
            }
        }

        private void ConnectCamera()
        {
            var settings = new MLWorldCamera.Settings(MLWorldCamera.Mode.NormalExposure | MLWorldCamera.Mode.LowExposure, MLWorldCamera.CameraId.All);
            worldCamera.Connect(in settings);
        }

        private void GetCameraData()
        {
            var result = worldCamera.GetLatestWorldCameraData(out MLWorldCamera.Frame[] frames);
            if (!result.IsOk)
                return;

            foreach (var frame in frames)
            {
                var cameraAndFrameType = new ValueTuple<MLWorldCamera.CameraId, MLWorldCamera.Frame.Type>(frame.CameraId, frame.FrameType);
                if (cameraAndFrameTypes[activeCombinationIndex] == cameraAndFrameType)
                {
                    worldCamVisualizer.RenderFrame(frame);
                    this.cameraAndFrameType.text = $"Camera: {cameraAndFrameType.Item1}\nType: {cameraAndFrameType.Item2}";
                    frameInfo.text = $"CameraPose: { frame.CameraPose}, CameraIntrinsics: { frame.CameraIntrinsics}, FrameBuffer: {frame.FrameBuffer}";
                    break;
                }
            }

        }
        private void IncrementIndex()
        {
            activeCombinationIndex++;
            if (activeCombinationIndex >= cameraAndFrameTypes.Count)
                activeCombinationIndex = 0;
        }

        void OnDisable()
        {
            controllerActions.Bumper.performed -= HandleOnBumper;
            mlInputs.Dispose();
        }


        void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                if (!MLPermissions.CheckPermission(MLPermission.Camera).IsOk)
                {
                    permissionGranted = false;
                    MLPermissions.RequestPermission(MLPermission.Camera, permissionCallbacks);
                }
            }
        }

        private void HandleOnBumper(InputAction.CallbackContext obj) => IncrementIndex();
    }
}

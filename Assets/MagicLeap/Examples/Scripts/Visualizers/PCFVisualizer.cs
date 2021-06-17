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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class helps visualize all the PCFs that are associated with the current localized map.
    /// </summary>
    public class PCFVisualizer : MonoBehaviour
    {
        #pragma warning disable 414
        [SerializeField, Tooltip("The PCF visual prefab")]
        private GameObject pcfVisualPrefab = null;
        #pragma warning restore 414

        private List<GameObject> _pcfVisuals = new List<GameObject>();

        private float _secondsToRequeue = 3;

        public static bool IsVisualizing
        {
            get;
            private set;
        }

        #if PLATFORM_LUMIN
        public delegate void OnFindAllPCFsDelegate(List<MLPersistentCoordinateFrames.PCF> allPCFs);
        public static event OnFindAllPCFsDelegate OnFindAllPCFs;
        #endif

        /// <summary>
        /// Starts up MLPersistentCoordinateFrames and registers to events.
        /// Defaults to visualizing pcfs (will contiously find and queue PCFs for updates).
        /// </summary>
        void Start()
        {
            #if PLATFORM_LUMIN
            MLResult result = MLPersistentCoordinateFrames.Start();
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: PCFVisualizer failed starting MLPersistentCoordinateFrames, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }

            MLPersistentCoordinateFrames.PCF.OnStatusChange += HandlePCFStatusChange;
            MLPersistentCoordinateFrames.OnLocalized += HandleOnLocalized;

            IsVisualizing = true;
            #endif
        }

        /// <summary>
        /// Stops the MLPersistentCoordinateFrames api and unregisters from events.
        /// </summary>
        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            MLPersistentCoordinateFrames.Stop();
            MLPersistentCoordinateFrames.OnLocalized -= HandleOnLocalized;
            MLPersistentCoordinateFrames.PCF.OnStatusChange -= HandlePCFStatusChange;
            #endif
        }

        /// <summary>
        /// Toggles IsVisualizing and all the pcf visuals.
        /// </summary>
        public void Toggle()
        {
            IsVisualizing = !IsVisualizing;

            foreach (GameObject pcfVisual in _pcfVisuals)
            {
                pcfVisual.SetActive(IsVisualizing);
            }
        }

        /// <summary>
        /// Every _secondsToRequeue, this coroutine will attempt to find all pcfs and queue them all for updates.
        /// </summary>
        private IEnumerator ContinuouslyFindAllPCFs()
        {
            // Uses a while loop so that we can catch PCFs being created during runtime.
            while (true)
            {
                yield return new WaitForEndOfFrame();

                if (IsVisualizing)
                {
                    #if PLATFORM_LUMIN
                    // MLPersistentCoordinateFrames.FindAllPCFs() returns the PCFs found in the current map.
                    // Calling this function is expensive and is only called repeatedly for demonstration purposes.
                    // This function will create new pcfs during a new headpose session.
                    MLResult result = MLPersistentCoordinateFrames.FindAllPCFs(out List<MLPersistentCoordinateFrames.PCF> allPCFs);
                    if (!result.IsOk)
                    {
                        if (result.Result == MLResult.Code.PassableWorldLowMapQuality || result.Result == MLResult.Code.PassableWorldUnableToLocalize)
                        {
                            Debug.LogWarningFormat("Map quality not sufficient enough for PCFVisualizer to find all pcfs. Reason: {0}", result);
                        }
                        else
                        {
                            Debug.LogErrorFormat("Error: PCFVisualizer failed to find all PCFs because MLPersistentCoordinateFrames failed to get all PCFs. Reason: {0}", result);
                        }
                    }
                    else
                    {
                        OnFindAllPCFs?.Invoke(allPCFs);
                    }
                    #endif

                   yield return new WaitForSeconds(_secondsToRequeue);
                }
            }
        }

        /// <summary>
        /// Handler for PCF status changes.
        /// </summary>
        /// <param name="pcfStatus">The PCF status.</param>
        /// <param name="pcf">The PCF.</param>
        private void HandlePCFStatusChange(MLPersistentCoordinateFrames.PCF.Status pcfStatus, MLPersistentCoordinateFrames.PCF pcf)
        {
            switch (pcfStatus)
            {
                // Creates a new gameobject to represent the newly created pcf.
                case MLPersistentCoordinateFrames.PCF.Status.Created:
                {
                    #if PLATFORM_LUMIN
                    GameObject pcfVisual = Instantiate(pcfVisualPrefab, pcf.Position, pcf.Rotation, transform);
                    _pcfVisuals.Add(pcfVisual);
                     pcfVisual.SetActive(IsVisualizing);
                    #endif
                   break;
                }
            }
        }

        /// <summary>
        /// Handler for when localization is gained or lost.
        /// Starts and stops the ContinuouslyFindAllPCFs coroutine.
        /// </summary>
        /// <param name="localized"> Map Events that happened. </param>
        private void HandleOnLocalized(bool localized)
        {
            if (localized)
            {
                StartCoroutine("ContinuouslyFindAllPCFs");
            }
            else
            {
                StopCoroutine("ContinuouslyFindAllPCFs");
            }
        }
    }
}

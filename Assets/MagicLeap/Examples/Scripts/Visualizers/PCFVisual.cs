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
using MagicLeap.Core;

namespace MagicLeap
{
    /// <summary>
    /// Class that helps visualize a PCF's pose and the different states a PCF can be in at any given time.
    /// </summary>
    public class PCFVisual : MonoBehaviour
    {
        #if PLATFORM_LUMIN
        /// <summary>
        /// Class used to demonstrate a basic use of the PCF binding interface.
        /// </summary>
        public class TextMeshBinding : MLPersistentCoordinateFrames.PCF.IBinding
        {
            /// <summary>
            /// The Transform that will be manipulated.
            /// </summary>
            private TextMesh _textMesh;

            /// <summary>
            /// The identifier of the binding.
            /// </summary>
            public string Id
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the PCF that the virtual object is bound to.
            /// </summary>
            public MLPersistentCoordinateFrames.PCF PCF
            {
                get;
                private set;
            }

            /// <summary>
            /// Sets the PCF to bind to and the text mesh to refer to when the PCF updates.
            /// Adds this binding to the passed PCF.
            /// </summary>
            public void Bind(MLPersistentCoordinateFrames.PCF pcf, TextMesh textMesh)
            {
                if (pcf != null)
                {
                    PCF = pcf;
                }

                if (textMesh != null)
                {
                    _textMesh = textMesh;
                }

                if (PCF != null && _textMesh != null)
                {
                    _textMesh = textMesh;
                    PCF = pcf;
                    PCF.AddBinding(this);
                    MLPersistentCoordinateFrames.QueueForUpdates(pcf);
                    switch (PCF.CurrentStatus)
                    {
                        case MLPersistentCoordinateFrames.PCF.Status.Created:
                        {
                            _textMesh.text = "<color=green>Created</color>\n" + GetPCFStateString();
                            break;
                        }
                        case MLPersistentCoordinateFrames.PCF.Status.Updated:
                        {
                            Update();
                            break;
                        }
                        case MLPersistentCoordinateFrames.PCF.Status.Regained:
                        {
                            Regain();
                            break;
                        }
                        case MLPersistentCoordinateFrames.PCF.Status.Lost:
                        {
                            Lost();
                            break;
                        }
                    }
                }
            }

            /// <summary>
            /// Removes this binding from the associated PCF.
            /// </summary>
            public void UnBind()
            {
                if (PCF != null)
                {
                    PCF.RemoveBinding(this);
                }
            }

            /// <summary>
            /// Changes the text mesh's text to Updated and displays the PCF's state properites.
            /// </summary>
            public bool Update()
            {
                _textMesh.text = "<color=yellow>Updated</color>\n" + GetPCFStateString();
                return true;
            }

            /// <summary>
            /// Changes the text mesh's text to Regained and displays the PCF's state properites.
            /// </summary>
            public bool Regain()
            {
                _textMesh.text = "<color=cyan>Regained</color>\n" + GetPCFStateString();
                return true;
            }

            /// <summary>
            /// Changes the text mesh's text to Lost and displays the PCF's state properites.
            /// </summary>
            public bool Lost()
            {
                _textMesh.text = "<color=red>Lost</color>\n";
                return true;
            }

            /// <summary>
            /// Used to format pcf properties into the _statusText.
            /// </summary>
            private string GetPCFStateString()
            {
                return string.Format("CFUID: {0}\nType: {1}\nConfidence: {2}\nValidRadiusM: {3}\nRotationErrDeg: {4}\nTranslationErrM: {5}",
                    PCF.CFUID.ToString(),
                    PCF.Type,
                    PCF.Confidence,
                    PCF.ValidRadiusM,
                    PCF.RotationErrDeg,
                    PCF.TranslationErrM);
            }
        }
        #endif

        #if PLATFORM_LUMIN
        private TransformBinding _visualTransformBinding = null;
        private TextMeshBinding _visualTextBinding = null;
        #endif

        #pragma warning disable 414
        [SerializeField, Tooltip("Text to display status")]
        private TextMesh _statusText = null;
        #pragma warning restore 414


        /// <summary>
        /// Starts the MLPersistentCoordinateFrames api and initializes bindings.
        /// </summary>
        void Start()
        {
            #if PLATFORM_LUMIN
            MLResult result = MLPersistentCoordinateFrames.Start();
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: PCFVisual failed starting MLPersistentCoordinateFrames, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }

            MLPersistentCoordinateFrames.FindClosestPCF(transform.position, out MLPersistentCoordinateFrames.PCF pcf);

            _visualTransformBinding = new TransformBinding(isPersistent: false);
            _visualTransformBinding.Bind(pcf, transform);

            _visualTextBinding = new TextMeshBinding();
            _visualTextBinding.Bind(pcf, _statusText);
            #endif
        }

        /// <summary>
        /// Unbinds each binding and stops the MLPersistentCoordinateFrames api.
        /// </summary>
        private void OnDestroy()
        {
            #if PLATFORM_LUMIN
            _visualTransformBinding.UnBind();
            _visualTextBinding.UnBind();

            MLPersistentCoordinateFrames.Stop();
            #endif
        }
    }
}

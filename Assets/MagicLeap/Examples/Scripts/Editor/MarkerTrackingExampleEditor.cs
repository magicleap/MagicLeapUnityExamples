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
using MagicLeap.Examples;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    [CustomEditor(typeof(MarkerTrackingExample))]
    [CanEditMultipleObjects]
    [ExecuteInEditMode]
    public class MarkerTrackingExampleEditor : Editor
    {
        SerializedProperty MarkerTypes;
        SerializedProperty QRCodeSize;
        SerializedProperty ArucoMarkerSize;
        SerializedProperty ArucoDicitonary;
        SerializedProperty TrackerProfile;
        SerializedProperty EnableMarkerScanning;
        SerializedProperty FPSHint;
        SerializedProperty ResolutionHint;
        SerializedProperty CameraHint;
        SerializedProperty FullAnalysisIntervalHint;
        SerializedProperty CornerRefineMethod;
        SerializedProperty UseEdgeRefinement;

        private void OnEnable()
        {
            MarkerTypes = serializedObject.FindProperty("MarkerTypes");
            QRCodeSize = serializedObject.FindProperty("QRCodeSize");
            ArucoMarkerSize = serializedObject.FindProperty("ArucoMarkerSize");
            ArucoDicitonary = serializedObject.FindProperty("ArucoDicitonary");
            TrackerProfile = serializedObject.FindProperty("TrackerProfile");
            EnableMarkerScanning = serializedObject.FindProperty("EnableMarkerScanning");
            FPSHint = serializedObject.FindProperty("FPSHint");
            ResolutionHint = serializedObject.FindProperty("ResolutionHint");
            CameraHint = serializedObject.FindProperty("CameraHint");
            FullAnalysisIntervalHint = serializedObject.FindProperty("FullAnalysisIntervalHint");
            CornerRefineMethod = serializedObject.FindProperty("CornerRefineMethod");
            UseEdgeRefinement = serializedObject.FindProperty("UseEdgeRefinement");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MarkerTrackingExample myTarget = (MarkerTrackingExample)target;
            
            GUILayout.BeginVertical("HelpBox");
            GUILayout.Label("Marker tracker settings:");

            EditorGUILayout.PropertyField(EnableMarkerScanning);
            EditorGUILayout.PropertyField(MarkerTypes);

            if (myTarget.MarkerTypes.HasFlag(MLMarkerTracker.MarkerType.All) ||
                myTarget.MarkerTypes.HasFlag(MLMarkerTracker.MarkerType.QR) ||
                myTarget.MarkerTypes.HasFlag(MLMarkerTracker.MarkerType.EAN_13) ||
                myTarget.MarkerTypes.HasFlag(MLMarkerTracker.MarkerType.UPC_A))
            {
                EditorGUILayout.PropertyField(QRCodeSize);
            }

            if (myTarget.MarkerTypes.HasFlag(MLMarkerTracker.MarkerType.All) ||
                myTarget.MarkerTypes.HasFlag(MLMarkerTracker.MarkerType.Aruco_April))
            {
                EditorGUILayout.PropertyField(ArucoMarkerSize);
                EditorGUILayout.PropertyField(ArucoDicitonary);
            }

            EditorGUILayout.PropertyField(TrackerProfile);

            if (myTarget.TrackerProfile.HasFlag(MLMarkerTracker.Profile.Custom))
            {
                GUILayout.BeginVertical("Custom Profile");
                GUILayout.Label("Custom profile configuration:");
                EditorGUILayout.PropertyField(FPSHint);
                EditorGUILayout.PropertyField(ResolutionHint);
                EditorGUILayout.PropertyField(CameraHint);
                EditorGUILayout.PropertyField(FullAnalysisIntervalHint);
                EditorGUILayout.PropertyField(CornerRefineMethod);
                EditorGUILayout.PropertyField(UseEdgeRefinement);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();



            serializedObject.ApplyModifiedProperties();
        }
    }
}

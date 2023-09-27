using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.MagicLeap;


namespace MagicLeap.Examples
{
    public class AnchorsExample : MonoBehaviour
    {
        public GameObject reticle;
        public Text statusText;
        private MagicLeapInputs magicLeapInputs;
        private MagicLeapInputs.ControllerActions controllerActions;
        private MLAnchors.Request query;
        private Timer localizationInfoUpdateTimer;

        void Start()
        {
#if UNITY_EDITOR
            Debug.LogError("Spatial Anchors is not supported on Application Simulator; please see the Application Simulator documentation for more info.");
            this.enabled = false;
            AnchorVisualizer vis = FindObjectOfType<AnchorVisualizer>();
            if (vis)
                vis.enabled = false;
            return;
#endif
            magicLeapInputs = new MagicLeapInputs();
            magicLeapInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(magicLeapInputs);
            controllerActions.Bumper.performed += HandleOnBumper;
            controllerActions.Trigger.performed += HandleOnTrigger;
            localizationInfoUpdateTimer = new Timer(3);
            query = new MLAnchors.Request();
        }

        private void Update()
        {
            
            if (localizationInfoUpdateTimer.LimitPassed)
            {
                localizationInfoUpdateTimer.Reset();

                // SPATIAL_ANCHOR is a normal permission; we don't request it at runtime - must be included in AndroidManifest.xml
                if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                {
                    MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
                    statusText.text = info.ToString();
                }
                else
                    Debug.LogError($"You must include {MLPermission.SpatialAnchors} in AndroidManifest.xml to run this example");
            }

        }

#if !UNITY_EDITOR
        private void OnDestroy()
        {
            controllerActions.Bumper.performed -= HandleOnBumper;
            controllerActions.Trigger.performed -= HandleOnTrigger;
        }
#endif

        private void HandleOnBumper(InputAction.CallbackContext obj)
        {
            if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
            {
                query.Start(new MLAnchors.Request.Params(controllerActions.Position.ReadValue<Vector3>(), 0, 0, true));
                query.TryGetResult(out MLAnchors.Request.Result result);
                if (result.anchors.Length > 0)
                {
                    var anchor = result.anchors[0];
                    anchor.Delete();
                }
            }
        }

        private void HandleOnTrigger(InputAction.CallbackContext obj)
        {
            if (reticle.activeInHierarchy)
                return;

            float triggerValue = obj.ReadValue<float>();
            if (triggerValue >= 1.0f)
            {
                if (MLPermissions.CheckPermission(MLPermission.SpatialAnchors).IsOk)
                {
                    MLAnchors.Anchor.Create(new Pose(controllerActions.Position.ReadValue<Vector3>(), controllerActions.Rotation.ReadValue<Quaternion>()), 300, out MLAnchors.Anchor anchor);

                    MLAnchors.GetLocalizationInfo(out MLAnchors.LocalizationInfo info);
                    if (info.LocalizationStatus == MLAnchors.LocalizationStatus.Localized)
                    {
                        anchor.Publish();
                    }
                }
            }
        }
    }

}

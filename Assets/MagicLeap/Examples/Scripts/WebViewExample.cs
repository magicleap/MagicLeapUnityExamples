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
using System.Collections;
using System.Text;
using MagicLeap.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class provides examples of how you can use WebView API.
    /// </summary>
    public class WebViewExample : MonoBehaviour
    {
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        [SerializeField, Tooltip("Home page URL address.")]
        private String homeUrl = "https://www.magicleap.com/en-us";

        [SerializeField, Tooltip("Input field with current URL.")]
        private InputField urlBar;

        [SerializeField, Tooltip("Button navigating to home page.")]
        private Button homeButton;

        [SerializeField, Tooltip("Button navigating to previous page.")]
        private Button backButton;

        [SerializeField, Tooltip("Button navigating to next page.")]
        private Button nextButton;

        [SerializeField, Tooltip("Button to pause webview.")]
        private Button pauseButton;

        [SerializeField, Tooltip("Dropdown to determine which pause type to use when pausing.")]
        private Dropdown pauseDropdown;

        [SerializeField, Tooltip("Button to resume webview.")]
        private Button resumeButton;

        [SerializeField, Tooltip("Text on the reset zoom button.")]
        private Text zoomFactorText;

        [SerializeField, Tooltip("WebView Screen Behavior.")]
        private MLWebViewScreenBehavior webViewScreenBehavior;

        [SerializeField, Tooltip("Virtual Keyboard to use for WebView text entry.")]
        private MLVirtualKeyboard virtualKeyboard;

        [SerializeField, Tooltip("Certificate Error Popup")]
        private GameObject certErrorPopup;

        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text statusText = null;

        [SerializeField, Tooltip("WebView tab bar.")]
        private MLWebViewTabBarBehavior tabBar;

        private string loadStatus = "";
        private bool isVirtualKeyboardShown = false;

        private void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
            controllerActions.Bumper.performed += OnBumper;

            tabBar.OnTabCreated += OnTabCreated;

            // WebView is a normal permission, so we don't request at runtime. it should be included in AndroidManifest.xml
            if (MLPermissions.CheckPermission(MLPermission.WebView).IsOk)
            {
                backButton.interactable = false;
                nextButton.interactable = false;

                CreateWebViewWindow();
                
                StartCoroutine(RegisterPopupEventsWhenReady());
            }
            else
            {
                Debug.LogError($"You must include {MLPermission.WebView} in AndroidManifest.xml to run this example");
                enabled = false;
                return;
            }
        }
        
        private IEnumerator RegisterPopupEventsWhenReady()
        {
            yield return new WaitUntil(() => webViewScreenBehavior.WebView != null);
            
            webViewScreenBehavior.WebView.OnPopupOpened += HandleOnPopupOpened;
        }

        private void HandleOnPopupOpened(MLWebView webView, ulong popupID, string url)
        {
            tabBar.CreatePopupTab(webView, popupID, url);
        }
        
        private void Update()
        {
            UpdateStatus();

            if (webViewScreenBehavior.IsConnected)
            {
                if (webViewScreenBehavior.WebView != null)
                {
                    zoomFactorText.text = (webViewScreenBehavior.WebView.GetZoomFactor() * 100) + "%";

                    backButton.interactable = webViewScreenBehavior.WebView.CanGoBack();
                    nextButton.interactable = webViewScreenBehavior.WebView.CanGoForward();
                }
            }
        }

        private void UpdateStatus()
        {
            statusText.text = $"<color=#B7B7B8><b>Web View Data</b></color>\n";
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append($"Scrolling Mode: <i>{webViewScreenBehavior.scrollingMode.ToString()}</i>\n\n");
            strBuilder.Append($"Load Status: <i>{loadStatus}</i>\n");
            statusText.text += strBuilder.ToString();
        }

        private void OnBumper(InputAction.CallbackContext obj)
        {
            if (webViewScreenBehavior.scrollingMode == MLWebViewScreenBehavior.ScrollingMode.Touchpad)
            {
                webViewScreenBehavior.scrollingMode = MLWebViewScreenBehavior.ScrollingMode.TriggerDrag;
            }
            else
            {
                webViewScreenBehavior.scrollingMode = MLWebViewScreenBehavior.ScrollingMode.Touchpad;
            }
        }

        private void OnTabCreated(MLWebViewTabBehavior tab, string url = null)
        {
            tab.WebView.OnLoadEnded += OnLoadEnded;
            tab.WebView.OnErrorLoaded += OnErrorLoaded;
            tab.WebView.OnCertificateErrorLoaded += OnCertificateErrorLoaded;
            tab.WebView.OnKeyboardShown += OnKeyboardShown;
            tab.WebView.OnKeyboardDismissed += OnKeyboardDismissed;

            tab.OnTabSelected += TabOnOnTabSelected;
            
            tab.GoToUrl(url ?? homeUrl);
        }

        private void TabOnOnTabSelected(MLWebViewTabBehavior obj)
        {
            UpdateVisuals();
        }

        private void OnTabDestroyed(MLWebViewTabBehavior tab)
        {
            tab.WebView.OnLoadEnded -= OnLoadEnded;
            tab.WebView.OnErrorLoaded -= OnErrorLoaded;
            tab.WebView.OnCertificateErrorLoaded -= OnCertificateErrorLoaded;
            tab.WebView.OnKeyboardShown -= OnKeyboardShown;
            tab.WebView.OnKeyboardDismissed -= OnKeyboardDismissed;
            
            tab.OnTabSelected -= TabOnOnTabSelected;
        }

        private void OnLoadEnded(MLWebView webView, bool isMainFrame, int httpStatusCode)
        {
            // make sure the next time a page is loaded that it doesn't ignore cert errors
            webView.IgnoreCertificateError = false;
            loadStatus = $"Success - {httpStatusCode.ToString()}";
        }

        private void OnErrorLoaded(MLWebView webView, bool isMainFrame, int httpStatusCode, string errorStr, string failedUrl)
        {
            loadStatus = $"Failed - {httpStatusCode.ToString()} - {errorStr}";
        }

        private void OnCertificateErrorLoaded(MLWebView webView, int errorCode, string url, string errorMessage, string details, bool certificateErrorIgnored)
        {
            if (!certificateErrorIgnored)
            {
                if (certErrorPopup != null)
                {
                    certErrorPopup.SetActive(true);
                }
            }

            loadStatus = $"Cert Error - {errorCode.ToString()} - {errorMessage}";
        }

        private void OnKeyboardShown(MLWebView webView, MLWebView.InputFieldData keyboardShowData)
        {
            if (virtualKeyboard != null)
            {
                if (!isVirtualKeyboardShown)
                {
                    virtualKeyboard.OnCharacterAdded.AddListener(OnCharacterAdded);
                    virtualKeyboard.OnCharacterDeleted.AddListener(OnCharacterDeleted);
                    isVirtualKeyboardShown = true;
                }
                virtualKeyboard.Open();
            }
        }

        private void OnKeyboardDismissed(MLWebView webView)
        {
            if (virtualKeyboard != null)
            {
                virtualKeyboard.Cancel();
            }
        }

        private void OnCharacterAdded(char character)
        {
            webViewScreenBehavior.WebView?.InjectChar(character);
        }

        private void OnCharacterDeleted()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                webViewScreenBehavior.WebView.InjectKeyDown(MLWebView.KeyCode.Delete, (uint)MLWebView.EventFlags.None);
                webViewScreenBehavior.WebView.InjectKeyUp(MLWebView.KeyCode.Delete, (uint)MLWebView.EventFlags.None);
            }
        }

        private void CreateWebViewWindow()
        {
            if (!webViewScreenBehavior.CreateWebViewWindow())
            {
                Debug.LogError("Failed to create web view window");
            }
            else
            {
                tabBar.CreateTab();
            }
        }

        /// <summary>
        /// Change current web page to the provided Url address.
        /// </summary>
        public void GoToUrl(String url)
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.GoTo(url).IsOk)
                {
                    Debug.LogError("Failed to navigate to url " + url);
                }
            }
        }

        /// <summary>
        /// Load the home page.
        /// This is not a WebView API concept but a normal browser convention
        /// </summary>
        public void HomePage()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.GoTo(homeUrl).IsOk)
                {
                    Debug.LogError("Failed to load home page URL");
                }
            }
        }

        /// <summary>
        /// Reloads current web page.
        /// </summary>
        public void ReloadPage(bool ignoreCertificateError)
        {
            if (webViewScreenBehavior.WebView != null)
            {
                webViewScreenBehavior.WebView.IgnoreCertificateError = ignoreCertificateError;
                if (!webViewScreenBehavior.WebView.Reload().IsOk)
                {
                    Debug.LogError("Failed to reload current URL");
                }
            }
        }

        /// <summary>
        /// Change current web page to next one (if exists in history).
        /// </summary>
        public void NextPage()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.GoForward().IsOk)
                {
                    Debug.LogError("Failed to navigate forward");
                }
            }
            
            UpdateVisuals();
        }

        /// <summary>
        /// Change current web page to previous one (if exists in history).
        /// </summary>
        public void PreviousPage()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.GoBack().IsOk)
                {
                    Debug.LogError("Failed to navigate back");
                }
            }
            
            UpdateVisuals();
        }

        /// <summary>
        /// Zooms In the web page
        /// </summary>
        public void ZoomIn()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.ZoomIn().IsOk)
                {
                    Debug.LogError("Failed to zoom in");
                }
            }
        }

        /// <summary>
        /// Zooms out the web page
        /// </summary>
        public void ZoomOut()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.ZoomOut().IsOk)
                {
                    Debug.LogError("Failed to zoom out");
                }
            }
        }

        /// <summary>
        /// Resets zoom on the web page.
        /// </summary>
        public void ResetZoom()
        {
            if (webViewScreenBehavior.WebView != null)
            {
                if (!webViewScreenBehavior.WebView.ResetZoom().IsOk)
                {
                    Debug.LogError("Failed to reset zoom to 100%");
                }
            }
        }

        public void CloseCertErrorPopup()
        {
            if (certErrorPopup != null)
            {
                certErrorPopup.SetActive(false);
            }
        }

        public void Pause()
        {
            var pauseType = (MLWebView.PauseType)pauseDropdown.value;
            if (!webViewScreenBehavior.WebView.Pause(pauseType).IsOk)
            {
                Debug.LogError("Failed to pause");
            }
            else
            {
                tabBar.currentTab.Pause();
            }
            
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            pauseButton.gameObject.SetActive(!tabBar.currentTab.IsPaused);
            pauseDropdown.gameObject.SetActive(!tabBar.currentTab.IsPaused);
            resumeButton.gameObject.SetActive(tabBar.currentTab.IsPaused);
        }

        public void Resume()
        {
            if (!webViewScreenBehavior.WebView.Resume().IsOk)
            {
                Debug.LogError("Failed to resume");
            }
            else
            {
                tabBar.currentTab.Resume();
            }

            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (virtualKeyboard != null && isVirtualKeyboardShown)
            {
                virtualKeyboard.OnCharacterAdded.RemoveListener(OnCharacterAdded);
                virtualKeyboard.OnCharacterDeleted.RemoveListener(OnCharacterDeleted);
            }
            
            if (webViewScreenBehavior.WebView != null)
            {
                webViewScreenBehavior.WebView.OnPopupOpened -= HandleOnPopupOpened;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class VoiceIntentsExample : MonoBehaviour
{
    [SerializeField, Tooltip("The text used to display status information for the example.")]
    private Text _statusText = null;

    [SerializeField, Tooltip("The configuration file that holds the list of intents used for this application.")]
    private MLVoiceIntentsConfiguration voiceConfiguration;

    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions controllerActions;

    private string startupStatus = "Requesting Permission...";
    private string lastResults = "";
    private bool isProcessing = false;

    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
    }

    // Start is called before the first frame update
    private void Start()
    {
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

        MLPermissions.RequestPermission(MLPermission.VoiceInput, permissionCallbacks);
    }

    private void Initialize()
    {
        bool isEnabled = MLVoice.VoiceEnabled;
        startupStatus = "System Supports Voice Intents: "+ isEnabled.ToString();

        if (isEnabled)
        {
            MLResult result = MLVoice.SetupVoiceIntents(voiceConfiguration);

            if (result.IsOk)
            {
                controllerActions.Bumper.performed += HandleOnBumper;
                isProcessing = true;
                startupStatus += "\n\n<color=#dbfb76><b>List of Voice Intents:</b></color>";

                MLVoice.OnVoiceEvent += VoiceEvent;

                List<string> allValues = voiceConfiguration.GetValues();

                foreach (string value in allValues)
                {
                    startupStatus += "\n" + value;
                }
            }
            else
            {
                startupStatus += "\nSetup failed with result: " + result.ToString();
                Debug.LogError("Failed to Setup Voice Intents with result: " + result);
            }
        }
    }

    private void OnDestroy()
    {
        MLVoice.OnVoiceEvent -= VoiceEvent;
        controllerActions.Bumper.performed -= HandleOnBumper;

        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        _statusText.text = $"<color=#dbfb76><b>Voice Intents Data</b></color>\n{startupStatus}";
        _statusText.text += "\n\nIs Processing: " + isProcessing;
        _statusText.text += lastResults;
    }

    void VoiceEvent(in bool wasSuccessful, in MLVoice.IntentEvent voiceEvent)
    {
        StringBuilder strBuilder = new StringBuilder();
        strBuilder.Append($"\n\n<color=#dbfb76><b>Last Voice Event:</b></color>\n");
        strBuilder.Append($"Was Successful: <i>{wasSuccessful}</i>\n");
        strBuilder.Append($"State: <i>{voiceEvent.State}</i>\n");
        strBuilder.Append($"No Intent Reason\n(Expected NoReason): \n<i>{voiceEvent.NoIntentReason}</i>\n");
        strBuilder.Append($"Event Unique Name:\n<i>{voiceEvent.EventName}</i>\n");
        strBuilder.Append($"Event Unique Id: <i>{voiceEvent.EventID}</i>\n");

        lastResults = strBuilder.ToString();
    }

    private void HandleOnBumper(InputAction.CallbackContext obj)
    {
        bool bumperDown = obj.ReadValueAsButton();

        if (bumperDown)
        {
            MLResult result;
            if (isProcessing)
            {  
                result = MLVoice.Stop();
                if(result.IsOk)
                {
                    isProcessing = false;
                }
                else
                {
                    Debug.LogError("Failed to Stop Processing Voice Intents with result: " + result);
                }
            }
            else
            {
                result = MLVoice.SetupVoiceIntents(voiceConfiguration);
                if (result.IsOk)
                {
                    isProcessing = true;
                }
                else
                {
                    Debug.LogError("Failed to Re-Setup Voice Intents with result: " + result);
                }
            }
            
        }
    }

    private void OnPermissionDenied(string permission)
    {
        startupStatus = "<color=#ff0000><b>Permission Denied!</b></color>";
    }

    private void OnPermissionGranted(string permission)
    {
        Initialize();
    }
}

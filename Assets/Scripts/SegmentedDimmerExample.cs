using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using UnityEngine.XR.OpenXR.NativeTypes;

public class SegmentedDimmerExample : MonoBehaviour
{
    [SerializeField]
    private Text status;

    [SerializeField]
    private Dropdown blendModeSelection;

    [Header("Objects")] [SerializeField] private List<MeshRenderer> testObjects = new();
    [SerializeField] private float colorUpdateInterval = 2.5f;
    
    private XrEnvironmentBlendMode currentBlendMode;
    private IEnumerable<Material> materials;
    private bool dimmerEnabled = false;
    private MagicLeapRenderingExtensionsFeature rendering;
    
    private void Start()
    {
        materials = testObjects.Select(to => to.material);

        rendering = OpenXRSettings.Instance.GetFeature<MagicLeapRenderingExtensionsFeature>();

        currentBlendMode = rendering.BlendMode;

        blendModeSelection.value = ((int)currentBlendMode) - 2;
        blendModeSelection.onValueChanged.AddListener(OnBlendModeSelectionChange);
        dimmerEnabled = CheckDimmerSetting();

        UpdateStatusText();
        
        StartCoroutine(UpdateColors());
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            dimmerEnabled = CheckDimmerSetting();
            UpdateStatusText();
        }
    }

    private void OnBlendModeSelectionChange(int blendModeIndex)
    {
        var selectedBlendMode = (XrEnvironmentBlendMode)(blendModeIndex + 2);
        Debug.Log($"setting XrEnvironmentBlendMode to {selectedBlendMode}");
        rendering.BlendMode = selectedBlendMode;
        currentBlendMode = rendering.BlendMode;
        UpdateStatusText();
    }


    private void OnDestroy()
    {
        blendModeSelection.onValueChanged.RemoveAllListeners();
        rendering.BlendMode = XrEnvironmentBlendMode.Additive;
    }

    private IEnumerator UpdateColors()
    {
        while (true)
        {
            yield return new WaitForSeconds(colorUpdateInterval);
            foreach (var mat in materials)
            {
                mat.color = Random.ColorHSV(0f, 1, 1f, 1f, 1f, 1f, .01f, 1f);
            }
        }
    }

    private void UpdateStatusText()
    {
        string vis = currentBlendMode == XrEnvironmentBlendMode.AlphaBlend && dimmerEnabled ? "VISIBLE" : "NOT VISIBLE";
        status.text = $"Segmented Dimmer Enabled:\n<B>{dimmerEnabled}</B>\n\n" +
                      $"Current blend mode:\n<B>{currentBlendMode}</B>\n\n" +
                      $"Segmented Dimmer:\n<B>{vis}</B>";
    }

    public bool CheckDimmerSetting()
    {
#if !UNITY_EDITOR
        // Get context
        using (var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");

            var dimmerMode = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "is_segmented_dimmer_enabled");

            Debug.Log("Dimmer Mode is set to : " + dimmerMode);
            return dimmerMode > 0;
        }
#else
        return true;
#endif
    }
}

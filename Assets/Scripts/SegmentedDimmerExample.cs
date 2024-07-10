using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.NativeTypes;
using MagicLeap.OpenXR.Features;

public class SegmentedDimmerExample : MonoBehaviour
{
    [SerializeField]
    private Text status;

    [SerializeField]
    private Dropdown blendModeSelection;

    [Header("Objects")] [SerializeField] private List<MeshRenderer> testObjects = new();
    [SerializeField] private float colorUpdateInterval = 2.5f;
    
    private XrEnvironmentBlendMode currentBlendMode;
    private XrEnvironmentBlendMode defaultBlendMode;
    private IEnumerable<Material> materials;
    private bool dimmerEnabled = false;
    private MagicLeapRenderingExtensionsFeature rendering;
    
    private void Start()
    {
        materials = testObjects.Select(to => to.material);

        rendering = OpenXRSettings.Instance.GetFeature<MagicLeapRenderingExtensionsFeature>();
        rendering.BlendMode = XrEnvironmentBlendMode.AlphaBlend;
        defaultBlendMode = currentBlendMode = rendering.BlendMode;

        blendModeSelection.value = ((int)currentBlendMode) - 2;
        blendModeSelection.onValueChanged.AddListener(OnBlendModeSelectionChange);
        dimmerEnabled = IsDimmerEnabledInSettings();

        UpdateStatusText();
        
        StartCoroutine(UpdateColors());
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            dimmerEnabled = IsDimmerEnabledInSettings();
            UpdateStatusText();
        }
    }

    private void OnBlendModeSelectionChange(int blendModeIndex)
    {
        var selectedBlendMode = (XrEnvironmentBlendMode)(blendModeIndex + 2);
        rendering.BlendMode = selectedBlendMode;
        currentBlendMode = rendering.BlendMode;
        UpdateStatusText();
    }


    private void OnDestroy()
    {
        blendModeSelection.onValueChanged.RemoveAllListeners();
        rendering.BlendMode = defaultBlendMode;
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

    public bool IsDimmerEnabledInSettings()
    {
        if (!Application.isEditor)
        {
            // Get context
            using var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
            var systemGlobal = new AndroidJavaClass("android.provider.Settings$System");

            var dimmerMode = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "is_segmented_dimmer_enabled");

            return dimmerMode > 0;
        }
        return false;
    }
}

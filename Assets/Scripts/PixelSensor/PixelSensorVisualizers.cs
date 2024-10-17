using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PixelSensorVisualizers : MonoBehaviour
{
    [Serializable]
    private class VisualizerConfig
    {
        public GameObject configObject;
        [HideInInspector] public List<PixelSensorVisualizer> visualizers = new();
        public void Init()
        {
            visualizers = configObject.GetComponentsInChildren<PixelSensorVisualizer>(includeInactive:true).ToList();
        }
    }
    
    [SerializeField] private List<VisualizerConfig> visualizerConfigs = new();


    private VisualizerConfig activeVisualizerConfig;
    public IReadOnlyList<PixelSensorVisualizer> ActiveVisualizers => activeVisualizerConfig?.visualizers ?? new List<PixelSensorVisualizer>();

    private void Awake()
    {
        foreach (var config in visualizerConfigs)
        {
            config.Init();
        }
    }

    public void Reset()
    {
        foreach (var config in visualizerConfigs)
        {
            config.configObject.SetActive(false);
            config.visualizers.ForEach(v => v.Reset());
        }
    }

    public void SetCount(int count)
    {
        var result = visualizerConfigs[0];
        foreach (var config in visualizerConfigs)
        {
            config.configObject.SetActive(false);
            if (config.visualizers.Count == count)
            {
                result = config;
            }
        }
        activeVisualizerConfig =  result;
    }

    public void EnableVisualizer(bool enable)
    {
        activeVisualizerConfig?.configObject.SetActive(enable);
    }
}

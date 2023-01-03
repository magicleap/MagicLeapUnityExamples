using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class DepthCameraExample : MonoBehaviour
{
    private readonly MLPermissions.Callbacks permissionCallbacks = new();

    private bool permissionGranted;
    private bool isFrameAvailable = false;
    private MLDepthCamera depthCamera = null;
    private MLDepthCamera.Data lastData = null;

    /// <summary>
    /// Flags used along with the depth mode. Currently, we limit the flag to only one instead of combinations but that is possible. 
    /// </summary>
    [SerializeField, Tooltip("Dropdown for selecting depth camera flags.")]
    private EnumDropdown captureFlagsDropdown;

    [SerializeField, Tooltip("Dropdown for selecting depth camera mode.")]
    private EnumDropdown cameraModeDropdown;


    [SerializeField]
    private GameObject depthImgMin;

    [SerializeField]
    private GameObject depthImgMax;

    [SerializeField]
    private GameObject ambientDepthMin;

    [SerializeField]
    private GameObject ambientDepthMax;

    [SerializeField]
    private GameObject confidenceMin;
    [SerializeField]
    private GameObject confidenceMax;


    private float depthImgMinDist;
    private float depthImgMaxDist;

    private float ambientRawImgMinDist;
    private float ambientRawImgMaxDist;

    private float confidenceMinDist;
    private float confidenceMaxDist;

    [SerializeField, Tooltip("Timeout in milliseconds for data retrieval.")]
    private ulong timeout = 0;

    [SerializeField]
    private Renderer imgRenderer;
    [SerializeField]
    private Renderer confidenceRenderer;

    [SerializeField]
    private Text statusOutput;


    private Texture2D ImageTexture = null;

    private readonly int minDepthMatPropId = Shader.PropertyToID("_MinDepth");
    private readonly int maxDepthMatPropId = Shader.PropertyToID("_MaxDepth");
    private readonly int mapTexMatPropId = Shader.PropertyToID("_MapTex");

    private Vector2 scale = new Vector2(1.0f, -1.0f);


    private MLDepthCamera.Mode mode = MLDepthCamera.Mode.LongRange;
    private MLDepthCamera.CaptureFlags captureFlag = MLDepthCamera.CaptureFlags.DepthImage;

    void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

        cameraModeDropdown.AddOptions(
            MLDepthCamera.Mode.LongRange
        );

        captureFlagsDropdown.AddOptions(
            MLDepthCamera.CaptureFlags.DepthImage,
            MLDepthCamera.CaptureFlags.Confidence,
            MLDepthCamera.CaptureFlags.AmbientRawDepthImage
        );
    }

    void Start()
    {
        var settings = new MLDepthCamera.Settings()
        {
            Mode = mode,
            Flags = captureFlag
        };
        depthCamera = new MLDepthCamera(settings);

        MLPermissions.RequestPermission(MLPermission.DepthCamera, permissionCallbacks);
    }

    private void OnEnable()
    {
        captureFlagsDropdown.onValueChanged.AddListener(UpdateUI);
    }

    void Update()
    {
        if (!permissionGranted || depthCamera == null || !depthCamera.IsConnected)
        {
            return;
        }

        var result = depthCamera.GetLatestDepthData(timeout, out MLDepthCamera.Data data);
        isFrameAvailable = result.IsOk;
        if (result.IsOk)
        {
            lastData = data;
        }

        switch (captureFlag)
        {
            case MLDepthCamera.CaptureFlags.AmbientRawDepthImage:
                if (lastData.AmbientRawDepthImage != null)
                {
                    CheckAndCreateTexture((int)lastData.AmbientRawDepthImage.Value.Width, (int)lastData.AmbientRawDepthImage.Value.Height);

                    ambientRawImgMinDist = ambientDepthMin.GetComponentInChildren<Slider>().value;
                    ambientRawImgMaxDist = ambientDepthMax.GetComponentInChildren<Slider>().value;

                    AdjustRendererFloats(imgRenderer, ambientRawImgMinDist, ambientRawImgMaxDist);
                    ImageTexture.LoadRawTextureData(lastData.AmbientRawDepthImage.Value.Data);
                    ImageTexture.Apply();
                }
                break;
            case MLDepthCamera.CaptureFlags.DepthImage:
                if (lastData.DepthImage != null)
                {
                    CheckAndCreateTexture((int)lastData.DepthImage.Value.Width, (int)lastData.DepthImage.Value.Height);

                    depthImgMinDist = depthImgMin.GetComponentInChildren<Slider>().value;
                    depthImgMaxDist = depthImgMax.GetComponentInChildren<Slider>().value;

                    AdjustRendererFloats(imgRenderer, depthImgMinDist, depthImgMaxDist);
                    ImageTexture.LoadRawTextureData(lastData.DepthImage.Value.Data);
                    ImageTexture.Apply();
                }
                break;
            case MLDepthCamera.CaptureFlags.Confidence:
                if (lastData.ConfidenceBuffer != null)
                {
                    CheckAndCreateTexture((int)lastData.ConfidenceBuffer.Value.Width, (int)lastData.ConfidenceBuffer.Value.Height);

                    confidenceMinDist = confidenceMin.GetComponentInChildren<Slider>().value;
                    confidenceMaxDist = confidenceMax.GetComponentInChildren<Slider>().value;

                    AdjustRendererFloats(confidenceRenderer, confidenceMinDist, confidenceMaxDist);

                    confidenceRenderer.material.SetTexture(mapTexMatPropId, ImageTexture);
                    ImageTexture.LoadRawTextureData(lastData.ConfidenceBuffer.Value.Data);
                    ImageTexture.Apply();
                }
                break;
        }
    }

    private void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
        DisonnectCamera();
    }

    private void OnPermissionDenied(string permission)
    {
        if (permission == MLPermission.Camera)
        {
            MLPluginLog.Error($"{permission} denied, example won't function.");
        }
        else if (permission == MLPermission.DepthCamera)
        {
            MLPluginLog.Error($"{permission} denied, example won't function.");
        }
    }

    private void OnPermissionGranted(string permission)
    {
        MLPluginLog.Debug($"Granted {permission}.");
        permissionGranted = true;
        ConnectCamera();
        UpdateUI(0);
    }

    private void ConnectCamera()
    {
        var result = depthCamera.Connect();
        if (result.IsOk && depthCamera.IsConnected)
        {
            Debug.Log($"Connected to new depth camera with mode = {depthCamera.CurrentSettings.Mode} and flags = {depthCamera.CurrentSettings.Flags}");
        }
        else
        {
            Debug.LogError($"Failed to connect to camera: {result.Result}");
        }
    }

    private void DisonnectCamera()
    {
        var result = depthCamera.Disconnect();
        if (result.IsOk && !depthCamera.IsConnected)
        {
            Debug.Log($"Disconnected depth camera with mode = {depthCamera.CurrentSettings.Mode} and flags = {depthCamera.CurrentSettings.Flags}");
        }
        else
        {
            Debug.LogError($"Failed to disconnect to camera: {result.Result}");
        }
    }

    void UpdateUI(int _)
    {

        mode = cameraModeDropdown.GetSelected<MLDepthCamera.Mode>();
        captureFlag = captureFlagsDropdown.GetSelected<MLDepthCamera.CaptureFlags>();

        bool showAmbientSlider = false;
        bool showDepthImgSlider = false;
        bool showConfidenceSlider = false;
        bool updateSliders = false;
        switch (captureFlag)
        {
            case MLDepthCamera.CaptureFlags.DepthImage:
                showDepthImgSlider = true;
                if (!depthImgMin.activeSelf)
                    updateSliders = true;
                break;
            case MLDepthCamera.CaptureFlags.AmbientRawDepthImage:
                showAmbientSlider = true;
                if (!ambientDepthMin.activeSelf)
                    updateSliders = true;
                break;
            case MLDepthCamera.CaptureFlags.Confidence:
                showConfidenceSlider = true;
                if (!confidenceMin.activeSelf)
                    updateSliders = true;
                break;
        }

        if (updateSliders)
        {
            ambientDepthMax.SetActive(showAmbientSlider);
            ambientDepthMin.SetActive(showAmbientSlider);
            depthImgMin.SetActive(showDepthImgSlider);
            depthImgMax.SetActive(showDepthImgSlider);
            confidenceMin.SetActive(showConfidenceSlider);
            confidenceMax.SetActive(showConfidenceSlider);
            confidenceRenderer.enabled = showConfidenceSlider;
            imgRenderer.enabled = showDepthImgSlider || showAmbientSlider;
            ImageTexture = null;
        }

        UpdateSettings();
    }

    private void CheckAndCreateTexture(int width, int height)
    {
        if (ImageTexture == null || ImageTexture.width != width || ImageTexture.height != height)
        {
            ImageTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
            ImageTexture.filterMode = FilterMode.Bilinear;
            var material = confidenceRenderer.enabled ? confidenceRenderer.material : imgRenderer.material;
            material.mainTexture = ImageTexture;
            material.mainTextureScale = scale;
        }
    }

    private void AdjustRendererFloats(Renderer renderer, float minValue, float maxValue)
    {
        renderer.material.SetFloat(minDepthMatPropId, minValue);
        renderer.material.SetFloat(maxDepthMatPropId, maxValue);
        renderer.material.SetTextureScale(mapTexMatPropId, scale);
    }

    private void UpdateSettings()
    {
        var settings = new MLDepthCamera.Settings()
        {
            Mode = mode,
            Flags = captureFlag
        };

        depthCamera.UpdateSettings(settings);
    }
}

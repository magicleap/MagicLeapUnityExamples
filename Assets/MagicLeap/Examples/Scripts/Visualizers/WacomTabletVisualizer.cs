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
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class demonstrates how to visualize information from the MLInput Tablet API.
    /// Once connected, the tablet will display which buttons are being pressed and the
    /// pen will respond by indicating position and pressure on the tablet. As you begin
    /// to draw the line thickness will change based on applied pressure and the color will
    /// reflect the selection from the color wheel.
    /// </summary>
    public class WacomTabletVisualizer : MonoBehaviour
    {
        // The canvas drawing resolution.
        private const int RESOLUTION = 512;

        // The degrees of the touch ring.
        private const float TOUCHRING_DEGREES = 360;

        // The retuned value for the touch ring is between (0 - 71).
        // In order to reach 360, it must be multiplied by 5.
        private const int TOUCHRING_MULTIPLIER = 5;

        // The default drawing size of the brush.
        private const float BRUSH_SIZE = 0.1f;

        [Header("Tablet")]

        [SerializeField, Tooltip("The default material to apply to the tablet.")]
        private Material _defaultMaterial = null;

        [SerializeField, Tooltip("The active material to apply to the tablet.")]
        private Material _activeMaterial = null;

        [SerializeField, Tooltip("An array of tablet buttons linked to their appropriate GameObject.")]
        private DeviceButton[] _buttonAssignment = null;

        [SerializeField, Tooltip("The transform of the touchpad arrow.")]
        private Transform _touchpadArrow = null;

        [Header("Pen")]

        [SerializeField, Tooltip("The transform of the pen.")]
        private Transform _pen = null;

        [SerializeField, Tooltip("The brush texture that is used for drawing on the canvas.")]
        public Texture2D _brushTexture = null;

        [SerializeField, Tooltip("The material that is used for drawing on the canvas.")]
        private Material _brushMaterial = null;

        [SerializeField, Tooltip("The material that is used for erasing on the canvas.")]
        private Material _eraseMaterial = null;

        [SerializeField, Tooltip("The canvas renderer that will be used for drawing.")]
        private Renderer _canvasRenderer = null;

        [Header("Color Wheel")]

        [SerializeField, Tooltip("The color wheel that will display the available colors.")]
        private GameObject _colorWheel = null;

        [SerializeField, Tooltip("An array of available colors.")]
        private Color[] _colors = null;

        // A dictionary reference to the device buttons.
        private Dictionary<MLInput.TabletDeviceButton, List<Renderer>> _deviceButtons;

        // The material applied to the touch ring indicator.
        private Material _touchpadArrowMaterial = null;

        // Used for canvas hit detection and drawing.
        private RaycastHit _pixelHit;
        private Vector2 _pixelPoint = Vector2.zero;
        private RenderTexture _canvas = null;

        // Information about the last known state of the device, pen, and buttons.
        public Vector3 LastPositionAndForce
        {
            get;
            private set;
        }
        public float LastDistance
        {
            get;
            private set;
        }
        public Vector2 LastTilt
        {
            get;
            private set;
        }
        public bool LastIsTouching
        {
            get;
            private set;
        }
        public int LastTouchRing
        {
            get;
            private set;
        }

        public MLInput.TabletDeviceToolType LastToolType
        {
            get;
            private set;
        }
        public MLInput.TabletDeviceButton LastButton
        {
            get;
            private set;
        }

        public bool LastButtonState
        {
            get;
            private set;
        }
        public bool ButtonErase
        {
            get;
            private set;
        }
        public bool Connected
        {
            get;
            private set;
        }

        // Used to store a button references to a GameObject.
        [System.Serializable]
        private struct DeviceButton
        {
            [SerializeField, Tooltip("The MLInputDeviceButton being referenced.")]
            private MLInput.TabletDeviceButton _button;

            [SerializeField, Tooltip("A list of Renderers for the MLInputDeviceButton.")]
            private List<Renderer> _renderers;

            /// <summary>
            /// The MLInputDeviceButton being referenced.
            /// </summary>
            public MLInput.TabletDeviceButton Button
            {
                get { return _button; }
                private set { _button = value; }
            }

            /// <summary>
            /// A list of Renderers for the MLInputDeviceButton.
            /// </summary>
            public List<Renderer> Renderers
            {
                get { return _renderers; }
                private set { _renderers = value; }
            }
        }

        private void Awake()
        {
            #if PLATFORM_LUMIN
            MLInput.OnTabletConnected += HandleOnTabletConnect;
            #endif
        }
        /// <summary>
        /// Validates fields, sets up the canvas, and registers for the MLInput tablet callbacks.
        /// </summary>
        void Start()
        {
            // Disable and exit early, if there was an issue.
            if (!Initialization())
            {
                enabled = false;
                return;
            }

            // Initialize the drawing canvas and on-screen information.
            SetupCanvas();
            UpdateColor();

            #if PLATFORM_LUMIN
            // Register for callbacks.
            MLInput.OnTabletDisconnected += HandleOnTabletDisconnect;
            MLInput.OnTabletButtonUp += HandleOnTabletButtonUp;
            MLInput.OnTabletButtonDown += HandleOnTabletButtonDown;
            MLInput.OnTabletPenTouch += HandleOnTabletPenTouch;
            MLInput.OnTabletRingTouch += HandleOnTabletRingTouch;
            #endif
        }

        void OnDestroy()
        {
            #if PLATFORM_LUMIN
            // Un-register for callbacks.
            MLInput.OnTabletConnected -= HandleOnTabletConnect;
            MLInput.OnTabletDisconnected -= HandleOnTabletDisconnect;
            MLInput.OnTabletButtonUp -= HandleOnTabletButtonUp;
            MLInput.OnTabletButtonDown -= HandleOnTabletButtonDown;
            MLInput.OnTabletPenTouch -= HandleOnTabletPenTouch;
            MLInput.OnTabletRingTouch -= HandleOnTabletRingTouch;
            // Stop the input service.
            MLInput.Stop();
            #endif
        }

        /// <summary>
        /// Validates fields and starts MLInput.
        /// </summary>
        private bool Initialization()
        {
            if (_defaultMaterial == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._defaultMaterial is not set, disabling script.");
                return false;
            }

            if (_activeMaterial == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._activeMaterial is not set, disabling script.");
                return false;
            }

            if (_buttonAssignment == null || _buttonAssignment.Length == 0)
            {
                Debug.LogError("Error: WacomTabletVisualizer._buttonAssignment is not set, disabling script.");
                return false;
            }

            if (_touchpadArrow == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._touchpadArrow is not set, disabling script.");
                return false;
            }

            // Obtain the reference for the touchpad arrow renderer.
            Renderer touchpadArrowRenderer = _touchpadArrow.GetComponent<Renderer>();
            if (touchpadArrowRenderer == null)
            {
                Debug.LogErrorFormat("Error: WacomTabletVisualizer._touchpadArrow does not have a renderer, disabling script.");
                return false;
            }

            // Set the touchpad arrow material reference.
            _touchpadArrowMaterial = touchpadArrowRenderer.material;

            if (_pen == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._pen is not set, disabling script.");
                return false;
            }

            if (_brushTexture == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._brushTexture is not set, disabling script.");
                return false;
            }

            if (_brushMaterial == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._brushMaterial is not set, disabling script.");
                return false;
            }

            if (_eraseMaterial == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._eraseMaterial is not set, disabling script.");
                return false;
            }

            if (_canvasRenderer == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._canvasRenderer is not set, disabling script.");
                return false;
            }

            if (_colorWheel == null)
            {
                Debug.LogError("Error: WacomTabletVisualizer._colorWheel is not set, disabling script.");
                return false;
            }

            if (_colors == null || _colors.Length == 0)
            {
                Debug.LogError("Error: WacomTabletVisualizer._colors is not set, disabling script.");
                return false;
            }

            #if PLATFORM_LUMIN
            // Attempt to start the input service.
            MLResult result = MLInput.Start(new MLInput.Configuration(false));
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: WacomTabletVisualizer failed starting MLInput, disabling script. Reason: {0}", result);
                return false;
            }
            #endif

            // Initialize and create the button dictionary.
            _deviceButtons = new Dictionary<MLInput.TabletDeviceButton, List<Renderer>>();
            for (int i = 0; i < _buttonAssignment.Length; i++)
            {
                // Only allow adding a key to the dictionary, if it doesn't exist.
                if (!_deviceButtons.ContainsKey(_buttonAssignment[i].Button))
                {
                    _deviceButtons.Add(_buttonAssignment[i].Button, _buttonAssignment[i].Renderers);
                }
            }

            return true;
        }

        /// <summary>
        /// Initializes the canvas.
        /// </summary>
        private void SetupCanvas()
        {
            Texture2D canvasMask = new Texture2D(1, 1);
            canvasMask.SetPixel(0, 0, Color.black);
            canvasMask.Apply();

            _canvas = new RenderTexture(RESOLUTION, RESOLUTION, 24, RenderTextureFormat.ARGB32);
            Graphics.Blit(canvasMask, _canvas);

            _canvasRenderer.material.SetTexture("_Pen", _canvas);
        }

        /// <summary>
        /// Erases the entire canvas.
        /// </summary>
        private void ClearCanvas()
        {
            RenderTexture.active = _canvas;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        /// <summary>
        /// Applies the texture to the canvas at the given location and applied pressure.
        /// </summary>
        /// <param name="rt">The render texture to apply the texture.</param>
        /// <param name="x">X Location</param>
        /// <param name="y">Y Location</param>
        /// <param name="pressure">Applied Pressure (0.0f - 1.0f)</param>
        /// <param name="erase">When true, erase will occur instead of drawing at the specified location.</param>
        private void DrawTexture(RenderTexture rt, float x, float y, float pressure, bool erase = false)
        {
            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, RESOLUTION, RESOLUTION, 0);

            Graphics.DrawTexture(
                new Rect(
                    x - _brushTexture.width * (BRUSH_SIZE + pressure) / 2,
                    (rt.height - y) - _brushTexture.height * (BRUSH_SIZE + pressure) / 2,
                    _brushTexture.width * (BRUSH_SIZE + pressure),
                    _brushTexture.height * (BRUSH_SIZE + pressure)),

                _brushTexture,

                (erase) ? _eraseMaterial : _brushMaterial
            );

            GL.PopMatrix();
            RenderTexture.active = null;
        }

        /// <summary>
        /// Hides the color wheel after a few seconds, unless canceled.
        /// </summary>
        /// <returns></returns>
        private IEnumerator HideColorWheel()
        {
            yield return new WaitForSeconds(3);

            _colorWheel.SetActive(false);
        }

        /// <summary>
        /// Sets the current drawing color.
        /// </summary>
        /// <param name="degree">The accepted range is between (0-355).</param>
        private void UpdateColor(int degree = 0)
        {
            // Calculate the selected color based on the provided touch pad degree.
            int colorIndex = (int)(degree / (TOUCHRING_DEGREES / _colors.Length));
            if (colorIndex >= 0 && colorIndex < _colors.Length)
            {
                _touchpadArrowMaterial.color = _colors[colorIndex];
                _brushMaterial.SetColor("_Color", _colors[colorIndex]);
            }
        }

        private void HandleOnTabletConnect(byte tabletId)
        {
            Connected = true;
        }

        private void HandleOnTabletDisconnect(byte tabletId)
        {
            Connected = false;
        }

        private void HandleOnTabletButtonUp(byte tabletId, MLInput.TabletDeviceButton tabletButton, ulong timestamp)
        {
            if (_deviceButtons.ContainsKey(tabletButton))
            {
                for (int i = 0; i < _deviceButtons[tabletButton].Count; i++)
                {
                    _deviceButtons[tabletButton][i].material = _defaultMaterial;
                }
            }

            // Pen - Erase Mode
            if (tabletButton == MLInput.TabletDeviceButton.Button11)
            {
                ButtonErase = false;
            }

            LastButton = tabletButton;
            LastButtonState = false;
        }

        private void HandleOnTabletButtonDown(byte tabletId, MLInput.TabletDeviceButton tabletButton, ulong timestamp)
        {
            if (_deviceButtons.ContainsKey(tabletButton))
            {
                for (int i = 0; i < _deviceButtons[tabletButton].Count; i++)
                {
                    _deviceButtons[tabletButton][i].material = _activeMaterial;
                }
            }

            // If center touch ring button is pressed, clear the canvas.
            if (tabletButton == MLInput.TabletDeviceButton.Button9)
            {
                ClearCanvas();
            }

            // Pen - Erase Mode
            if(tabletButton == MLInput.TabletDeviceButton.Button11)
            {
                ButtonErase = true;
            }

            LastButton = tabletButton;
            LastButtonState = true;
        }

        #if PLATFORM_LUMIN
        private void HandleOnTabletPenTouch(byte tabletId, MLInput.TabletState tabletState)
        {
            // Set the location of the pen.
            _pen.localPosition = new Vector3(tabletState.PenTouchPosAndForce.x / -7.75f, tabletState.PenDistance / 100, tabletState.PenTouchPosAndForce.y / 13.25f);

            // Set the rotation of the pen.
            if (tabletState.ValidityCheck.HasFlag(MLInput.TabletDeviceStateMask.HasAdditionalPenTouchData))
            {
                _pen.localRotation = Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(tabletState.AdditionalPenTouchData[1], tabletState.AdditionalPenTouchData[0] * -1, 0);
            }

            // Only draw when the pen is touching.
            if (tabletState.IsPenTouchActive)
            {
                // Determine the hit location on the canvas from the location of the pen.
                if (Physics.Raycast(new Ray(_pen.transform.position + (_pen.transform.forward * 0.0025f), _canvasRenderer.transform.up * -1), out _pixelHit))
                {
                    // Confirm the correct object is being hit.
                    if (_pixelHit.collider.gameObject == _canvasRenderer.gameObject)
                    {
                        // Only draw the pixel if it doesn't match what was detected.
                        if (_pixelPoint != _pixelHit.lightmapCoord)
                        {
                            _pixelPoint = _pixelHit.lightmapCoord;

                            _pixelPoint.y *= RESOLUTION;
                            _pixelPoint.x *= RESOLUTION;

                            // Check if the tool type is set to erase, or if the user is pressing the erase button.
                            bool erase = (tabletState.ToolType == MLInput.TabletDeviceToolType.Eraser || ButtonErase);

                            DrawTexture(_canvas, _pixelPoint.x, _pixelPoint.y, tabletState.PenTouchPosAndForce.z, erase);
                        }
                    }
                }
            }

            // Update our last state, used for on-screen information.
            LastPositionAndForce = tabletState.PenTouchPosAndForce;
            LastDistance = tabletState.PenDistance;

            // Pen tilt information.
            if (tabletState.ValidityCheck.HasFlag(MLInput.TabletDeviceStateMask.HasAdditionalPenTouchData))
            {
                LastTilt = new Vector2(tabletState.AdditionalPenTouchData[0], tabletState.AdditionalPenTouchData[1]);
            }

            LastIsTouching = tabletState.IsPenTouchActive;
            LastToolType = tabletState.ToolType;
        }
        #endif

        private void HandleOnTabletRingTouch(byte tabletId, int touchRingValue, ulong timestamp)
        {
            if (touchRingValue != 0)
            {
                // Cancel any coroutines to hide the colorwheel.
                StopAllCoroutines();

                _colorWheel.SetActive(true);
                _touchpadArrow.localEulerAngles = new Vector3(0, touchRingValue * TOUCHRING_MULTIPLIER, 0);

                UpdateColor(touchRingValue * TOUCHRING_MULTIPLIER);
            }
            else
            {
                StartCoroutine(HideColorWheel());
            }

            LastTouchRing = touchRingValue;
        }
    }
}

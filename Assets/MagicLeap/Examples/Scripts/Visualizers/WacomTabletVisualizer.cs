// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections;
using UnityEngine;

namespace MagicLeap.Examples
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

        // The default drawing size of the brush.
        private const float BRUSH_SIZE = 0.1f;

        [Header("Tablet")]

        [SerializeField, Tooltip("The wacom tablet example to use for displaying pen and button data.")]
        private WacomTabletFeedbackExample wacomTabletExample = null;

        [SerializeField, Tooltip("The default material to apply to the tablet.")]
        private Material _defaultMaterial = null;

        [SerializeField, Tooltip("The active material to apply to the tablet.")]
        private Material _activeMaterial = null;

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

        // The material applied to the touch ring indicator.
        private Material _touchpadArrowMaterial = null;

        // Used for canvas hit detection and drawing.
        private RaycastHit _pixelHit;
        private Vector2 _pixelPoint = Vector2.zero;
        private RenderTexture _canvas = null;
        private int currentColorIndex;

        // Origin point (0,0) in Drawing_Plane_GEO, used to offset the pen movement
        private Vector2 originPoint = new Vector2(0.128f, -0.077f);
        // Used to scale the pen position
        private Vector2 penPositionScale = new Vector2(2800, 5700);


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
            UpdateColor(currentColorIndex);
            
            wacomTabletExample.OnFirstBarrelButtonDown += HandleFirstBarrelButton;
            wacomTabletExample.OnSecondBarrelButtonDown += HandleSecondBarrelButton;
        }

        void Update()
        {
            var isPenConnected = wacomTabletExample.PenConnected;
            _pen.gameObject.SetActive(isPenConnected);
            if (!isPenConnected)
                return;

            // Set the location of the pen.
            _pen.localPosition = new Vector3(originPoint.x - wacomTabletExample.Position.x / penPositionScale.x,
                wacomTabletExample.Distance / 100,
                originPoint.y - wacomTabletExample.Position.y / penPositionScale.y);

            // Set the rotation of the pen.
            _pen.localRotation = Quaternion.Euler(-90, 0, 0) * 
                                 Quaternion.Euler(wacomTabletExample.PenTilt.y, wacomTabletExample.PenTilt.x * -1, 0);



            // Only draw when the pen is touching.
            if (wacomTabletExample.Tip)
            {
                var ray = new Ray(_pen.transform.position + (_pen.transform.forward * 0.0025f), _canvasRenderer.transform.up * -1);
                    
                // Determine the hit location on the canvas from the location of the pen.
                if (Physics.Raycast(ray, out _pixelHit))
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

                            var erasing = wacomTabletExample.Eraser;
                            DrawTexture(_canvas, _pixelPoint.x, _pixelPoint.y, wacomTabletExample.Pressure, erasing);
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            // Un-register for callbacks.
            wacomTabletExample.OnFirstBarrelButtonDown -= HandleFirstBarrelButton;
        }

        /// <summary>
        /// Validates fields and starts MLInput.
        /// </summary>
        private bool Initialization()
        {
            if (wacomTabletExample == null)
            {
                Debug.LogError("Error: WacomTabletFeedbackExample._wacomTabletVisualizer is not set, disabling script.");
                enabled = false;
                return false;
            }

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
        /// Erases the texture to the canvas at the given location and applied pressure <seealso cref="DrawTexture"/>.
        /// </summary>
        private void EraseTexture(RenderTexture rt, float x, float y, float pressure)
            => DrawTexture(rt, x, y, pressure, true);

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
        /// Sets the current drawing color based on color index. 
        /// </summary>
        private void UpdateColor(int colorIndex)
        {
            UpdateColor(_colors[colorIndex]);
        }
        
        /// <summary>
        /// Sets the current drawing color.
        /// </summary>
        private void UpdateColor(Color color)
        {
            _touchpadArrowMaterial.color = color;
            _brushMaterial.SetColor("_Color", color);
        }
        
        /// <summary>
        /// If first barrel button is pressed, clear the canvas.
        /// </summary>
        private void HandleFirstBarrelButton()
        {
            ClearCanvas();
        }
        
        /// <summary>
        /// If second barrel button is pressed, pick next color from palette.
        /// </summary>
        private void HandleSecondBarrelButton()
        {
            currentColorIndex = (currentColorIndex + 1) % _colors.Length;
            UpdateColor(currentColorIndex);
        }
    }
}

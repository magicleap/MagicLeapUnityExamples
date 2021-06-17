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

using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System.Collections;

namespace MagicLeap
{
    [DisallowMultipleComponent]
    public class MediaPlayerSlider : MediaPlayerButton
    {
        public event System.Action<float> OnValueChanged;

        [SerializeField, Tooltip("Local position of beginning point")]
        private Vector3 _beginRelative = Vector3.zero;

        [SerializeField, Tooltip("Local position of ending point")]
        private Vector3 _endRelative = Vector3.zero;

        [SerializeField, Tooltip("Handle of the Slider")]
        private Transform _handle = null;

        [SerializeField, Tooltip("Initial value")]
        private float _value = 0.0f;

        /// <summary>
        /// Value represents a percentage, clamped in the range [0, 1] inclusive
        /// Invokes OnValueChanged if needed
        /// </summary>
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _value))
                {
                    return;
                }
                ChangeValue(value);
            }
        }

        private void Awake()
        {
            if (_handle == null)
            {
                Debug.LogError("Error: MediaPlayerSlider._handle is not set, disabling script.");
                enabled = false;
                return;
            }

            _handle.localPosition = Vector3.Lerp(_beginRelative, _endRelative, _value);
        }

        protected override void OnEnable()
        {
            _handle.gameObject.GetComponent<Renderer>().enabled = true;

            #if PLATFORM_LUMIN
            OnControllerDrag += HandleControllerDrag;
            #endif

            base.OnEnable();
        }

        private void Start()
        {
            StartCoroutine(InitializeValue());
        }

        protected override void OnDisable()
        {
            _handle.gameObject.GetComponent<Renderer>().enabled = false;

            #if PLATFORM_LUMIN
            OnControllerDrag -= HandleControllerDrag;
            #endif

            base.OnDisable();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(_beginRelative), 0.02f);
            Gizmos.DrawWireSphere(transform.TransformPoint(_endRelative), 0.02f);
        }

        private void OnValidate()
        {
            _value = Mathf.Clamp01(_value);
        }

        #if PLATFORM_LUMIN
        /// <summary>
        /// Find the point on the slider that's closest to the line defined by
        /// the controller's position and direction.
        /// </summary>
        /// <param name="controller">Information on the controller</param>
        private void HandleControllerDrag(MLInput.Controller controller)
        {
            // Line 1 is the Controller's ray
            Vector3 P1World = controller.Position;
            Vector3 V1World = controller.Orientation * Vector3.forward;

            // Put controller position and orientation into slider space, as relative end points are in slider space
            Vector3 P1 = transform.InverseTransformPoint(P1World);
            Vector3 V1 = transform.InverseTransformVector(V1World);

            // Line 2 is the slider
            Vector3 P2 = _beginRelative;
            Vector3 V2 = _endRelative - _beginRelative;

            Vector3 deltaP = P2 - P1;
            float V21 = Vector3.Dot(V2, V1);
            float V11 = Vector3.Dot(V1, V1);
            float V22 = Vector3.Dot(V2, V2);

            float tNum = Vector3.Dot(deltaP, V21 * V1 - V11 * V2);
            float tDen = V22 * V11 - V21 * V21;

            // Lines are parallel
            if (Mathf.Approximately(tDen, 0.0f))
            {
                return;
            }

            // closest point on Line 2 to Line 1 is P2 + t * V2
            // but we're only concerned with t
            Value = tNum / tDen;
        }
        #endif

        private IEnumerator InitializeValue()
        {
            yield return new WaitForEndOfFrame();
            ChangeValue(_value);
        }

        private void ChangeValue(float newValue)
        {
            _value = newValue;
            _handle.localPosition = Vector3.Lerp(_beginRelative, _endRelative, _value);
            if (OnValueChanged != null)
            {
                OnValueChanged(_value);
            }
        }
    }
}

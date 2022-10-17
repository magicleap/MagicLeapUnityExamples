using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.Examples
{
    public class AnchorVisual : MonoBehaviour
    {
        private TextMesh dataText;
#if UNITY_MAGICLEAP || UNITY_ANDROID
        private Timer disableTimer;
#endif

        void Awake()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            dataText = transform.Find("Info").GetComponent<TextMesh>();
            disableTimer = new Timer(0.1f);
#endif
        }

        void Update()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            if (gameObject.activeSelf && disableTimer.LimitPassed)
            {
                gameObject.SetActive(false);
            }
#endif
        }

        public void Set(MLAnchors.Anchor data)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            disableTimer?.Reset();
#endif
            transform.position = data.Pose.position;
            transform.rotation = data.Pose.rotation;
            dataText.text = data.ToString();
            gameObject.SetActive(true);
        }


    }
}

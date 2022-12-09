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
        private Timer disableTimer;

        void Awake()
        {
            dataText = transform.Find("Info").GetComponent<TextMesh>();
            disableTimer = new Timer(0.1f);
        }

        void Update()
        {
            if (gameObject.activeSelf && disableTimer.LimitPassed)
            {
                gameObject.SetActive(false);
            }
        }

        public void Set(MLAnchors.Anchor data)
        {
            disableTimer?.Reset();
            transform.position = data.Pose.position;
            transform.rotation = data.Pose.rotation;
            dataText.text = data.ToString();
            gameObject.SetActive(true);
        }


    }
}

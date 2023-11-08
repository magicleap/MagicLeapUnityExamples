using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.MagicLeap
{
    //Disabling WebRTC deprecated warning for the examples project
    #pragma warning disable 618
    public class DefinedAudioSourceExample : MLWebRTC.AppDefinedAudioSource
    {
        public DefinedAudioSourceExample(string trackId)
           : base(trackId)
        {

        }

        protected override void OnSourceDestroy()
        {
            Debug.Log("OnSourceDestroy");
        }

        protected override void OnSourceSetEnabled(bool enabled)
        {
            Debug.Log("OnSourceSetEnabled: " + enabled);
        }

    }
}

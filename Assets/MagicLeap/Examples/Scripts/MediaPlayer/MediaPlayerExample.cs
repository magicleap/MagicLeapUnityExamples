// Disabling MLMedia deprecated warning for the internal project
#pragma warning disable 618

using MagicLeap.Core;
using UnityEngine;

namespace MagicLeap.Examples
{
    public class MediaPlayerExample : MonoBehaviour
    {
        [SerializeField, Tooltip("Media player")]
        private MLMediaPlayerBehavior mediaPlayerBehavior;

        void Start()
        {
            mediaPlayerBehavior.OnStop += OnMediaPlayerStopped;
        }

        private void OnMediaPlayerStopped()
        {
            //Prepare player to reduce buffer delay
            mediaPlayerBehavior.MediaPlayer.PreparePlayerAsync();
        }
    }
}

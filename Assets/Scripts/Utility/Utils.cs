using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace MagicLeap.Examples
{
    public static class Utils
    {
        public static bool AreSubsystemsLoaded<T>() where T : class, ISubsystem
        {
            if (XRGeneralSettings.Instance == null) return false;
            if (XRGeneralSettings.Instance.Manager == null) return false;
            var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
            if (activeLoader == null) return false;
            return activeLoader.GetLoadedSubsystem<T>() != null;
        }
        
    }
}

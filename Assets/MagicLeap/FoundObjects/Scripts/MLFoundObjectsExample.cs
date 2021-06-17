// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// <copyright company="Magic Leap">
//
// Copyright (c) 2018-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://id.magicleap.com/terms/developer
//
// </copyright>
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using MagicLeap.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Demonstrates an example of using the MLFoundObjectsBehavior.
    /// </summary>
    public class MLFoundObjectsExample : MonoBehaviour
    {
        public MLFoundObjects.Settings settings = MLFoundObjects.Settings.Create();

        [SerializeField]
        private Text _statusText = null;

        [SerializeField]
        private MLFoundObjectsBehavior _foundObjectsBehavior = null;

        private Dictionary<string, int> _foundObjectsTypes = new Dictionary<string, int>();

        void OnEnable()
        {
            if (_statusText == null)
            {
                Debug.LogError("Error: FoundObjectExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }


            if (_foundObjectsBehavior == null)
            {
                Debug.LogError("Error: FoundObjectExample._foundObjectsBehavior is not set, disabling script.");
                enabled = false;
                return;
            }

            _foundObjectsBehavior.OnFoundObjects += HandleOnFoundObjects;
        }

        void OnDisable()
        {
            _foundObjectsBehavior.OnFoundObjects -= HandleOnFoundObjects;
        }

        private void HandleOnFoundObjects(MLFoundObjects.FoundObject[] foundObjects)
        {
            _foundObjectsTypes.Clear();

            foreach (MLFoundObjects.FoundObject foundObject in foundObjects)
            {
                if (!_foundObjectsTypes.ContainsKey(foundObject.Label))
                {
                    _foundObjectsTypes.Add(foundObject.Label, 0);
                }

                _foundObjectsTypes[foundObject.Label]++;
            }

            _statusText.text = "Found Object Labels:\n";
            foreach (KeyValuePair<string, int> pair in _foundObjectsTypes)
            {
                _statusText.text += string.Format("{0}: {1}\n", pair.Key, pair.Value);
            }
        }
    }
}

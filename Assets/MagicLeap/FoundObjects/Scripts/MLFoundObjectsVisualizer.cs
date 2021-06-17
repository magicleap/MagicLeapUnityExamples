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

using System;
using MagicLeap.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Demonstrates how to detect and visualize found objects.
    /// </summary>
    public class MLFoundObjectsVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The prefab used for the detected found objects.")]
        private MLFoundObjectVisual _objectVisual = null;

        [SerializeField]
        private MLFoundObjectsBehavior _foundObjectsBehavior = null;
        private Dictionary<Guid, GameObject> _foundObjects = new Dictionary<Guid, GameObject>();
        private HashSet<Guid> _foundObjectsToRemove = new HashSet<Guid>();

#if PLATFORM_LUMIN
        void OnEnable()
        {
            if (_objectVisual == null)
            {
                Debug.LogError("Error: MLFoundObjectsVisualizer._objectVisual is not set, disabling script.");
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
            foreach (KeyValuePair<Guid, GameObject> pair in _foundObjects)
            {
                _foundObjectsToRemove.Add(pair.Key);
            }

            foreach (MLFoundObjects.FoundObject foundObject in foundObjects)
            {
                GameObject objectInstance = null;

                // Obtain a reference to the found object visual GameObject.
                if (_foundObjects.ContainsKey(foundObject.Id))
                {
                    _foundObjects.TryGetValue(foundObject.Id, out objectInstance);
                    _foundObjectsToRemove.Remove(foundObject.Id);
                }
                else
                {
                    objectInstance = Instantiate(_objectVisual.gameObject);

                    // Add the new instance to the dictionary.
                    _foundObjects.Add(foundObject.Id, objectInstance);
                    _foundObjectsToRemove.Remove(foundObject.Id);
                }

                if (objectInstance == null)
                {
                    Debug.LogError("Error: MLFoundObjectsVisualizer.HandleOnFoundObjects failed to obtain an object instance.");
                    return;
                }

                // Update the found object visual with the new properties.
                _objectVisual.UpdateVisual(foundObject.Position, foundObject.Rotation, foundObject.Size, foundObject.Label);
            }

            foreach (Guid foundObjectId in _foundObjectsToRemove)
            {
                if(_foundObjects.ContainsKey(foundObjectId))
                {
                    Destroy(_foundObjects[foundObjectId]);
                    _foundObjects.Remove(foundObjectId);
                }
            }

            _foundObjectsToRemove.Clear();
        }
#endif
    }
}

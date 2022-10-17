// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2021-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.XR.MagicLeap.MLMarkerTracker;

namespace MagicLeap.Examples
{
    [ExecuteInEditMode]
    public class DictionaryNameDropdownOptionsInjector : MonoBehaviour
    {
        private Dropdown _dropdown;

        // Start is called before the first frame update
        void Start()
        {
            _dropdown = GetComponent<Dropdown>();

            if (_dropdown.options.Count == 0)
            {
                var dictionaryNames = Enum.GetNames(typeof(ArucoDictionaryName)).ToList();
                _dropdown.AddOptions(dictionaryNames);
            }
        }
    }
}

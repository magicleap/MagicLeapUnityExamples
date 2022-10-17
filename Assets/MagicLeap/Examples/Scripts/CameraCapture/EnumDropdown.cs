// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace UnityEngine.XR.MagicLeap
{
    public class EnumDropdown : Dropdown
    {
        public T GetSelected<T>()
        {
            if (options.Count <= value)
            {
                return default(T);
            }
            return (T)Enum.Parse(typeof(T), options[value].text);
        }

        public void AddOptions<T>(params T[] options)
        {
            foreach (T option in options)
            {
                base.AddOptions(new List<string> { option.ToString() });
            }
        }

        public void SelectOption<T>(T option, bool notify)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (!((T)Enum.Parse(typeof(T), options[i].text)).Equals(option))
                    continue;

                if (notify)
                {
                    value = i;
                    return;
                }

                SetValueWithoutNotify(i);
                return;
            }
        }
    }
}

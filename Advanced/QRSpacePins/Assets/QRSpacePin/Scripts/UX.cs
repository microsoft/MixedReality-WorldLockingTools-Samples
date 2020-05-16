// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.WorldLocking.Core;

namespace Microsoft.MixedReality.WorldLocking.Samples
{
    /// <summary>
    /// Simple bit of UX to toggle and display the status of the WorldLockingManager.
    /// </summary>
    public class UX : MonoBehaviour
    {
        public List<GameObject> targets = new List<GameObject>();

        public TextMesh statusText = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (statusText != null)
            {
                var wltMgr = WorldLockingManager.GetInstance();
                var settings = wltMgr.Settings;
                string status = settings.Enabled ? "on" : "off";
                statusText.text = $"WLT {status}";
                statusText.color = settings.Enabled ? Color.green : Color.red;
            }
        }

        public void OnToggleManager()
        {
            var wltMgr = WorldLockingManager.GetInstance();
            var settings = wltMgr.Settings;
            settings.Enabled = !settings.Enabled;
            wltMgr.Settings = settings;
            for (int i = 0; i < targets.Count; ++i)
            {
                targets[i].SetActive(!targets[i].activeSelf);
            }
        }
    }
}
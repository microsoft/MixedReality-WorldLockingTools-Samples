// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;

// mafinc - All ASA needs to be hidden on the other side of the IPublisher interface. 
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using NativeAnchor = Microsoft.Azure.SpatialAnchors.Unity.ARFoundation.UnityARFoundationAnchorComponent;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    public class SpacePinASA : SpacePinOrientable
    {

        private ILocalPeg localPeg = null;

        public ILocalPeg LocalPeg { get { return localPeg; } }

        public IPublisher Publisher { get; set; }

        [Serializable]
        public class KeyValPair
        {
            public string key;
            public string val;
        };

        [SerializeField]
        private List<KeyValPair> propertyList = new List<KeyValPair>();
        
        private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

        public Dictionary<string, string> Properties => properties;

        private void Awake()
        {
            foreach (var keyval in propertyList)
            {
                Debug.Assert(!string.IsNullOrEmpty(keyval.key));
                Debug.Assert(!string.IsNullOrEmpty(keyval.val));
                properties[keyval.key] = keyval.val;
            }
            if (!properties.ContainsKey(SpacePinBinder.SpacePinIdKey))
            {
                properties[SpacePinBinder.SpacePinIdKey] = name;
            }
        }

        private Pose lastPose = new Pose(new Vector3(1000.0f, 1000.0f, 1000.0f), Quaternion.identity);
        protected void DebugSpew()
        {
#if false
            if (NativeAnchor != null)
            {
                Debug.Log($"{name} Frame:{Time.frameCount} {anchorHolder.transform.GetGlobalPose().ToString("F3")} nonident:{anchorHolder.transform.position.x != 0}");
            }
            else
            {
                Debug.Log($"{name} {Time.frameCount}:{name}: anchorHolder={(anchorHolder == null ? "null" : anchorHolder.name)}, NativeAnchor={(NativeAnchor == null ? "null" : NativeAnchor.name)}");
            }
#endif
#if false
            if (AnchorHolder != null)
            {
                float dist = Vector3.Distance(lastPose.position, AnchorHolder.transform.position);
                float distanceCutoff = 0.005f; // 5mm
                if (dist > distanceCutoff)
                {
                    SimpleConsole.AddLine(8, $"{name} moved {lastPose.position.ToString("F3")} -> {AnchorHolder.transform.position.ToString("F3")}");
                    lastPose = anchorHolder.transform.GetGlobalPose();
                }
            }
#endif
        }

        public bool IsReadyForPublish
        {
            get
            {
                if (LocalPeg == null)
                {
                    return false;
                }
                return LocalPeg.IsReadyForPublish;
            }
        }
#if false
        public void SetLocalPeg(GameObject holder)
        {
            Debug.Assert(holder.FindNativeAnchor() != null, "Anchor holder must be provisioned with a NativeAnchor.");
            if (anchorHolder != null)
            {
                GameObject.Destroy(anchorHolder);
            }
            anchorHolder = holder;
            Debug.Log($"SetAnchorHolder:{name}: anchorHolder={(anchorHolder == null ? "null" : anchorHolder.name)}, NativeAnchor={(NativeAnchor == null ? "null" : NativeAnchor.name)}");
            SimpleConsole.AddLine(8, $"SetAH: {name} p={anchorHolder.transform.position.ToString("F3")}");
        }
#else
        public void SetLocalPeg(ILocalPeg peg)
        {
            if (localPeg != null)
            {
                Publisher.ReleaseLocalPeg(peg);
            }
            localPeg = peg;
        }
#endif

        public async void ConfigureLocalPeg()
        {
#if false
            SimpleConsole.AddLine(8, $"ConfigAH: {Time.frameCount}");
            int waitForAnchor = 30;
            await Task.Delay(waitForAnchor);
            SimpleConsole.AddLine(8, $"Waited {waitForAnchor}ms: {Time.frameCount}");
            if (anchorHolder == null)
            {
                anchorHolder = new GameObject($"{name}_anchorHolder");
            }
            anchorHolder.DeleteNativeAnchor();
            Debug.Log($"SfL: {WorldLockingManager.GetInstance().SpongyFromLocked.ToString("F3")}");
            Pose spongyPose = WorldLockingManager.GetInstance().SpongyFromLocked.Multiply(LockedPose);
            Pose anchorPose = WorldLockingManager.GetInstance().AnchorManager.AnchorFromSpongy.Multiply(spongyPose);
            Debug.Log($"ConfigureAnchor: lo={LockedPose.ToString("F3")}, sp={spongyPose.ToString("F3")} an={anchorPose.ToString("F3")}");
            SimpleConsole.AddLine(8, $"ConfigAH: {name} lo={LockedPose.position.ToString("F3")}, sp={spongyPose.position.ToString("F3")} ah={anchorPose.position.ToString("F3")}");
            anchorHolder.transform.SetGlobalPose(anchorPose);
            anchorHolder.CreateNativeAnchor();
            Debug.Log($"ConfigureAnchorHolder:{name}: anchorHolder={(anchorHolder == null ? "null" : anchorHolder.name)}, NativeAnchor={(NativeAnchor == null ? "null" : NativeAnchor.name)}");
            SimpleConsole.AddLine(8, $"ConfigAH: {name} p={anchorHolder.transform.position.ToString("F3")}");
#else
            if (Publisher == null)
            {
                SimpleConsole.AddLine(8, $"Publisher hasn't been set on SpacePin={name}");
                return;
            }
            if (localPeg != null)
            {
                Publisher.ReleaseLocalPeg(localPeg);
            }
            localPeg = await Publisher.CreateLocalPeg($"{name}_peg", LockedPose);
#endif
        }
    }
}
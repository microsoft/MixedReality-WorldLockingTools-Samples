// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if WLT_ARSUBSYSTEMS_PRESENT
using UnityEngine.XR.ARSubsystems;

namespace Microsoft.MixedReality.WorldLocking.Core
{
    /// <summary>
    /// Encapsulation of spongy world (raw input) state. Its primary duty is the creation and maintenance
    /// of the graph of (spongy) anchors built up over the space traversed by the camera.
    /// </summary>
    /// <remarks>
    /// Anchor and Edge creation algorithm:
    /// 
    /// Goal: a simple and robust algorithm that guarantees an even distribution of anchors, fully connected by
    /// edges between nearest neighbors with a minimum of redundant edges
    ///
    /// For simplicity, the algorithm should be stateless between time steps
    ///
    /// Rules
    /// * two parameters define spheres MIN and MAX around current position
    /// * whenever MIN does not contain any anchors, a new anchor is created
    /// * when a new anchor is created is is linked by edges to all anchors within MAX
    /// * the MAX radius is 20cm larger than MIN radius which would require 12 m/s beyond world record sprinting speed to cover in one frame
    /// * whenever MIN contains more than one anchor, the anchor closest to current position is connected to all others within MIN 
    /// </remarks>
    public class AnchorManagerXR : AnchorManager
    {

        protected override float TrackingStartDelayTime { get { return SpongyAnchorXR.TrackingStartDelayTime; } }

        private readonly XRAnchorSubsystem anchorSubsystem;

        private readonly Dictionary<TrackableId, SpongyAnchorXR> anchorsByTrackableId = new Dictionary<TrackableId, SpongyAnchorXR>();

        public static AnchorManagerXR TryCreate(IPlugin plugin, IHeadPoseTracker headTracker)
        {
            /// Try to find an XRAnchorSubsystem here. 
            /// If we fail that,
            ///     give up. 
            /// Else 
            ///     pass the manager into AnchorManagerXR for its use.
            XRAnchorSubsystem anchorSubsystem = FindAnchorSubsystem();

            if (anchorSubsystem == null)
            {
                return null;
            }
            anchorSubsystem.Start();

            AnchorManagerXR anchorManager = new AnchorManagerXR(plugin, headTracker, anchorSubsystem);

            return anchorManager;
        }

        private static XRAnchorSubsystem FindAnchorSubsystem()
        {
            List<XRAnchorSubsystemDescriptor> descriptors = new List<XRAnchorSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            Debug.Log($"Found {descriptors.Count} XRAnchorSubsystemDescriptors");
            if (descriptors.Count < 1)
            {
                Debug.Log("No XRAnchorSubsystem descriptors found, failing");
                return null;
            }
            string descriptorList = "Descriptor List:\n";
            for (int i = 0; i < descriptors.Count; ++i)
            {
                descriptorList += descriptors[i].ToString() + "\n";
            }
            Debug.Log(descriptorList);
            var anchorSubsystem = descriptors[0].Create();
            Debug.Assert(anchorSubsystem != null, "Failure creating anchor subsystem from descriptor");
            return anchorSubsystem;
        }

        /// <summary>
        /// Set up an anchor manager.
        /// </summary>
        /// <param name="plugin">The engine interface to update with the current anchor graph.</param>
        private AnchorManagerXR(IPlugin plugin, IHeadPoseTracker headTracker, XRAnchorSubsystem anchorSubsystem)
            : base(plugin, headTracker)
        {
            this.anchorSubsystem = anchorSubsystem;
            Debug.Log($"XR: Created AnchorManager XR, xrMgr={(this.anchorSubsystem != null ? "good" : "null")}");
        }

        public override bool Update()
        {
            if (!UpdateTrackables())
            {
                return false;
            }
            return base.Update();
        }

        private bool UpdateTrackables()
        {
            if (anchorSubsystem == null)
            {
                return false;
            }
            TrackableChanges<XRAnchor> changes = anchorSubsystem.GetChanges(Unity.Collections.Allocator.Temp);
            if (changes.isCreated && (changes.added.Length + changes.updated.Length + changes.removed.Length > 0))
            {
                Debug.Log($"Changes Fr{Time.frameCount:0000}: isCreated={changes.isCreated} Added={changes.added.Length}, Updated={changes.updated.Length} Removed={changes.removed.Length}");
                for (int i = 0; i < changes.added.Length; ++i)
                {
                    UpdateTracker("Added::", changes.added[i], anchorsByTrackableId);
                }
                for (int i = 0; i < changes.updated.Length; ++i)
                {
                    UpdateTracker("Updated::", changes.updated[i], anchorsByTrackableId);
                }
                for (int i = 0; i < changes.removed.Length; i++)
                {
                    RemoveTracker(changes.removed[i], anchorsByTrackableId);
                }
            }
            changes.Dispose();
            return true;
        }
        private static bool RemoveTracker(TrackableId trackableId, Dictionary<TrackableId, SpongyAnchorXR> anchors)
        {
            Debug.Log($"Removed:: id={trackableId}");

            return anchors.Remove(trackableId);
        }

        private static float DebugNormAngleDeg(float deg)
        {
            while (deg > 180.0f)
            {
                deg -= 360.0f;
            }
            return deg;
        }
        private static Vector3 DebugNormRot(Vector3 euler)
        {
            euler.x = DebugNormAngleDeg(euler.x);
            euler.y = DebugNormAngleDeg(euler.y);
            euler.z = DebugNormAngleDeg(euler.z);
            return euler;
        }
        public static string DebugEuler(string label, Vector3 euler)
        {
            euler = DebugNormRot(euler);
            return $"{label}{euler}";
            //return DebugVector3(label, euler);
        }
        public static string DebugQuaternion(string label, Quaternion q)
        {
            return $"{label}({q.x:0.00},{q.y:0.00},{q.z:0.00},{q.w:0.00})";
        }
        public static string DebugVector3(string label, Vector3 p)
        {
            return $"{label}({p.x:0.000},{p.y:0.000},{p.z:0.000})";
        }

        private static void DebugOut(string label, XRAnchor xrAnchor, SpongyAnchorXR tracker)
        {
            Debug.Assert(xrAnchor.trackableId == tracker.TrackableId);
            Vector3 tP = tracker.transform.position;
            Vector3 tR = tracker.transform.rotation.eulerAngles;
            Vector3 rP = xrAnchor.pose.position;
            Vector3 rR = xrAnchor.pose.rotation.eulerAngles;
            rR = new Vector3(1.0f, 2.0f, 3.0f);
            Debug.Log($"{label}{tracker.name}-{tracker.TrackableId}/{xrAnchor.trackingState}: {DebugVector3("tP=", tP)}|{DebugEuler("tR=", tR)} <=> {DebugVector3("rP=", rP)}|{DebugEuler("rR=", rR)}");
        }

        private static void UpdateTracker(string label, XRAnchor xrAnchor, Dictionary<TrackableId, SpongyAnchorXR> anchors)
        {
            SpongyAnchorXR tracker;
            if (anchors.TryGetValue(xrAnchor.trackableId, out tracker))
            {
                DebugOut(label, xrAnchor, tracker);

                /// mafinc - Would rather base this on the current TrackingState of the XRAnchor, 
                /// but that is not currently reliable.
                //tracker.IsReliablyLocated = true;
                if (tracker.IsReliablyLocated != (xrAnchor.trackingState != TrackingState.None))
                {
                    Debug.Log($"TOGGLE::{label}{tracker.name} - {xrAnchor.trackingState}");
                }
                tracker.IsReliablyLocated = xrAnchor.trackingState != TrackingState.None;

                Pose repose = ExtractPose(xrAnchor);
                tracker.transform.position = repose.position;
                tracker.transform.rotation = repose.rotation;
            }
            else
            {
                Debug.LogError($"Missing trackableId {xrAnchor.trackableId} from DB.");
            }
        }

        private static Pose ExtractPose(XRAnchor xrAnchor)
        {
            Pose repose = xrAnchor.pose;
            if (xrAnchor.trackingState == TrackingState.None)
            {
                repose.position.z = -repose.position.z;
                repose.rotation.x = -repose.rotation.x;
                repose.rotation.y = -repose.rotation.y;
            }
            return repose;
        }

        private static bool CheckTracking(XRAnchor xrAnchor)
        {
            return xrAnchor.trackingState != TrackingState.None;
        }


        protected override bool IsTracking()
        {
            //Debug.Log($"AnchorManagerXR F{Time.frameCount}: xrMgr is {(anchorSubsystem != null && anchorSubsystem.running ? "running" : "null")}");
            return anchorSubsystem != null && anchorSubsystem.running;
        }

        protected override SpongyAnchor CreateAnchor(AnchorId id, Transform parent, Pose initialPose)
        {
            SpongyAnchorXR spongyAnchorXR = null;
            if (IsTracking())
            {
                Debug.Log($"Creating refPt at initial ({initialPose.position.x:0.000}, {initialPose.position.y:0.000}, {initialPose.position.z:0.000})");
                XRAnchor xrAnchor;
                bool created = anchorSubsystem.TryAddAnchor(initialPose, out xrAnchor);
                if (created)
                {
                    Pose xrPose = xrAnchor.pose;
                    Debug.Log($"Created refPt {id} at ({xrPose.position.x:0.000}, {xrPose.position.y:0.000}, {xrPose.position.z:0.000}) is {xrAnchor.trackingState}");
                    var newAnchorObject = new GameObject(id.FormatStr());
                    newAnchorObject.transform.parent = parent;
                    newAnchorObject.transform.SetGlobalPose(initialPose);
                    spongyAnchorXR = newAnchorObject.AddComponent<SpongyAnchorXR>();
                    anchorsByTrackableId[xrAnchor.trackableId] = spongyAnchorXR;
                    spongyAnchorXR.TrackableId = xrAnchor.trackableId;

                    Debug.Log($"{id} {DebugVector3("P=", initialPose.position)}, {DebugQuaternion("Q=", initialPose.rotation)}");
                }
            }
            return spongyAnchorXR;
        }

        protected override SpongyAnchor DestroyAnchor(AnchorId id, SpongyAnchor spongyAnchor)
        {
            SpongyAnchorXR spongyAnchorXR = spongyAnchor as SpongyAnchorXR;
            if (spongyAnchorXR != null)
            {
                Debug.Assert(anchorsByTrackableId[spongyAnchorXR.TrackableId] == spongyAnchorXR);
                anchorsByTrackableId.Remove(spongyAnchorXR.TrackableId);
                anchorSubsystem.TryRemoveAnchor(spongyAnchorXR.TrackableId);
                GameObject.Destroy(spongyAnchorXR.gameObject);
            }
            RemoveSpongyAnchorById(id);

            return null;
        }


        protected override async Task SaveAnchors(List<SpongyAnchorWithId> spongyAnchors)
        {
            await Task.CompletedTask;
        }


        /// <summary>
        /// Load the spongy anchors from persistent storage
        /// </summary>
        /// <remarks>
        /// The set of spongy anchors loaded by this routine is defined by the frozen anchors
        /// previously loaded into the plugin.
        /// 
        /// Likewise, when a spongy anchor fails to load, this routine will delete its frozen
        /// counterpart from the plugin.
        /// </remarks>
        protected override async Task LoadAnchors(IPlugin plugin, AnchorId firstId, Transform parent, List<SpongyAnchorWithId> spongyAnchors)
        {
            var anchorIds = plugin.GetFrozenAnchorIds();

            /// Placeholder for consistency. Persistence not yet implemented for ARF, so
            /// to be consistent with this APIs contract, we must clear all frozen anchors from the plugin.
            foreach (var id in anchorIds)
            {
                plugin.RemoveFrozenAnchor(id);
            }

            await Task.CompletedTask;
        }
    }
}
#endif // WLT_ARSUBSYSTEMS_PRESENT
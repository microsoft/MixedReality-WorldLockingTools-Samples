// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using NativeAnchor = Microsoft.Azure.SpatialAnchors.Unity.ARFoundation.UnityARFoundationAnchorComponent;

using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;

/// <summary>
/// NOTE DANGER OF RACE CONDITION ON REFIT EVENTS
/// If we are receiving system updates/refinements on cloud anchor poses,
///    If tracker tracker makes correction (e.g. due to loop closure) to cloud anchor pose
///    Then later FW issues a refreeze correction suitable for previous cloud anchor pose
///       FW correction will be applied to already corrected cloud anchor pose.
/// </summary>
// mafish - SpacePinCloudBinder???

/// Communication pattern between binder and publisher.
/// Binder keeps list of AnchoredSpacePins anchoredPins.
/// AnchoredSpacePin is protected (or private) type.
/// SpacePinPublisher is class private to SpacePinBinder, so has access to AnchoredSpacePin.
/// SpacePinBinder makes async calls to SpacePinPublisher, but doesn't await them.
/// Instead, when the SpacePinPublisher finishes creating, updating, or destroying something,
/// it puts the new version of the thing in List<AnchoredSpacePin> completedTasks (or some other name).
/// Appending to completedTasks is protected by a lock.
/// Then every update, the SpacePinBinder goes through the completedTasks list (protected by a lock)
/// and replaces the matching thing in anchoredPins with the element in completedTasks, then 
/// clears the completedTasks list.
/// Note that anchoredPins is only ever accessed on main thread, so doesn't need lock protection.
/// 
/// PROBLEM: We care what has been changed about this AnchoredSpacePin.
///   Published: Do nothing, we've just broadcast local SpacePin to everyone else. So by
///        definition, local SpacePin is already correct.
///   Downloaded: Push new Pose to appropriate SpacePin (via SetLockedPose())
///   Deleted: Reset associated SpacePin, its backing Pose is no more.
/// So, maybe an enum accompanying the updated AnchoredSpacePin? 
/// enum CompletionType
/// {
///    Published,
///    Downloaded,
///    Deleted
/// };
/// struct CompletedTask
/// {
///     CompletionType type;
///     AnchoredSpacePin newState;
/// };
/// 
///
/// SpacePin states:
///    SpacePin + anchorHolder(nativeAnchor) => ready to be published.
///    SpacePin + cloudAnchorId => ready to be downloaded.
///    SpacePin + anchorHolder + cloudAnchorId => has been published or has been downloaded. 
///        Ready to be unpublished.
///        If unpublished, goes to state SpacePin + anchorHolder(nativeAnchor), i.e. ready to be published.
///    SpacePin + nothing => Can't do anything with it.
namespace Microsoft.MixedReality.WorldLocking.ASA
{
    using AnchorProperties = Dictionary<string, string>;
    using CloudAnchorId = System.String;

    [RequireComponent(typeof(SpacePinPublisher))]
    public partial class SpacePinBinder : MonoBehaviour, IBinder
    {
        #region Inspector members
        [SerializeField]
        private string binderName = "default";

        public string BinderName
        {
            get { return binderName; }
            set
            {
                // mafish - assert value is valid name;

                // mafish - reset

                binderName = value;
            }
        }

        [SerializeField]
        private List<SpacePinASA> spacePins = new List<SpacePinASA>();

        [SerializeField]
        private float searchRadius = 25.0f; // meters

        public float SearchRadius { get { return searchRadius; } set { searchRadius = value; } }

        #endregion // Inspector members

        #region Public types

        // mafinc - interesting events
        // 1. We've sent a pose to the cloud, notify when cloud anchor has been published.
        //    Really? Why does the client want to know? 
        // 2. We've requested an anchor from the cloud by cloudId. Notify when recieved.
        //    Callback should include (frozen space?) pose of cloud anchor. Anything else?
        // 3. We've received a correction on a cloud anchor's pose. Notify of new pose.
        //
        // 

        #endregion // Public types

        #region Internal types

        private class SpacePinPegAndProps
        {
            public SpacePinASA spacePin;
            public LocalPegAndProperties pegAndProps;
        };

        #endregion // Internal types

        #region Internal members

        private readonly List<SpacePinCloudBinding> bindings = new List<SpacePinCloudBinding>();

        private IPublisher publisher = null;

        #endregion // Internal members

        #region Public APIs

        public static readonly string SpacePinIdKey = "SpacePinId";

        public bool IsReady
        {
            get { return publisher.IsReady; }
        }

        // mafinc - this is a temp hack and needs to go away.
        public IPublisher Publisher { get { return publisher; } }

        #region Create and maintain bindings between space pins and cloud anchors
        public IReadOnlyList<SpacePinCloudBinding> GetBindings()
        {
            return bindings;
        }

        /// <summary>
        /// Set the cloud anchor id associated with this space pin.
        /// </summary>
        /// <param name="spacePinId">Name of the space pin to be bound to this cloud id.</param>
        /// <param name="cloudAnchorId">Cloud id to be bound to the space pin.</param>
        /// <returns>False if space pin is unknown. Space pin must be registered via inspector or <see cref="AddSpacePin(SpacePin)"/> before being bound.</returns>
        /// <remarks>
        /// A space pin must be bound to a cloud anchor id before it can be downloaded.
        /// </remarks>
        public bool CreateBinding(string spacePinId, string cloudAnchorId)
        {
            int spacePinIdx = FindSpacePinById(spacePinId);
            if (spacePinIdx < 0)
            {
                Debug.LogError($"Trying to bind a space pin that Binder doesn't know about. Check inspector or add from script.");
                return false;
            }
            SetBinding(spacePinId, cloudAnchorId);
            return true;
        }

        public bool RemoveBinding(string spacePinId)
        {
            int bindingIdx = FindBindingBySpacePinId(spacePinId);
            if (bindingIdx < 0)
            {
                Debug.LogError($"Trying to remove unknown binding for space pin {spacePinId}");
                return false;
            }
            bindings.RemoveAt(bindingIdx);
            return true;
        }
        #endregion // Create and maintain bindings between space pins and cloud anchors

        #region Space pin list control from script
        public bool AddSpacePin(SpacePinASA spacePin)
        {
            // mafish - make sure it's not already in there.
            int idx = FindSpacePin(spacePin);
            if (idx <= 0)
            {
                spacePins.Add(spacePin);
                spacePin.Publisher = publisher;
                return true;
            }
            return false;
        }

        public bool RemoveSpacePin(string spacePinId)
        {
            int idx = FindSpacePinById(spacePinId);
            if (idx < 0)
            {
                Debug.Assert(FindBindingBySpacePinId(spacePinId) < 0, $"Space pin id {spacePinId} not found in list of space pins, but found in bindings");
                return false;
            }
            spacePins[idx].Publisher = null;
            spacePins.RemoveAt(idx);
            int bindingIdx = FindBindingBySpacePinId(spacePinId);
            if (bindingIdx >= 0)
            {
                bindings.RemoveAt(bindingIdx);
            }
            return true;
        }
        #endregion Space pin list control from script

        #region Publish to cloud
        /// <summary>
        /// For each unpublished space pin, create a new CloudSpatialAnchor.
        /// </summary>
        /// <returns>Task true if anything got uploaded.</returns>
        /// <remarks>
        /// Unpublished means it doesn't already have a CloudSpatialAnchor.
        /// </remarks>
        public async Task<bool> Publish()
        {
            //List<SpacePin> toPublish = FindUnpublishedSpacePins();
            //if (toPublish.Count == 0)
            //{
            //    // mafish - found nothing to publish. 
            //    // 3 possibilities:
            //    //   1. There are no SpacePins to publish. Probably an error.
            //    //   2. All SpacePins are already published. Might be okay, might be calling Publish too often.
            //    //   3. Neither of the above. Definitely unexpected error.

            //    return;
            //}
            //foreach(var spacePin in toPublish)
            //{
            //    Publish(spacePin);
            //}
            bool allSuccessful = true;
            foreach (var spacePin in spacePins)
            {
                if (IsReadyForPublish(spacePin))
                {
                    bool success = await Publish(spacePin);
                    if (!success)
                    {
                        Debug.LogError($"Failed to publish {spacePin.name}, continuing.");
                        allSuccessful = false;
                    }
                }
            }
            return allSuccessful;
        }

        public async Task<bool> Publish(SpacePinASA spacePin)
        {
            if (!publisher.IsReady)
            {
                // mafinc - Should we wait until it is ready? Maybe as a binder option?
                return false;
            }

            int idx = FindSpacePin(spacePin);
            if (idx < 0)
            {
                Debug.LogError($"Trying to publish unknown space pin. Must be added in inspector or AddSpacePin() first.");
                return false;
            }

            var obj = ExtractForPublisher(spacePin);
            if (obj == null)
            {
                return false;
            }
            CloudAnchorId cloudAnchorId = await publisher.Create(obj);
            if (string.IsNullOrEmpty(cloudAnchorId))
            {
                Debug.LogError($"Failed to create cloud anchor for {spacePin.name}");
                return false;
            }
            SetBinding(spacePin.name, cloudAnchorId);
            return true;
        }

        #endregion // Publish to cloud

        #region Download from cloud
        /// <summary>
        /// For each SpacePin which has a cloud anchor id, 
        /// kick off a request to asynchronously download the pose from the cloud.
        /// These will be processed and the SpacePins positioned as they come in.
        /// </summary>
        /// <remarks>
        /// The <see cref="anchoredPins"/> list has all the SpacePins we are aware of, either from inspector or added from script using <see cref="AddSpacePin(SpacePin)"/>.
        /// To be downloaded, the space pin must have a cloud anchor id (set from <see cref="CreateBinding(string, string)"/>.
        /// </remarks>
        public async Task<bool> Download()
        {
            bool allSuccessful = true;
            List<SpacePinPegAndProps> readObjects = new List<SpacePinPegAndProps>();
            foreach (var spacePin in spacePins)
            {
                int bindingIdx = FindBindingBySpacePinId(spacePin.name);
                if (bindingIdx >= 0)
                {
                    string cloudAnchorId = bindings[bindingIdx].cloudAnchorId;
                    var obj = await publisher.Read(cloudAnchorId);
                    if (obj == null)
                    {
                        allSuccessful = false;
                    }
                    else
                    {
                        Debug.Assert(obj.localPeg != null);
                        readObjects.Add(new SpacePinPegAndProps() { spacePin = spacePin, pegAndProps = obj });
                    }
                }
            }
            // mafinc - I don't think this delay is needed.
            int waitForAnchorsMS = 30;
            await Task.Delay(waitForAnchorsMS);
            var wltMgr = WorldLockingManager.GetInstance();
            Pose SpongyFromAnchor = wltMgr.AnchorManager.AnchorFromSpongy.Inverse();
            Pose LockedFromAnchor = wltMgr.LockedFromSpongy.Multiply(SpongyFromAnchor);
            foreach (var readObj in readObjects)
            {
                Pose lockedPose = LockedFromAnchor.Multiply(readObj.pegAndProps.localPeg.GlobalPose);
                readObj.spacePin.SetLockedPose(lockedPose);
                readObj.spacePin.SetLocalPeg(readObj.pegAndProps.localPeg);
            }
            return allSuccessful;
        }

        public async Task<bool> Search()
        {
            Dictionary<CloudAnchorId, LocalPegAndProperties> found = await publisher.Find(searchRadius);

            var wltMgr = WorldLockingManager.GetInstance();
            Pose SpongyFromAnchor = wltMgr.AnchorManager.AnchorFromSpongy.Inverse();
            Pose LockedFromAnchor = wltMgr.LockedFromSpongy.Multiply(SpongyFromAnchor);

            bool foundAny = false;
            foreach (var keyval in found)
            {
                string spacePinId = keyval.Value.properties[SpacePinIdKey];
                string cloudAnchorId = keyval.Key;
                var pegAndProps = keyval.Value;
                int idx = FindSpacePinById(spacePinId);
                if (idx >= 0)
                {
                    CreateBinding(spacePinId, cloudAnchorId);
                    foundAny = true;
                    SpacePinASA spacePin = spacePins[idx];

                    Pose lockedPose = LockedFromAnchor.Multiply(pegAndProps.localPeg.GlobalPose);
                    spacePin.SetLockedPose(lockedPose);
                    spacePin.SetLocalPeg(pegAndProps.localPeg);
                }
                else
                {
                    SimpleConsole.AddLine(8, $"Found anchor for unknown SpacePin={spacePinId}.");
                }
            }
            return foundAny;
        }
        #endregion // Download from cloud

        #region Cleanup

        public async Task<bool> Purge()
        {
            await publisher.PurgeArea(searchRadius);

            return true;
        }

        public async Task<bool> Clear()
        {
            foreach(var binding in bindings)
            {
                await publisher.Delete(binding.cloudAnchorId);
            }
            bindings.Clear();
            return true;
        }

        public void UnPin()
        {
            foreach (var spacePin in spacePins)
            {
                if (spacePin.PinActive)
                {
                    spacePin.Reset();
                }
            }
        }
        #endregion // Cleanup

        #endregion // Public APIs

        #region Internal 

        #endregion // Internal

        #region Unity
        private void Awake()
        {
            publisher = GetComponent<SpacePinPublisher>();
            // When Setup is complete, publisher.IsReady will be true.
            publisher.Setup();
            SetSpacePinsPublisher();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
        }
        #endregion // Unity

        #region Internal helpers

        private void SetSpacePinsPublisher()
        {
            foreach (var spacePin in spacePins)
            {
                spacePin.Publisher = publisher;
            }
        }

        private bool IsReadyForPublish(SpacePinASA spacePin)
        {
            if (spacePin == null)
            {
                SimpleConsole.AddLine(11, $"Getting null space pin to check ready for publish.");
                return false;
            }
            if (spacePin.Publisher != publisher)
            {
                SimpleConsole.AddLine(11, $"SpacePin={spacePin.name} has different publisher than binder={name}.");
                return false;
            }
            return spacePin.IsReadyForPublish;
        }

        private bool IsReadyForDownload(SpacePinASA spacePin)
        {
            int bindingIdx = FindBindingBySpacePinId(spacePin.name);
            if (bindingIdx < 0)
            {
                return false;
            }
            return true;
        }

        private void SetBinding(string spacePinId, CloudAnchorId cloudAnchorId)
        {
            Debug.Log($"Setting binding between sp={spacePinId} ca={cloudAnchorId}");
            int bindingIdx = FindBindingBySpacePinId(spacePinId);
            var binding = new SpacePinCloudBinding() { spacePinId = spacePinId, cloudAnchorId = cloudAnchorId };
            if (bindingIdx < 0)
            {
                Debug.Log($"Adding new binding sp={spacePinId} ca={cloudAnchorId}");
                bindings.Add(binding);
            }
            else
            {
                Debug.Log($"Updating existing binding sp={spacePinId} from ca={bindings[bindingIdx].cloudAnchorId} to ca={cloudAnchorId}");
                bindings[bindingIdx] = binding;
            }
        }

        private LocalPegAndProperties ExtractForPublisher(SpacePinASA spacePin)
        {
            if (!spacePin.IsReadyForPublish)
            {
                Debug.LogError($"Trying to publish a space pin with no native anchor. Place it first.");
                return null;
            }

            LocalPegAndProperties ret = new LocalPegAndProperties();
            ret.localPeg = spacePin.LocalPeg;
            ret.properties = spacePin.Properties;

            return ret;
        }

        private int FindSpacePinById(string spacePinId)
        {
            return FindByPredicate(spacePins, x => x.name == spacePinId);
        }

        private int FindSpacePin(SpacePin spacePin)
        {
            return FindByPredicate(spacePins, x => x == spacePin);
        }

        private int FindBindingByCloudAnchorId(string cloudAnchorId)
        {
            return FindByPredicate(bindings, x => x.cloudAnchorId == cloudAnchorId);
        }

        private int FindBindingBySpacePinId(string spacePinId)
        {
            return FindByPredicate(bindings, x => x.spacePinId == spacePinId);
        }

        private static int FindByPredicate<T>(List<T> searchList, Predicate<T> pred)
        {
            int idx = searchList.FindIndex(x => pred(x));
            return idx;
        }

        #endregion // Internal helpers
    }

}
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

#if UNITY_ANDROID
using UnityEngine.Android;
#endif // UNITY_ANDROID

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    using AnchorProperties = Dictionary<string, string>;
    using CloudAnchorId = System.String;
    using Readiness = IPublisher.Readiness;
    using ReadinessStatus = IPublisher.ReadinessStatus;

    public class SpacePinPublisher : MonoBehaviour, IPublisher
    {
        #region Inspector fields
        public List<string> BeaconUuids { get { return beaconUuids; } }

        [Tooltip("Enable coarse relocation")]
        [SerializeField]
        private bool coarseRelocationEnabled = true;
        /// <summary>
        /// Enable coarse relocation.
        /// </summary>
        /// <remarks>
        /// This must be set before calling Setup to have any effect.
        /// </remarks>
        public bool CoarseRelocationEnabled { get { return coarseRelocationEnabled; } set { coarseRelocationEnabled = value; } }

        [SerializeField]
        private bool coarseRelocPublishEnabled = true;

        public bool CoarseRelocPublishEnabled { get { return coarseRelocPublishEnabled; } set { coarseRelocPublishEnabled = value; } }

        [SerializeField]
        private bool crUseWifi = true;

        public bool CoarseRelocUseWifi { get { return crUseWifi; } set { crUseWifi = value; } }

        [SerializeField]
        private bool crUseGPS = true;

        public bool CoarseRelocUseGPS { get { return crUseGPS; } set { crUseGPS = value; } }

        [SerializeField]
        private List<string> beaconUuids = new List<string>();

        /// <summary>
        /// Whether use of bluetooth beacons is to be used in coarse relocation.
        /// </summary>
        /// <remarks>
        /// To enable use of bluetooth beacons, add some beacon uuids to BeaconUuids.
        /// </remarks>
        public bool CoarseRelocUseBluetooth { get { return BeaconUuids.Count > 0; } }

        [Tooltip("Transform to attach created anchors to. Should have identity global pose.")]
        [SerializeField]
        private Transform anchorsParent = null;

        /// <summary>
        /// Transform to attach created anchors to. Should have identity global pose.
        /// </summary>
        public Transform AnchorsParent { get { return anchorsParent; } set { anchorsParent = value; } }

        [SerializeField]
        private float maxSearchSeconds = 90.0f;

        public float MaxSearchSeconds { get { return maxSearchSeconds; } set { maxSearchSeconds = value; } }

        [SerializeField]
        private float maxWaitForMoreAnchorsSeconds = 0.25f;

        public float MaxWaitForMoreAnchorsSeconds { get { return maxWaitForMoreAnchorsSeconds; } set { maxWaitForMoreAnchorsSeconds = value; } }

        [SerializeField]
        private float minRecommendedForCreateProgress = 1.0f;

        public float MinRecommendedForCreateProgress { get { return minRecommendedForCreateProgress; } set { minRecommendedForCreateProgress = value; } }

        #endregion // Inspector fields

        #region Internal members

        private SpatialAnchorManager asaManager = null;

        protected AnchorLocateCriteria anchorLocateCriteria = null;

        private readonly List<AnchorRecord> records = new List<AnchorRecord>();
        private readonly List<AnchorLocatedEventArgs> locatedAnchors = new List<AnchorLocatedEventArgs>();

        private PlatformLocationProvider locationProvider = null;

        private Readiness readiness = Readiness.NotSetup;

        private int ConsoleHigh = 10;
        private int ConsoleMid = 8;
        private int ConsoleLow = 3;

        #endregion // Internal members

        #region Internal types
        private class AnchorRecord
        {
            public LocalPeg localPeg = null;
            public CloudSpatialAnchor cloudAnchor = null;
            public CloudAnchorId cloudAnchorId = null;

            public LocalPegAndProperties GetPegWithProperties()
            {
                Debug.Assert(localPeg != null, "Missing localPeg on AnchorRecord.");
                Debug.Assert(cloudAnchor != null, "Missing cloudAnchor on AnchorRecord.");
                LocalPegAndProperties pegAndProps = new LocalPegAndProperties();
                pegAndProps.localPeg = localPeg;
                pegAndProps.properties = cloudAnchor.AppProperties;

                return pegAndProps;
            }

            public static string DebugString(AnchorRecord record, string msg)
            {
                if (record == null)
                {
                    return $"{msg}: record is null";
                }
                Pose cloudPose = record.cloudAnchor == null ? Pose.identity : record.cloudAnchor.GetPose();
                return msg + "\n"
                    + $"nativeAnchor: {(record.localPeg.NativeAnchor == null ? "null" : record.localPeg.NativeAnchor.name)}\n"
                    + $"cloudAnchor: {(record.cloudAnchor == null ? "null" : record.cloudAnchor.Identifier)}\n"
                    + $"cloudAnchorId: {(!string.IsNullOrEmpty(record.cloudAnchorId) ? record.cloudAnchorId : "null")}\n"
                    + $"hanger: {(record.localPeg.anchorHanger == null ? "null" : record.localPeg.anchorHanger.name)}\n"
                    + $"pose: p={record.localPeg.GlobalPose.position.ToString("F3")} r={record.localPeg.GlobalPose.rotation.ToString("F3")}\n"
                    + $"cldp: p={cloudPose.position.ToString("F3")} r={cloudPose.rotation.ToString("F3")}";
            }

            public static void DebugLog(AnchorRecord record, string msg)
            {
                Debug.Log(DebugString(record, msg));
            }
        };

        private class LocalPeg : ILocalPeg
        {
            public GameObject anchorHanger;

            public NativeAnchor NativeAnchor
            {
                get 
                { 
                    return anchorHanger?.FindNativeAnchor(); 
                }
            }

            public string Name { get; set; }

            public bool IsReadyForPublish 
            {
                get
                {
                    return NativeAnchor != null;
                }
            }

            public Pose GlobalPose
            {
                get
                {
                    return anchorHanger.transform.GetGlobalPose();
                }
            }

        }

        #endregion // Internal types

        #region Public API

        public ReadinessStatus Status { get { return CheckReadiness(); } }

        private ReadinessStatus WrapReadiness()
        {
            float recommended = 0;
            float ready = 0;
            if (asaManager != null && asaManager.SessionStatus != null)
            {
                recommended = asaManager.SessionStatus.RecommendedForCreateProgress;
                ready = asaManager.SessionStatus.ReadyForCreateProgress;
            }
            var ret = new ReadinessStatus(readiness, recommended, ready);
            return ret;
        }
        private ReadinessStatus CheckReadiness()
        {
            if (asaManager == null)
            {
                if (readiness != IPublisher.Readiness.NoManager)
                {
                    readiness = IPublisher.Readiness.NoManager;
                    SimpleConsole.AddLine(ConsoleHigh, "Not ready: No ASA Manager.");
                }
                return WrapReadiness(); 
            }
            if (!asaManager.IsSessionStarted)
            {
                if (readiness != IPublisher.Readiness.Starting)
                {
                    readiness = IPublisher.Readiness.Starting;
                    SimpleConsole.AddLine(ConsoleHigh, "Not ready: Not started.");
                }
                return WrapReadiness();
            }
            if (IsBusy)
            {
                if (readiness != Readiness.Busy)
                {
                    readiness = Readiness.Busy;
                    SimpleConsole.AddLine(ConsoleHigh, $"Not ready: Busy on task [{busy}].");
                }
                return WrapReadiness();
            }
            if (CoarseRelocPublishEnabled && !IsReadyForCreate(asaManager))
            {
                if (readiness != IPublisher.Readiness.NotReadyToCreate)
                {
                    readiness = IPublisher.Readiness.NotReadyToCreate;
                    SimpleConsole.AddLine(ConsoleHigh, "Not ready: Not ready for create.");
                }
                return WrapReadiness();
            }
            if (!LocationReady(readiness))
            {
                if (readiness != IPublisher.Readiness.NotReadyToLocate)
                {
                    readiness = IPublisher.Readiness.NotReadyToLocate;
                    SimpleConsole.AddLine(ConsoleHigh, "Not ready: Location provider not ready.");
                }
                return WrapReadiness();
            }
            if (readiness != IPublisher.Readiness.Ready)
            {
                readiness = IPublisher.Readiness.Ready;
                SimpleConsole.AddLine(ConsoleHigh, "Ready.");
            }

            return WrapReadiness();
        }

        private bool IsReadyForCreate(SpatialAnchorManager mgr)
        {
            if (mgr == null)
            {
                return false;
            }
            if (mgr.SessionStatus.RecommendedForCreateProgress < minRecommendedForCreateProgress)
            {
                return false;
            }
            return true;
        }

        public async void Setup()
        {
            if (CoarseRelocPublishEnabled && !CoarseRelocationEnabled)
            {
                SimpleConsole.AddLine(ConsoleHigh, $"Coarse Reloc Curation enabled, but not Coarse Reloc. Disabling Coarse Reloc Curation.");
                CoarseRelocPublishEnabled = false;
            }
#if UNITY_ANDROID
            if (CoarseRelocationEnabled)
            {
                RequestLocationPermissions();
            }
#endif // UNITY_ANDROID

            Debug.Log($"Setting up publisher.");
            asaManager = GameObject.FindObjectOfType<SpatialAnchorManager>();
            if (asaManager == null)
            {
                Debug.LogError($"Can't find SpatialAnchorManager in scene.");
                return;
            }
            asaManager.AnchorLocated += OnAnchorLocated;
            asaManager.LocateAnchorsCompleted += OnAnchorLocateCompleted;

            Debug.Log($"To create session");
            await asaManager.CreateSessionAsync();

            int delayBeforeStartMS = 3000;
            await Task.Delay(delayBeforeStartMS);

            Debug.Log($"To start session");
            await asaManager.StartSessionAsync();

            asaManager.Session.OnLogDebug += OnASALog;
            asaManager.Session.Error += OnASAError;

            Debug.Log($"To create criteria");
            anchorLocateCriteria = new AnchorLocateCriteria();

            locationProvider = CreateLocationProvider();
            asaManager.Session.LocationProvider = locationProvider;

            CheckReadiness();
            SimpleConsole.AddLine(ConsoleHigh, $"Publisher setup complete S={asaManager.IsSessionStarted} Readiness={readiness}");
        }

        public async Task<ILocalPeg> CreateLocalPeg(string id, Pose lockedPose)
        {
            int waitForAnchor = 30;
            await Task.Delay(waitForAnchor);

            return InternalCreateLocalPeg(id, lockedPose);
        }

        public void ReleaseLocalPeg(ILocalPeg peg)
        {
            LocalPeg localPeg = peg as LocalPeg;
            if (localPeg == null)
            {
                throw new ArgumentException("ILocalPeg argument should be of type LocalPeg. Gotten from invalid source?");
            }
            GameObject.Destroy(localPeg.anchorHanger);
        }

        public async Task<CloudAnchorId> Create(LocalPegAndProperties pegAndProps)
        {
            if (AcquireBusy("Create"))
            {
                SimpleConsole.AddLine(ConsoleMid, $"Create for AH={pegAndProps.localPeg.Name}, {pegAndProps.properties.Count} props.");

                AnchorRecord record = new AnchorRecord();

                if (!pegAndProps.localPeg.IsReadyForPublish)
                {
                    throw new System.ArgumentNullException("pegAndProps.localPeg", "Local Peg not ready for create in Create");
                }

                LocalPeg localPeg = pegAndProps.localPeg as LocalPeg;
                if (localPeg == null)
                {
                    throw new System.ArgumentException("Invalid type of local peg", "pegAndProps.localPeg");
                }
                record.localPeg = localPeg;

                // mafinc - this is a design flaw. Now is not the time to create the native anchor,
                // as we are not necessarily in a good position to create a good native anchor.
                // We should create the native anchor when we are as near as possible to the corresponding pose.
                // Could we make the anchorHanger, with native anchor already on it, the input?
                // I hate pusing that responsibility out to the caller, but the caller is in a much better
                // position to make good anchors. For example, a SpacePin could create an anchor in SetLockedPose(),
                // under the assumption that the camera is in vicinity when SetLockedPose is invoked, either from 
                //
                // Refactored to accept anchorHanger as input parameter. Should we throw an invalid argument exception
                // if it doesn't already have a native anchor?

                // manipulation or QR code read or whatever.
                // Create the native anchor.

                AnchorRecord.DebugLog(record, "Pre ToCloud");

                // Now get the cloud spatial anchor
                record.cloudAnchor = await record.localPeg.NativeAnchor.ToCloud();

                foreach (var prop in pegAndProps.properties)
                {
                    record.cloudAnchor.AppProperties[prop.Key] = prop.Value;
                    SimpleConsole.AddLine(8, $"Add prop {prop.Key}: {prop.Value}");
                }

                AnchorRecord.DebugLog(record, "Past ToCloud");

                Debug.Log($"asaMgr:{(asaManager != null ? "valid" : "null")}, session:{(asaManager != null && asaManager.Session != null ? "valid" : "null")}");

                await asaManager.Session.CreateAnchorAsync(record.cloudAnchor);

                AnchorRecord.DebugLog(record, "Past CreateAnchorAsync");

                record.cloudAnchorId = record.cloudAnchor.Identifier;

                AddRecord(record.cloudAnchorId, record);

                SimpleConsole.AddLine(ConsoleMid, $"Created {pegAndProps.localPeg.Name} - {record.cloudAnchorId} at {record.localPeg.GlobalPose.position.ToString("F3")}");

                ReleaseBusy();
                return record.cloudAnchorId;
            }
            return null;
        }

        public async Task<LocalPegAndProperties> Read(CloudAnchorId cloudAnchorId)
        {
            if (AcquireBusy("Read"))
            {
                SimpleConsole.AddLine(ConsoleMid, $"Read CID={cloudAnchorId}");
                // mafinc - do we want an option here to force downloading, even if we already have it cached locally?
                AnchorRecord record = GetRecord(cloudAnchorId);
                Debug.Log($"GetRecord ca={cloudAnchorId}, record={(record == null ? "null" : record.localPeg.Name)}");
                if (record == null)
                {
                    Debug.Log($"Downloading record ca={cloudAnchorId}");
                    record = await DownloadRecord(cloudAnchorId);
                    if (record == null)
                    {
                        Debug.LogError($"Error downloading cloud anchor {cloudAnchorId}");
                        ReleaseBusy();
                        return null;
                    }
                    AddRecord(record.cloudAnchorId, record);
                }
                AnchorRecord.DebugLog(record, "Read: Got record");

                SimpleConsole.AddLine(ConsoleMid, $"Got record p={record.localPeg.GlobalPose.position.ToString("F3")}");

                
                var ret = record.GetPegWithProperties();
                ReleaseBusy();
                return ret;
            }
            return null;
        }

        public async Task<CloudAnchorId> Modify(CloudAnchorId cloudAnchorId, LocalPegAndProperties peg)
        {
            /// This might be more efficiently implemented with ASA update API.
            await Delete(cloudAnchorId);

            return await Create(peg);
        }

        public async Task Delete(string cloudAnchorId)
        {
            if (AcquireBusy("Delete"))
            {
                await DeleteById(cloudAnchorId);

                ReleaseBusy();
            }
        }

        public async Task<Dictionary<CloudAnchorId, LocalPegAndProperties>> Find(float radiusFromDevice)
        {
            if (AcquireBusy("Find"))
            {
                var found = await SearchArea(radiusFromDevice);

                ReleaseBusy();
                return found;
            }
            return null;
        }

        public async Task PurgeArea(float radius)
        {
            if (AcquireBusy("Purge"))
            {
                SimpleConsole.AddLine(ConsoleMid, $"Purging area of radius {radius} meters.");

                var dict = await SearchArea(radius);

                if (dict != null)
                {
                    SimpleConsole.AddLine(ConsoleMid, $"Found {dict.Count} anchors to delete.");

                    foreach (var keyval in dict)
                    {
                        string cloudAnchorId = keyval.Key;
                        SimpleConsole.AddLine(ConsoleMid, $"Deleting {cloudAnchorId}");

                        await DeleteById(cloudAnchorId);
                    }
                }
                else
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Search area failed!");
                }

                SimpleConsole.AddLine(ConsoleMid, "Purge finished.");
                ReleaseBusy();
            }
        }
        #endregion // Public API

        #region Internal implementations
        private async Task<AnchorRecord> DownloadRecord(CloudAnchorId cloudAnchorId)
        {
            Debug.Log($"Criteria.Identifiers to [{cloudAnchorId}]");
            anchorLocateCriteria.NearAnchor = new NearAnchorCriteria();
            anchorLocateCriteria.Identifiers = new CloudAnchorId[] { cloudAnchorId };

            SimpleConsole.AddLine(ConsoleMid, $"Crit get id: m={(int)anchorLocateCriteria.RequestedCategories} cache={!anchorLocateCriteria.BypassCache}");

            var watcher = asaManager.Session.CreateWatcher(anchorLocateCriteria);

            Debug.Log($"Got watcher, start waiting");

            AnchorRecord record = null;
            bool waiting = true;
            bool haveAnchor = false;
            while (waiting)
            {
                int DownloadCheckDelayMS = 100;
                await Task.Delay(DownloadCheckDelayMS);
                lock (locatedAnchors)
                {
                    if (locatedAnchors.Count > 0)
                    {
                        Debug.Log($"Got {locatedAnchors.Count} located anchors");
                        int idx = locatedAnchors.FindIndex(x => x.Identifier == cloudAnchorId);
                        if (idx >= 0)
                        {
                            Debug.Log($"Found located anchor {cloudAnchorId}, status={locatedAnchors[idx].Status}");
                            if (locatedAnchors[idx].Status == LocateAnchorStatus.Located)
                            {
                                record = new AnchorRecord();
                                record.cloudAnchor = locatedAnchors[idx].Anchor;
                                record.cloudAnchorId = locatedAnchors[idx].Identifier;
                                record = RecordFromCloud(record);
                            }
                            locatedAnchors.RemoveAt(idx);
                            haveAnchor = true;
                            SimpleConsole.AddLine(8, $"Have anchor {cloudAnchorId}");
                        }
                    }
                    waiting = !haveAnchor;
                }
            }
            if (record != null)
            {
                int anchorWaitMS = 100;
                AnchorRecord.DebugLog(record, $"Waiting {anchorWaitMS}ms from frame={Time.frameCount} to give new anchor time to fix itself.");
                await Task.Delay(anchorWaitMS);
                AnchorRecord.DebugLog(record, $"Finished waiting, frame={Time.frameCount}");
            }
            return record;
        }

        private async Task<Dictionary<CloudAnchorId, LocalPegAndProperties>> SearchArea(float radiusFromDevice)
        {
            if (locationProvider == null)
            {
                SimpleConsole.AddLine(11, $"Trying to search for records but location provider is null.");
                return null;
            }
            List<AnchorRecord> locatedRecords = await SearchForRecords(radiusFromDevice);

            SimpleConsole.AddLine(8, $"Found {locatedRecords.Count} records.");
            Dictionary<CloudAnchorId, LocalPegAndProperties> found = new Dictionary<CloudAnchorId, LocalPegAndProperties>();
            foreach (var record in locatedRecords)
            {
                AddRecord(record.cloudAnchorId, record);
                var obj = record.GetPegWithProperties();
                found[record.cloudAnchorId] = obj;
            }

            return found;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        /// <remarks>
        /// We initiate a search, and then at some point we need to know to stop waiting. 
        /// The LocateAnchorsCompleted event seems just the thing, but it doesn't seem to ever fire
        /// for the NearDevice search. (It does for looking up by id, but then we already know whether
        /// all queries have come in yet or not.)
        /// It isn't documented whether all anchors are guaranteed to come in at once (same frame) or not,
        /// so we assume not. So the basic idea is that when we get a batch of anchors in, we start a timer,
        /// and if no other anchors come in before the timer goes off, we give up. If any anchors do come in,
        /// we restart the timer.
        /// </remarks>
        private async Task<List<AnchorRecord>> SearchForRecords(float radius)
        {
            SimpleConsole.AddLine(ConsoleMid, $"Criteria to search radius={radius}");
            anchorLocateCriteria.NearAnchor = null;
            anchorLocateCriteria.Identifiers = new CloudAnchorId[0];

            anchorLocateCriteria.NearDevice = new NearDeviceCriteria();
            anchorLocateCriteria.NearDevice.DistanceInMeters = radius;
            anchorLocateCriteria.NearDevice.MaxResultCount = 35;

            SimpleConsole.AddLine(ConsoleMid, $"Crit radius: m={(int)anchorLocateCriteria.RequestedCategories} cache={!anchorLocateCriteria.BypassCache}");

            var watcher = asaManager.Session.CreateWatcher(anchorLocateCriteria);

            Debug.Log($"Got watcher, start waiting");

            double startTime = Time.timeAsDouble;
            List<AnchorRecord> locatedRecords = new List<AnchorRecord>();
            bool haveAnchors = false;
            bool waiting = true;
            while (waiting)
            {
                // If any records are found, then we give up waiting for more after waitForMoreAnchorsTimeoutMS.
                int DownloadCheckDelayMS = 100;
                await Task.Delay(DownloadCheckDelayMS);
                lock (locatedAnchors)
                {
                    if (locatedAnchors.Count > 0)
                    {
                        Debug.Log($"Got {locatedAnchors.Count} located anchors");
                        foreach (var located in locatedAnchors)
                        {
                            SimpleConsole.AddLine(ConsoleMid, $"Found located anchor {located.Identifier}, status={located.Status}");
                            if (located.Status == LocateAnchorStatus.Located)
                            {
                                AnchorRecord record = new AnchorRecord();
                                record.cloudAnchor = located.Anchor;
                                record.cloudAnchorId = located.Identifier;
                                record = RecordFromCloud(record);
                                locatedRecords.Add(record);
                            }
                        }
                        locatedAnchors.Clear();
                        haveAnchors = true;
                    }
                    else
                    {
                        // We have some anchors, but didn't get any more while waiting, so give up and go with what we have.
                        waiting = !haveAnchors;
                    }
                }
                double timeSearching = Time.timeAsDouble - startTime;
                if (haveAnchors)
                {
                    int waitForMoreAnchorsTimeoutMS = (int)(MaxWaitForMoreAnchorsSeconds * 1000.0f + 0.5f);
                    await Task.Delay(waitForMoreAnchorsTimeoutMS);
                }
                else if (timeSearching > MaxSearchSeconds)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Searched {timeSearching}s,found no anchors, giving up.");
                    waiting = false;
                }
            }
            watcher.Stop();
            if (locatedRecords.Count > 0)
            {
                int anchorWaitMS = 100;
                SimpleConsole.AddLine(ConsoleMid, $"Waiting {anchorWaitMS}ms from frame={Time.frameCount} to give new anchors time to fix.");
                await Task.Delay(anchorWaitMS);
                SimpleConsole.AddLine(ConsoleMid, $"Finished waiting, frame={Time.frameCount}");
            }
            return locatedRecords;
        }

        private async Task DeleteById(string cloudAnchorId)
        {
            SimpleConsole.AddLine(ConsoleMid, $"Deleting {cloudAnchorId}");
            AnchorRecord record = DeleteRecord(cloudAnchorId);
            if (record == null)
            {
                SimpleConsole.AddLine(11, $"No loaded record found for {cloudAnchorId}, downloading.");
                record = await DownloadRecord(cloudAnchorId);
                if (record == null)
                {
                    SimpleConsole.AddLine(11, $"Failed to download {cloudAnchorId}, not deleted.");
                    return;
                }
            }
            Debug.Assert(record.cloudAnchor != null, $"Trying to un-publish an anchor that isn't published: {record.cloudAnchorId}");
            if (record.cloudAnchor != null)
            {
                try
                {
                    await asaManager.Session.DeleteAnchorAsync(record.cloudAnchor);
                    SimpleConsole.AddLine(ConsoleMid, $"{cloudAnchorId} Deleted");
                }
                catch (Exception e)
                {
                    SimpleConsole.AddLine(11, $"Tried but failed to delete {cloudAnchorId}, {e.Message}");
                }
            }
        }


        #endregion // Internal implementations

        #region Internal helpers

        private ILocalPeg InternalCreateLocalPeg(string id, Pose lockedPose)
        {
            LocalPeg peg = new LocalPeg();
            peg.Name = id;

            peg.anchorHanger = new GameObject(id);
            peg.anchorHanger.transform.SetParent(AnchorsParent, false);

            var wltMgr = WorldLockingManager.GetInstance();
            Pose anchorPose = wltMgr.AnchorManager.AnchorFromSpongy.Multiply(wltMgr.SpongyFromLocked).Multiply(lockedPose);
            peg.anchorHanger.transform.SetGlobalPose(anchorPose);
            peg.anchorHanger.CreateNativeAnchor();

            return peg;
        }


        private bool AddRecord(CloudAnchorId id, AnchorRecord record)
        {
            Debug.Assert(id == record.cloudAnchorId, $"Adding record under inconsistent id {id} vs {record.cloudAnchorId}");
            int idx = records.FindIndex(x => x.cloudAnchorId == record.cloudAnchorId);
            if (idx < 0)
            {
                SimpleConsole.AddLine(ConsoleLow, $"Adding record ah={record.localPeg.Name} ca={id} ");
                records.Add(record);
                return true;
            }
            SimpleConsole.AddLine(ConsoleLow, $"Updating record ah={record.localPeg.Name} ca={id} ");
            ReleaseLocalPeg(records[idx].localPeg);
            records[idx] = record;
            return false;
        }

        private AnchorRecord GetRecord(CloudAnchorId id)
        {
            int idx = records.FindIndex(x => x.cloudAnchorId == id);
            if (idx >= 0)
            {
                return records[idx];
            }
            return null;
        }

        private AnchorRecord DeleteRecord(CloudAnchorId id)
        {
            AnchorRecord ret = null;
            int idx = records.FindIndex(x => x.cloudAnchorId == id);
            if (idx >= 0)
            {
                ret = records[idx];
                ReleaseLocalPeg(ret.localPeg);
                records.RemoveAt(idx);
            }
            return ret;
        }


        private AnchorRecord RecordFromCloud(AnchorRecord record)
        {
            Debug.Assert(record.cloudAnchor != null, $"Trying to create native resources from a null cloud anchor");
            record.localPeg = InternalCreateLocalPeg(record.cloudAnchorId, record.cloudAnchor.GetPose()) as LocalPeg;
            Debug.Log($"RecordFromCloud: ah={record.localPeg.GlobalPose.ToString("F3")} ca={record.cloudAnchor.GetPose().ToString("F3")}");
            SimpleConsole.AddLine(ConsoleMid, $"Got record={record.cloudAnchorId} with {record.cloudAnchor.AppProperties.Count} properties.");
            foreach (var prop in record.cloudAnchor.AppProperties)
            {
                SimpleConsole.AddLine(ConsoleMid, $"Prop: {prop.Key}: {prop.Value}");
            }
            return record;
        }

        #endregion // Internal helpers

        #region ASA events

        //  Rather than locking the list, we could put the Add action onto the main thread. 
        private void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            UnityDispatcher.InvokeOnAppThread(() =>
                SimpleConsole.AddLine(ConsoleHigh,
                    $"OnAnchorLocated: {args.Status}, {args.Identifier}, {args.Anchor?.Identifier}, {args.Anchor?.GetPose().ToString("F3")}"
                ));
            lock (locatedAnchors)
            {
                locatedAnchors.Add(args);
            }
        }

        private void OnAnchorLocateCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            UnityDispatcher.InvokeOnAppThread(() =>
                SimpleConsole.AddLine(ConsoleHigh,
                    $"OnAnchorLocateCompleted: {args.Watcher.Identifier} cancelled={args.Cancelled}"
                ));
        }
        private void OnASALog(object sender, OnLogDebugEventArgs args)
        {
            SimpleConsole.AddLine(ConsoleLow, args.Message);
        }

        private void OnASAError(object sender, SessionErrorEventArgs args)
        {
            SimpleConsole.AddLine(ConsoleHigh, args.ErrorMessage);
        }

        #endregion // ASA events

        #region Setup helpers
        private PlatformLocationProvider CreateLocationProvider()
        {
            Debug.Log($"To create location provider");

            if (!CoarseRelocationEnabled)
            {
                SimpleConsole.AddLine(8, $"Coarse relocation is not enabled!");
                return null;
            }

            PlatformLocationProvider provider = new PlatformLocationProvider();
            // Allow GPS
            provider.Sensors.GeoLocationEnabled = CoarseRelocUseGPS;

            // Allow WiFi scanning
            provider.Sensors.WifiEnabled = CoarseRelocUseWifi;

            // Allow a set of known BLE beacons
            provider.Sensors.BluetoothEnabled = (beaconUuids.Count > 0);
            // mafinc - todo, add api for adding list of blutooth beacon uuids.
            provider.Sensors.KnownBeaconProximityUuids = beaconUuids.ToArray();

            return provider;
        }

        private bool LocationReady(IPublisher.Readiness status)
        {
            // If locationProvider is null, we aren't using location provider, so don't need to wait on it. I.e. ready.
            if (locationProvider == null)
            {
                return true;
            }
            if (CoarseRelocUseGPS && locationProvider.GeoLocationStatus == GeoLocationStatusResult.Available)
            {
                if (readiness == IPublisher.Readiness.NotReadyToLocate)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Ready: GeoLocationStatus={locationProvider.GeoLocationStatus}");
                }
                return true;
            }
            if (CoarseRelocUseWifi && locationProvider.WifiStatus == WifiStatusResult.Available)
            {
                if (readiness == IPublisher.Readiness.NotReadyToLocate)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Ready: WifiStatus={locationProvider.WifiStatus}");
                }
                return true;
            }
            if (CoarseRelocUseBluetooth && locationProvider.BluetoothStatus == BluetoothStatusResult.Available)
            {
                if (readiness == IPublisher.Readiness.NotReadyToLocate)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Ready: BluetoothStatus={locationProvider.BluetoothStatus}");
                }
                return true;
            }
            if (readiness != IPublisher.Readiness.NotReadyToLocate)
            {
                SimpleConsole.AddLine(ConsoleHigh, $"Not Ready: GeoLocationStatus={locationProvider.GeoLocationStatus} {CoarseRelocUseGPS}");
                SimpleConsole.AddLine(ConsoleHigh, $"Not Ready: WifiStatus={locationProvider.WifiStatus} {CoarseRelocUseWifi}");
                SimpleConsole.AddLine(ConsoleHigh, $"Not Ready: BluetoothStatus={locationProvider.BluetoothStatus} {CoarseRelocUseBluetooth}");
            }
            return false;
        }

        private string busy = null;

        private bool IsBusy { get { return busy != null; } }

        private bool AcquireBusy(string msg)
        {
            Debug.Assert(msg != null);
            if (busy != null)
            {
                SimpleConsole.AddLine(ConsoleHigh, $"{msg} failed because IPublisher already busy.");
                return false;
            }
            busy = msg;
            return true;
        }
        private void ReleaseBusy()
        {
            Debug.Assert(busy != null);
            busy = null;
        }
        #endregion // Setup helpers

        #region Awful stuff

#if UNITY_ANDROID
        private static readonly string[] androidPermissions = new string[]
            {
                Permission.CoarseLocation,
                Permission.FineLocation,
                "android.permission.ACCESS_WIFI_STATE",
                "android.permission.CHANGE_WIFI_STATE",
                "android.permission.BLUETOOTH",
                "android.permission.BLUETOOTH_ADMIN"
            };

        const string androidWifiAccessPermission = "android.permission.ACCESS_WIFI_STATE";
        const string androidWifiChangePermission = "android.permission.CHANGE_WIFI_STATE";
        const string androidBluetoothPermission = "android.permission.BLUETOOTH";
        const string androidBluetoothAdminPermission = "android.permission.BLUETOOTH_ADMIN";

        private static bool RequestLocationPermissions()
        {
            bool haveAll = true;
            for (int i = 0; i < androidPermissions.Length; ++i)
            {
                if (!RequestPermissionIfNotGiven(androidPermissions[i]))
                {
                    haveAll = false;
                }
            }
            return haveAll;
        }

        private static bool RequestPermissionIfNotGiven(string permission)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                Permission.RequestUserPermission(permission);
            }
            SimpleConsole.AddLine(8, $"{permission} {(Permission.HasUserAuthorizedPermission(permission) ? "granted" : "denied")}");
            return Permission.HasUserAuthorizedPermission(permission);
        }
#endif
        #endregion // Awful stuff
    }
}

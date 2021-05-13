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

    public class SpacePinPublisher : IPublisher
    {
        #region Internal members

        private SpatialAnchorManager asaManager = null;

        protected AnchorLocateCriteria anchorLocateCriteria = null;

        private readonly List<AnchorRecord> records = new List<AnchorRecord>();
        private readonly List<AnchorLocatedEventArgs> locatedAnchors = new List<AnchorLocatedEventArgs>();

        private PlatformLocationProvider locationProvider = null;
        private readonly List<string> beaconUuids = new List<string>();

        private int ConsoleHigh = 10;
        private int ConsoleMid = 8;
        private int ConsoleLow = 3;

        private Transform anchorsParent = null;

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
                LocalPegAndProperties peg = new LocalPegAndProperties();
                peg.localPeg = localPeg;
                peg.properties = cloudAnchor.AppProperties;

                return peg;
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

        public Transform AnchorsParent { get { return anchorsParent; } set { anchorsParent = value; } }

        /// <summary>
        /// Enable coarse relocation.
        /// </summary>
        /// <remarks>
        /// This must be set before calling Setup to have any effect.
        /// </remarks>
        public bool CoarseRelocationEnabled { get; set; } = true;

        private enum NotReadyReason
        {
            Invalid,
            NoManager,
            NotStarted,
            NotReadyForCreate,
            NotReadyForLocate,
            Ready
        };
        private NotReadyReason readyReason = NotReadyReason.Invalid;
        public bool IsReady
        {
            get
            {
                if (asaManager == null)
                {
                    if (readyReason != NotReadyReason.NoManager)
                    {
                        readyReason = NotReadyReason.NoManager;
                        SimpleConsole.AddLine(ConsoleHigh, "Not ready: No ASA Manager.");
                    }
                    return false;
                }
                if (!asaManager.IsSessionStarted)
                {
                    if (readyReason != NotReadyReason.NotStarted)
                    {
                        readyReason = NotReadyReason.NotStarted;
                        SimpleConsole.AddLine(ConsoleHigh, "Not ready: Not started.");
                    }
                    return false;
                }
                if (!asaManager.IsReadyForCreate)
                {
                    if (readyReason != NotReadyReason.NotReadyForCreate)
                    {
                        readyReason = NotReadyReason.NotReadyForCreate;
                        SimpleConsole.AddLine(ConsoleHigh, "Not ready: Not ready for create.");
                    }
                    return false;
                }
                if (!LocationReady(readyReason))
                {
                    if (readyReason != NotReadyReason.NotReadyForLocate)
                    {
                        readyReason = NotReadyReason.NotReadyForLocate;
                        SimpleConsole.AddLine(ConsoleHigh, "Not ready: Location provider not ready.");
                    }
                    return false;
                }
                if (readyReason != NotReadyReason.Ready)
                {
                    readyReason = NotReadyReason.Ready;
                    SimpleConsole.AddLine(ConsoleHigh, "Ready.");
                }
                return true;
            }
        }

        public async void Setup()
        {
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

            SimpleConsole.AddLine(ConsoleHigh, $"Publisher setup complete S={asaManager.IsSessionStarted}");
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

        public async Task<CloudAnchorId> Create(LocalPegAndProperties peg)
        {
            SimpleConsole.AddLine(ConsoleMid, $"Create for AH={peg.localPeg.Name}, {peg.properties.Count} props.");

            AnchorRecord record = new AnchorRecord();

            if (!peg.localPeg.IsReadyForPublish)
            {
                throw new System.ArgumentNullException("peg.localPeg", "Local Peg not ready for create in Create");
            }

            LocalPeg localPeg = peg.localPeg as LocalPeg;
            if (localPeg == null)
            {
                throw new System.ArgumentException("Invalid type of local peg", "peg.localPeg");
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

            foreach (var prop in peg.properties)
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

            SimpleConsole.AddLine(ConsoleMid, $"Created {peg.localPeg.Name} - {record.cloudAnchorId} at {record.localPeg.GlobalPose.position.ToString("F3")}");

            return record.cloudAnchorId;
        }

        public async Task<LocalPegAndProperties> Read(CloudAnchorId cloudAnchorId)
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
                    return null;
                }
                AddRecord(record.cloudAnchorId, record);
            }
            AnchorRecord.DebugLog(record, "Read: Got record");

            SimpleConsole.AddLine(ConsoleMid, $"Got record p={record.localPeg.GlobalPose.position.ToString("F3")}");

            return record.GetPegWithProperties();
        }

        public async Task<CloudAnchorId> Update(CloudAnchorId cloudAnchorId, LocalPegAndProperties peg)
        {
            /// This might be more efficiently implemented with ASA update API.
            await Delete(cloudAnchorId);

            return await Create(peg);
        }

        public async Task Delete(string cloudAnchorId)
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

        public async Task<Dictionary<CloudAnchorId, LocalPegAndProperties>> Find(float radiusFromDevice)
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

        public async Task PurgeArea(float radius)
        {
            SimpleConsole.AddLine(ConsoleMid, $"Purging area of radius {radius} meters.");

            var dict = await Find(radius);

            SimpleConsole.AddLine(ConsoleMid, $"Found {dict.Count} anchors to delete.");

            foreach (var keyval in dict)
            {
                string cloudAnchorId = keyval.Key;
                SimpleConsole.AddLine(ConsoleMid, $"Deleting {cloudAnchorId}");

                await Delete(cloudAnchorId);
            }

            SimpleConsole.AddLine(ConsoleMid, "Purge finished.");
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

            List<AnchorRecord> locatedRecords = new List<AnchorRecord>();
            bool haveAnchors = false;
            bool waiting = true;
            while (waiting)
            {
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
                if (haveAnchors)
                {
                    int waitForMoreAnchorsTimeoutMS = 1000;
                    await Task.Delay(waitForMoreAnchorsTimeoutMS);
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
            Debug.Log($"Adding record ah={record.localPeg.Name} ca={id} ");
            Debug.Assert(id == record.cloudAnchorId, $"Adding record under inconsistent id {id} vs {record.cloudAnchorId}");
            int idx = records.FindIndex(x => x.cloudAnchorId == record.cloudAnchorId);
            if (idx < 0)
            {
                records.Add(record);
                return true;
            }
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
            provider.Sensors.GeoLocationEnabled = true;

            // Allow WiFi scanning
            provider.Sensors.WifiEnabled = true;

            // Allow a set of known BLE beacons
            provider.Sensors.BluetoothEnabled = (beaconUuids.Count > 0);
            // mafinc - todo, add api for adding list of blutooth beacon uuids.
            provider.Sensors.KnownBeaconProximityUuids = beaconUuids.ToArray();

            return provider;
        }

        private bool LocationReady(NotReadyReason readyReason)
        {
            // If locationProvider is null, we aren't using location provider, so don't need to wait on it. I.e. ready.
            if (locationProvider == null)
            {
                return true;
            }
            if (locationProvider.GeoLocationStatus == GeoLocationStatusResult.Available)
            {
                if (readyReason == NotReadyReason.NotReadyForLocate)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Ready: GeoLocationStatus={locationProvider.GeoLocationStatus}");
                }
                return true;
            }
            if (locationProvider.WifiStatus == WifiStatusResult.Available)
            {
                if (readyReason == NotReadyReason.NotReadyForLocate)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Ready: WifiStatus={locationProvider.WifiStatus}");
                }
                return true;
            }
            if (locationProvider.BluetoothStatus == BluetoothStatusResult.Available)
            {
                if (readyReason == NotReadyReason.NotReadyForLocate)
                {
                    SimpleConsole.AddLine(ConsoleHigh, $"Ready: BluetoothStatus={locationProvider.BluetoothStatus}");
                }
                return true;
            }
            if (readyReason != NotReadyReason.NotReadyForLocate)
            {
                SimpleConsole.AddLine(ConsoleHigh, $"Not Ready: GeoLocationStatus={locationProvider.GeoLocationStatus}");
                SimpleConsole.AddLine(ConsoleHigh, $"Not Ready: WifiStatus={locationProvider.WifiStatus}");
                SimpleConsole.AddLine(ConsoleHigh, $"Not Ready: BluetoothStatus={locationProvider.BluetoothStatus}");
            }
            return false;
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

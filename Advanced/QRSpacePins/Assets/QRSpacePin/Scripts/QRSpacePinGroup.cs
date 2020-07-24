// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;
using Microsoft.MixedReality.QR;

namespace Microsoft.MixedReality.WorldLocking.Samples.Advanced.QRSpacePins
{
    /// <summary>
    /// A group of space pins with locations fed by reading QR codes placed in the physical environment.
    /// </summary>
    [RequireComponent(typeof(QRCodeMiniManager))]
    public class QRSpacePinGroup : MonoBehaviour
    {
        /// <summary>
        /// An orienter to infer orientation from position of pins. Shared over all pins.
        /// </summary>
        private IOrienter orienter;

        [SerializeField]
        [Tooltip("An orienter instance in the scene. If unset, one will be created.")]
        private Orienter sharedOrienter = null;

        /// <summary>
        /// An orienter instance in the scene. If unset, one will be created.
        /// </summary>
        public Orienter SharedOrienter { get { return sharedOrienter; } set { sharedOrienter = value; orienter = value; } }

        [SerializeField]
        [Tooltip("Optional visual to draw at QR code position when detected.")]
        private GameObject markerHighlightPrefab;

        /// <summary>
        /// Optional visual to draw at QR code position when detected.
        /// </summary>
        public GameObject MarkerHighlightPrefab { get { return markerHighlightPrefab; } set { markerHighlightPrefab = value; } }

        /// <summary>
        /// Whether the enumeration completed event has been encountered yet.
        /// </summary>
        /// <remarks>
        /// Added, Updated, and Removed events before enumerationFinished is true are cached,
        /// not seen in the current session. These are ignored in favor of the SpacePin persistence
        /// system instead.
        /// </remarks>
        private bool enumerationFinished = false;

        private static readonly int trace = 0;
        private static readonly int log = 5;
        // Error level log currently unused.
        //private static readonly int error = 10;

        /// <summary>
        /// A collection of everything needed to set a space pin from a detected QR code.
        /// </summary>
        private class SpacePinPackage
        {
            /// <summary>
            /// The space pin to generate and maintain.
            /// </summary>
            public SpacePinOrientable spacePin = null;

            /// <summary>
            /// Optional visual to instantiate at QR code's position.
            /// </summary>
            private GameObject highlightPrefab = null;

            /// <summary>
            /// Instantiated visual for the QR code's detected position and orientation.
            /// </summary>
            public GameObject highlightProxy = null;

            /// <summary>
            /// Helper class to transform QR code's pose into Unity's global space.
            /// </summary>
            public QRSpatialCoord coordinateSystem = null;

            /// <summary>
            /// Size of the detected QR code. Only used for visualization.
            /// </summary>
            private float sizeMeters = 1.0f;

            /// <summary>
            /// The locked pose last sent to WLT space pin. 
            /// </summary>
            /// <remarks>
            /// This is only used to detect when the read QR position has changed enough to resubmit.
            /// </remarks>
            private Pose lastLockedPose = Pose.identity;

            /// <summary>
            /// Whether the QR code pose has been submitted to the space pin yet.
            /// </summary>
            private bool isSet = false;

            /// <summary>
            /// Create a new space pin package.
            /// </summary>
            /// <param name="owner">The owning space pin group.</param>
            /// <param name="virtualObject">Corresponding virtual object (for pose) in the scene.</param>
            /// <returns>The created package.</returns>
            /// <remarks>
            /// The created space pin package is ready to deploy, but currently idle.
            /// </remarks>
            public static SpacePinPackage Create(QRSpacePinGroup owner, Transform virtualObject)
            {
                SimpleConsole.AddLine(log, $"CreatePinPackage on {virtualObject.name}");
                SpacePinPackage package = new SpacePinPackage();
                package.spacePin = virtualObject.gameObject.AddComponent<SpacePinOrientable>();
                package.spacePin.Orienter = owner.orienter;
                package.highlightProxy = null;
                package.highlightPrefab = owner.markerHighlightPrefab;

                package.coordinateSystem = new QRSpatialCoord();

                return package;
            }

            /// <summary>
            /// Release all resources. Package is unusable after Release.
            /// </summary>
            public void Release()
            {
                SimpleConsole.AddLine(log, $"Release SpacePin {spacePin.name}");
                Destroy(spacePin);
                spacePin = null;
                Destroy(highlightProxy);
                highlightProxy = null;
            }

            /// <summary>
            /// Reset package to initial state. If space pin has been committed, it will be rescinded.
            /// </summary>
            public void Reset()
            {
                SimpleConsole.AddLine(log, $"Reset SpacePin {spacePin.name}");
                spacePin.Reset();
                Destroy(highlightProxy);
                highlightProxy = null;
                isSet = false;
            }

            /// <summary>
            /// Attempt to set a space pin from the QR code.
            /// </summary>
            /// <param name="qrCode">The source QR code.</param>
            /// <returns>True if a space pin was set from the current data.</returns>
            /// <remarks>
            /// Returning false does not necessarily mean an error occurred. For example, if the space pin
            /// has already been set from the given QR code, and the location hasn't changed, no action
            /// will be taken and the return value will be false. Or if the coordinate system is unable
            /// to resolve the transform to global space, again the return will be false, indicating
            /// trying again later. 
            /// </remarks>
            public bool Update(QRCode qrCode)
            {
                SimpleConsole.AddLine(trace, $"Update SpacePin {(coordinateSystem == null ? "null" : coordinateSystem.SpatialNodeId.ToString())}");
                coordinateSystem.SpatialNodeId = qrCode.SpatialGraphNodeId;
                sizeMeters = qrCode.PhysicalSideLength;
                Pose spongyPose;
                if (!coordinateSystem.ComputePose(out spongyPose))
                {
                    return false;
                }
                Pose frozenPose = WorldLockingManager.GetInstance().FrozenFromSpongy.Multiply(spongyPose);
                return UpdatePose(frozenPose);
            }

            /// <summary>
            /// Given a new frozen pose, send it to SpacePin system if appropriate.
            /// </summary>
            /// <param name="frozenPose">New frozen space pose.</param>
            /// <returns>True if the pose was pushed to the SpacePin system.</returns>
            private bool UpdatePose(Pose frozenPose)
            {
                SimpleConsole.AddLine(trace, "UpdatePose");
                bool didCommit = false;
                var wltMgr = WorldLockingManager.GetInstance();
                Pose lockedPose = wltMgr.LockedFromFrozen.Multiply(frozenPose);
                if (NeedCommit(lockedPose))
                {
                    didCommit = CommitPose(frozenPose, lockedPose);
                }
                else if (highlightProxy != null)
                {
                    if (!highlightProxy.activeSelf)
                    {
                        // Proxy has deactivated itself at end of animation, go ahead and destroy it.
                        Destroy(highlightProxy);
                        highlightProxy = null;
                    }
                }
                return didCommit;
            }

            /// <summary>
            /// Commit the pose to the SpacePin system, deploying the highlight marker if one is specified.
            /// </summary>
            /// <param name="frozenPose">New pose in frozen space.</param>
            /// <param name="lockedPose">New pose in locked space.</param>
            /// <returns>True if pose successfully committed to SpacePin system.</returns>
            private bool CommitPose(Pose frozenPose, Pose lockedPose)
            {
                SimpleConsole.AddLine(trace, $"Commit to {spacePin.name} F:{frozenPose} L:{lockedPose} S:{spacePin.transform.GetGlobalPose()}");
                spacePin.SetFrozenPose(frozenPose);

                DeployProxy();

                isSet = true;
                lastLockedPose = lockedPose;

                SimpleConsole.AddLine(trace, "Deployed");
                return true;
            }

            /// <summary>
            /// If a prefab for the highlight marker has been specified, instantiate it where the QR code was seen.
            /// </summary>
            /// <param name="frozenPose">Pose in frozen space where the qr code was seen.</param>
            /// <returns>True if the visual was deployed.</returns>
            private bool DeployProxy()
            {
                if (highlightProxy != null)
                {
                    Destroy(highlightProxy);
                }
                if (highlightPrefab != null)
                {
                    /// Note the assumption that the QR proxy is a box that needs to be
                    /// sized and offset to fit the read QR data.
                    Vector3 scale = new Vector3(sizeMeters, sizeMeters, sizeMeters * 0.1f);
                    Vector3 offset = scale * 0.5f;
                    highlightProxy = Instantiate(highlightPrefab, spacePin.transform);
                    highlightProxy.transform.localScale = scale;
                    highlightProxy.transform.localPosition = offset;
                    highlightProxy.transform.localRotation = Quaternion.identity;
                }

                return highlightProxy != null;
            }

            /// <summary>
            /// Determine if the new pose should be forwarded to the SpacePin system.
            /// </summary>
            /// <param name="lockedPose">The pose to test.</param>
            /// <returns>True if sending the new pose is indicated.</returns>
            /// <remarks>
            /// If the pose hasn't been sent yet, it will always be indicated to send.
            /// It is unusual that the position of the QR code as measured by the system changes
            /// significantly enough to be worth resending. It usually only occurs when the first 
            /// reading was faulty (e.g. during a rapid head move).
            /// </remarks>
            private bool NeedCommit(Pose lockedPose)
            {
                if (!isSet)
                {
                    SimpleConsole.AddLine(log, "Need commit because unset.");
                    return true;
                }
                float RefreshThreshold = 0.01f; // one cm?
                float distance = Vector3.Distance(lockedPose.position, lastLockedPose.position);
                if ( distance > RefreshThreshold)
                {
                    SimpleConsole.AddLine(log, $"Need commit because new distance {distance}");
                    return true;
                }
                SimpleConsole.AddLine(trace, $"No commit");
                return false;
            }
        };

        /// <summary>
        /// The mini manager will issue qr code events from the main thread.
        /// </summary>
        private QRCodeMiniManager miniManager;

        /// <summary>
        /// One pin created for each spacePinPoint.
        /// </summary>
        private readonly List<SpacePinPackage> spacePins = new List<SpacePinPackage>();

        [SerializeField]
        [Tooltip("The virtual poses in the scene to be matched with the poses of the QR codes in the physical world.")]
        private List<Transform> virtualMarkers = new List<Transform>();

        /// <summary>
        /// The virtual poses in the scene to be matched with the poses of the QR codes in the physical world.
        /// </summary>
        public List<Transform> VirtualMarkers { get { return virtualMarkers; } set { virtualMarkers = value; } }

        /// <summary>
        /// Clear back to the state before any QR codes had been detected.
        /// </summary>
        public void Reset()
        {
            for(int i = 0; i < spacePins.Count; ++i)
            {
                spacePins[i].Reset();
            }
        }

        /// <summary>
        /// Ensure all required components exist and cache references where appropriate.
        /// </summary>
        private void CheckComponents()
        {
            if (miniManager == null)
            {
                miniManager = GetComponent<QRCodeMiniManager>();
            }
            if (orienter == null)
            {
                if (sharedOrienter == null)
                {
                    orienter = gameObject.AddComponent<Orienter>();
                }
                else
                {
                    orienter = sharedOrienter;
                }
            }
        }

        /// <summary>
        /// Prepare to activate.
        /// </summary>
        private void Start()
        {
            CheckComponents();
        }

        /// <summary>
        /// Become active.
        /// </summary>
        private void OnEnable()
        {
            CheckComponents();

            SetUpCallbacks();
            SetUpSpacePins();
            SimpleConsole.AddLine(trace, "QRSpacePin Enabled");
        }

        /// <summary>
        /// Create the space pins in an idle state.
        /// </summary>
        /// <remarks>
        /// Pins won't become active until the corresponding QR code is seen.
        /// </remarks>
        private void SetUpSpacePins()
        {
            DestroySpacePins();
            for (int i = 0; i < VirtualMarkers.Count; ++i)
            {
                spacePins.Add(SpacePinPackage.Create(this, VirtualMarkers[i]));
            }
        }

        /// <summary>
        /// Free all resources created to support the space pins.
        /// </summary>
        /// <remarks>
        /// After calling this, the group is no longer useable.
        /// </remarks>
        private void DestroySpacePins()
        {
            if (spacePins.Count > 0)
            {
                Debug.Assert(spacePins.Count == VirtualMarkers.Count, "Bad state, there should be no space pins, or one for each marker");
                for (int i = 0; i < spacePins.Count; ++i)
                {
                    spacePins[i].Release();
                }
                spacePins.Clear();
            }
        }

        /// <summary>
        /// Go into dormant state. Can be revived later.
        /// </summary>
        private void OnDisable()
        {
            TearDownCallbacks();
        }

        /// <summary>
        /// Release resources in prepartion for destruction. Cannot be revived.
        /// </summary>
        private void OnDestroy()
        {
            DestroySpacePins();
        }

        /// <summary>
        /// Register for callbacks on QR code events. These callbacks will happen on the main thread.
        /// </summary>
        private void SetUpCallbacks()
        {
            Debug.Assert(miniManager != null, "Expected required component QRCodeMiniManager");

            miniManager.OnQRAdded += OnQRCodeAdded;
            miniManager.OnQRUpdated += OnQRCodeUpdated;
            miniManager.OnQRRemoved += OnQRCodeRemoved;
            miniManager.OnQREnumerated += OnQRCodeEnumerated;
            SimpleConsole.AddLine(trace, "Callbacks SetUp");
        }

        /// <summary>
        /// Unregister from callbacks.
        /// </summary>
        private void TearDownCallbacks()
        {
            miniManager.OnQRAdded -= OnQRCodeAdded;
            miniManager.OnQRUpdated -= OnQRCodeUpdated;
            miniManager.OnQRRemoved -= OnQRCodeRemoved;
            miniManager.OnQREnumerated -= OnQRCodeEnumerated;
            miniManager = null;
            SimpleConsole.AddLine(trace, "Callbacks torn down");
        }

        /// <summary>
        /// Check the validity of an index into the space pin packages.
        /// </summary>
        /// <param name="idx">The index to check.</param>
        /// <returns>True if the index is valid.</returns>
        private bool QRCodeIndexValid(int idx)
        {
            return idx >= 0 && idx < spacePins.Count;
        }

        /// <summary>
        /// Process a newly added QR code.
        /// </summary>
        /// <param name="qrCode">The qr code to process.</param>
        private void OnQRCodeAdded(QRCode qrCode)
        {
            SimpleConsole.AddLine(trace, $"OnAdded {qrCode.Data}, enumerated {enumerationFinished}");
            if (enumerationFinished)
            {
                int idx = ExtractIndex(qrCode);
                if (!QRCodeIndexValid(idx))
                {
                    return;
                }
                spacePins[idx].Update(qrCode);
            }
        }

        /// <summary>
        /// Process a newly updated QR code.
        /// </summary>
        /// <param name="qrCode">The qr code to process.</param>
        private void OnQRCodeUpdated(QRCode qrCode)
        {
            SimpleConsole.AddLine(trace, $"OnAdded {qrCode.Data}, enumerated {enumerationFinished}");
            if (enumerationFinished)
            {
                int idx = ExtractIndex(qrCode);
                if (!QRCodeIndexValid(idx))
                {
                    return;
                }
                spacePins[idx].Update(qrCode);
            }
        }

        /// <summary>
        /// Process a newly removed QR code.
        /// </summary>
        /// <param name="qrCode">The qr code to process.</param>
        private void OnQRCodeRemoved(QRCode qrCode)
        {
            SimpleConsole.AddLine(trace, $"OnQRCodeRemoved {qrCode.Data}");
            int idx = ExtractIndex(qrCode);
            if (!QRCodeIndexValid(idx))
            {
                return;
            }
            spacePins[idx].Reset();
        }

        /// <summary>
        /// Process the enumeration completed event.
        /// </summary>
        /// <param name="qrCode"></param>
        private void OnQRCodeEnumerated(QRCode qrCode)
        {
            Debug.Assert(qrCode == null, "Dummy qrCode parameter should always be null");
            SimpleConsole.AddLine(trace, $"OnQRCodeEnumerated");
            enumerationFinished = true;
        }

        /// <summary>
        /// Extract a space pin index out of the qr code.
        /// </summary>
        /// <param name="qrCode"></param>
        /// <returns>Space pin index corresponding to this qr code, or -1 if there isn't one.</returns>
        private int ExtractIndex(QRCode qrCode)
        {
            string code = qrCode.Data;

            string[] tokens = code.Split(new string[] { "_" }, System.StringSplitOptions.None);

            int qrIndex = GetQRCodeIndex(tokens);
            if (!QRCodeIndexValid(qrIndex))
            {
                Debug.LogError($"Failed to parse id out of code {code}");
                return -1;
            }

            return qrIndex;
        }

        /// <summary>
        /// Extract the index from the qrCode data.
        /// </summary>
        /// <param name="codeSplitContents">The string to parse the index from.</param>
        /// <returns>The index of the corresponding space pin.</returns>
        /// <remarks>
        /// Assumes the index is embedded in the qr code's data as the last element
        /// in an underscore ('_') delimited string, e.g. "My_QRCode_03" would have
        /// index 3, and "My_QR_Code_03_Wrong" would generate an error.
        /// </remarks>
        private int GetQRCodeIndex(string[] codeSplitContents)
        {
            int qrIndex = -1;
            if (!int.TryParse(codeSplitContents[codeSplitContents.Length - 1], out qrIndex))
            {
                return -1;
            }
            qrIndex--;
            return qrIndex;
        }

    }
}
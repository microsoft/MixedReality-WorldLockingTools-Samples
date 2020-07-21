using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.WorldLocking.Tools;
using Microsoft.MixedReality.QR;

namespace Microsoft.MixedReality.WorldLocking.Samples.Advanced.QRSpacePins
{
    /// <summary>
    /// The QRCodeMiniManager is a simple wrapper around the Microsoft.MixedReality.QR.QRCodeWatcher,
    /// to reissue qr code events on the main thread, where they can be acted on safely.
    /// </summary>
    /// <remarks>
    /// The events are slightly simplified to the uniform pattern of the void QRCodeFunction(qrCode) defined here.
    /// No other interpretation or processing of the events is done. Events are received (on another thread) from
    /// the QRCodeWatcher, then reissued on the main thread in the next Update loop.
    /// Note that since they are received asynchronously, no exact guarantees can be made about the order in which
    /// they are called on the main thread. But in general (and in best effort) they will be called in the order
    /// in which the asynchronous events are dispatched, and on the next Update after the events are originally invoked.
    /// </remarks>
    public class QRCodeMiniManager : MonoBehaviour
    {
        /// <summary>
        /// QRCodeWatcher which will deliver notifications asynchronously.
        /// </summary>
        private QRCodeWatcher qrWatcher = null;
        
        /// <summary>
        /// Status of access as retrieved from user.
        /// </summary>
        private QRCodeWatcherAccessStatus accessStatus = QRCodeWatcherAccessStatus.UserPromptRequired;

        /// <summary>
        /// Whether QRCodeWatcher reports itself as supported.
        /// </summary>
        private bool isSupported = false;

        /// <summary>
        /// Get accessor for whether QRCodeWatcher reports as supported.
        /// </summary>
        public bool IsSupported => isSupported;

        /// <summary>
        /// Notification callback for a QRCode event.
        /// </summary>
        /// <param name="qrCode">The code generating the event.</param>
        /// <remarks>
        /// Note that for the enumeration complete event, qrCode parameter is always null.
        /// </remarks>
        public delegate void QRCodeFunction(QRCode qrCode);

        private QRCodeFunction onQRAdded;

        /// <summary>
        /// Callback when a new QR code is added.
        /// </summary>
        public QRCodeFunction OnQRAdded { get { return onQRAdded; } set { onQRAdded = value; } }

        private QRCodeFunction onQRUpdated;

        /// <summary>
        /// Callback when a previously added QR code is updated.
        /// </summary>
        public QRCodeFunction OnQRUpdated { get { return onQRUpdated; } set { onQRUpdated = value; } }

        private QRCodeFunction onQRRemoved;

        /// <summary>
        /// Callback when a previously added QR code is removed.
        /// </summary>
        public QRCodeFunction OnQRRemoved { get { return onQRRemoved; } set { onQRRemoved = value; } }

        private QRCodeFunction onQREnumerated;

        /// <summary>
        /// Callback when the enumeration is complete.
        /// </summary>
        /// <remarks>
        /// Cached QR codes will have Added and Updated events BEFORE the enumeration complete.
        /// Newly seen QR codes will only start to appear after the enumeration complete event.
        /// <see href="https://github.com/chgatla-microsoft/QRTracking/issues/2"/>
        /// </remarks>
        public QRCodeFunction OnQREnumerated { get { return onQREnumerated; } set { onQREnumerated = value; } }

        /// <summary>
        /// Events are stored in the PendingQRCode struct for re-issue on the main thread.
        /// </summary>
        /// <remarks>
        /// While more elegant mechanisms exist for accomplishing the same thing, the simplicity of
        /// this form provides great efficiency, especially for memory pressure.
        /// </remarks>
        private struct PendingQRCode
        {
            /// <summary>
            /// The four actions that can be taken, corresponding to the 4 subscribable delegates.
            /// </summary>
            public enum QRAction
            {
                Add,
                Update,
                Remove,
                Enumerated
            };

            /// <summary>
            /// The code which has triggered the event. For Enumerated action, qrCode will be null.
            /// </summary>
            public readonly QRCode qrCode;

            /// <summary>
            /// The type of event.
            /// </summary>
            public readonly QRAction qrAction;

            /// <summary>
            /// Constructor for immutable action.
            /// </summary>
            /// <param name="qrAction">Action to take.</param>
            /// <param name="qrCode">QR Code causing event.</param>
            public PendingQRCode(QRAction qrAction, QRCode qrCode)
            {
                this.qrAction = qrAction;
                this.qrCode = qrCode;
            }
        }
        /// <summary>
        /// Queue of qr code events to process next Update.
        /// </summary>
        private readonly Queue<PendingQRCode> pendingActions = new Queue<PendingQRCode>();

        /// <summary>
        /// SimpleConsole verbosity levels.
        /// </summary>
        private static readonly int trace = 0;
        private static readonly int log = 5;
        // Error level logs currently unused.
        //private static readonly int error = 10;

        /// <summary>
        /// Check existence of watcher and attempt to create if needed and appropriate.
        /// </summary>
        private void CheckQRCodeWatcher()
        {
            /// Several things must happen consecutively to create the working QRCodeWatcher.
            /// 1) Access to the camera must be requested.
            /// 2) Access must have been granted.
            /// 3) And of course the whole package must be supported.
            /// When all of those conditions have been met, then we can create the watcher.
            if (qrWatcher == null && IsSupported && accessStatus == QRCodeWatcherAccessStatus.Allowed)
            {
                SetUpQRWatcher();
            }
        }

        /// <summary>
        /// Create the QRCodeWatcher instance and register for the events to be transported to the main thread.
        /// </summary>
        private void SetUpQRWatcher()
        {
            try
            {
                qrWatcher = new QRCodeWatcher();
                qrWatcher.Added += OnQRCodeAddedEvent;
                qrWatcher.Updated += OnQRCodeUpdatedEvent;
                qrWatcher.Removed += OnQRCodeRemovedEvent;
                qrWatcher.EnumerationCompleted += OnQREnumerationEnded;
                qrWatcher.Start();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start QRCodeWatcher, error: {e.Message}");
            }
            SimpleConsole.AddLine(log, $"SetUpQRWatcher {(qrWatcher != null ? "Success" : "Failed")}");
        }

        /// <summary>
        /// Deregister from the QRCodeWatcher and shut down the instance.
        /// </summary>
        private void TearDownQRWatcher()
        {
            if (qrWatcher != null)
            {
                qrWatcher.Stop();
                qrWatcher.Added -= OnQRCodeAddedEvent;
                qrWatcher.Updated -= OnQRCodeUpdatedEvent;
                qrWatcher.Removed -= OnQRCodeRemovedEvent;
                qrWatcher.EnumerationCompleted -= OnQREnumerationEnded;
                qrWatcher = null;
            }
        }

        /// <summary>
        /// No current action needed on enable, since setup is deferred to Update().
        /// </summary>
        private void OnEnable()
        {
        }

        /// <summary>
        /// On disable, shutdown all resources. They may be recreated on demand if re-enabled.
        /// </summary>
        private void OnDisable()
        {
            TearDownQRWatcher();
        }

        /// <summary>
        /// Record whether the QRCodeWatcher reports itself as supported, and request access.
        /// </summary>
        private async void Start()
        {
            isSupported = QRCodeWatcher.IsSupported();
            var capabilityTask = QRCodeWatcher.RequestAccessAsync();
            accessStatus = await capabilityTask;
            SimpleConsole.AddLine(log, $"Requested caps, access: {accessStatus.ToString()}");
        }

        /// <summary>
        /// Lazily create qr code watcher resources if needed, then issue any queued events.
        /// </summary>
        private void Update()
        {
            CheckQRCodeWatcher();

            lock (pendingActions)
            {
                while (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();

                    switch (action.qrAction)
                    {
                        case PendingQRCode.QRAction.Add:
                            AddQRCode(action.qrCode);
                            break;

                        case PendingQRCode.QRAction.Update:
                            UpdateQRCode(action.qrCode);
                            break;

                        case PendingQRCode.QRAction.Remove:
                            RemoveQRCode(action.qrCode);
                            break;

                        case PendingQRCode.QRAction.Enumerated:
                            QREnumerationComplete();
                            break;

                        default:
                            Debug.Assert(false, "Unknown action type");
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Capture an Added event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="args">Args containing relevant QRCode.</param>
        private void OnQRCodeAddedEvent(object sender, QRCodeAddedEventArgs args)
        {
            SimpleConsole.AddLine(trace, $"Adding {args.Code.Data}");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Add, args.Code));
            }
        }

        /// <summary>
        /// Capture an Updated event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="args">Args containing relevant QRCode.</param>
        private void OnQRCodeUpdatedEvent(object sender, QRCodeUpdatedEventArgs args)
        {
            SimpleConsole.AddLine(trace, $"Updating {args.Code.Data}");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Update, args.Code));
            }
        }

        /// <summary>
        /// Capture a Removed event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="args">Args containing relevant QRCode.</param>
        private void OnQRCodeRemovedEvent(object sender, QRCodeRemovedEventArgs args)
        {
            SimpleConsole.AddLine(trace, $"Removing {args.Code.Data}");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Remove, args.Code));
            }
        }

        /// <summary>
        /// Capture the Enumeration Ended event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void OnQREnumerationEnded(object sender, object e)
        {
            SimpleConsole.AddLine(log, "Enumerated");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Enumerated, null));
            }
        }

        /// <summary>
        /// Invoke Added delegate for specified qrCode.
        /// </summary>
        /// <param name="qrCode">The relevant QRCode.</param>
        private void AddQRCode(QRCode qrCode)
        {
            SimpleConsole.AddLine(trace, $"Adding QR Code {qrCode.Data}");

            onQRAdded?.Invoke(qrCode);
        }

        /// <summary>
        /// Invoke Updated delegate for specified qrCode.
        /// </summary>
        /// <param name="qrCode">The relevant QRCode.</param>
        private void UpdateQRCode(QRCode qrCode)
        {
            SimpleConsole.AddLine(trace, $"Updating QR Code {qrCode.Data}");

            onQRUpdated?.Invoke(qrCode);
        }

        /// <summary>
        /// Invoke Removed delegate for specified qrCode.
        /// </summary>
        /// <param name="qrCode">The relevant QRCode.</param>
        private void RemoveQRCode(QRCode qrCode)
        {
            SimpleConsole.AddLine(trace, $"Removing QR Code {qrCode.Data}");

            onQRRemoved?.Invoke(qrCode);
        }

        /// <summary>
        /// Invoke Enumeration Complete delegate.
        /// </summary>
        private void QREnumerationComplete()
        {
            SimpleConsole.AddLine(trace, $"Enumeration of QR Codes complete.");

            onQREnumerated?.Invoke(null);
        }
    }
}
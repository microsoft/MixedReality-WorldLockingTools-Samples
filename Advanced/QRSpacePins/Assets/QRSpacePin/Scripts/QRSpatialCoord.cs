// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

using Microsoft.MixedReality.WorldLocking.Tools;

/// A note about the appearance of "global::Windows.Etc" here. This is to disambiguate between
/// Windows.Perception.Spatial, which we want, and Microsoft.Windows.Perception.Spatial, which we don't want.
/// Because this sample is in a Microsoft namespace (Microsoft.MixedReality.WorldLocking.Samples), 
/// it will attempt to bind to the latter (Microsoft.Windows) unless the former is explicitly
/// indicated (global::Windows).

#if WINDOWS_UWP
using global::Windows.Perception.Spatial;
#endif // WINDOWS_UWP

namespace Microsoft.MixedReality.WorldLocking.Samples.Advanced.QRSpacePins
{
    /// <summary>
    /// Wrapper class for SpatialCoordinateSystem.
    /// </summary>
    /// <remarks>
    /// Provides a transform of the QR code's pose into Spongy space.
    /// </remarks>
    public class QRSpatialCoord
    {

#if WINDOWS_UWP
        /// <summary>
        /// Coordinate system of the QR Code.
        /// </summary>
        private SpatialCoordinateSystem coordinateSystem = null;

        /// <summary>
        /// Root coordinate system, aka Spongy space.
        /// </summary>
        private SpatialCoordinateSystem rootCoordinateSystem = null;
#endif // WINDOWS_UWP

        /// <summary>
        /// Spatial node id for the QR code.
        /// </summary>
        private System.Guid spatialNodeId;

        /// <summary>
        /// Accessor for spatial node id.
        /// </summary>
        public System.Guid SpatialNodeId
        {
            get { return spatialNodeId; }
            set
            {
                if (spatialNodeId != value)
                {
                    spatialNodeId = value;
#if WINDOWS_UWP
                    coordinateSystem = null;
#endif // WINDOWS_UWP
                }
            }
        }

        /// <summary>
        /// SimpleConsole verbosity levels.
        /// </summary>
        private static readonly int trace = 0;
        // log level only used in UWP
#if WINDOWS_UWP
        private static readonly int log = 5;
#endif // WINDOWS_UWP
        // No error level logs currently used.
        //private static readonly int error = 10;

        /// <summary>
        /// The last computed pose.
        /// </summary>
        public Pose CurrentPose { get; private set; } = Pose.identity;

        /// <summary>
        /// Compute the head relative pose for the spatial node id.
        /// </summary>
        /// <param name="pose">If return value is true, the newly computed pose, else the last pose computed.</param>
        /// <returns>True if a new pose was successfully computed.</returns>
        /// <remarks>
        /// This ultimately relies on SpatialCoordinateSystem.TryGetTransformTo.
        /// TryGetTransformTo seems to fail for a while after the QR code is created. 
        /// Or maybe just spurious failure. Haven't found any documentation on behavior so far.
        /// Main thing is to be prepared for failure, and just try back until success.
        /// </remarks>
        public bool ComputePose(out Pose pose)
        {
            SimpleConsole.AddLine(trace, "ComputePose");
            if (CheckActive())
            {
                System.Numerics.Matrix4x4? newMatrix = GetNewMatrix();
                if (newMatrix != null)
                {
                    CurrentPose = AdjustNewMatrix(newMatrix.Value);
                    pose = CurrentPose;
                    return true;
                }
            }
            pose = CurrentPose;
            return false;
        }

        /// <summary>
        /// Attempt to retrieve the current transform matrix.
        /// </summary>
        /// <returns>Non-null matrix on success.</returns>
        private System.Numerics.Matrix4x4? GetNewMatrix()
        {
#if WINDOWS_UWP
            Debug.Assert(rootCoordinateSystem != null);

            // Get the relative transform from the unity origin
            System.Numerics.Matrix4x4? newMatrix = coordinateSystem.TryGetTransformTo(rootCoordinateSystem);

            SimpleConsole.AddLine(trace, $"Got new matrix {(newMatrix == null ? "null" : newMatrix.ToString())}");
            if (newMatrix == null)
            {
                SimpleConsole.AddLine(log, "Coord: Got null newMatrix");
            }
            return newMatrix;

#else // WINDOWS_UWP
            return null;
#endif // WINDOWS_UWP
        }

        /// <summary>
        /// Convert the retrieved matrix to Unity lefthanded pose convention.
        /// </summary>
        /// <param name="newMatrix">Matrix to convert.</param>
        /// <returns>Unity pose equivalent.</returns>
        /// <remarks>
        /// Note that any scale is discarded, returned pose is position+rotation only.
        /// </remarks>
        private Pose AdjustNewMatrix(System.Numerics.Matrix4x4 newMatrix)
        {
            // Convert from right to left coordinate system
            newMatrix.M13 = -newMatrix.M13;
            newMatrix.M23 = -newMatrix.M23;
            newMatrix.M43 = -newMatrix.M43;

            newMatrix.M31 = -newMatrix.M31;
            newMatrix.M32 = -newMatrix.M32;
            newMatrix.M34 = -newMatrix.M34;

            /// Decompose into position + rotation (scale is discarded).
            System.Numerics.Vector3 sysScale;
            System.Numerics.Quaternion sysRotation;
            System.Numerics.Vector3 sysPosition;

            System.Numerics.Matrix4x4.Decompose(newMatrix, out sysScale, out sysRotation, out sysPosition);
            Vector3 position = new Vector3(sysPosition.X, sysPosition.Y, sysPosition.Z);
            Quaternion rotation = new Quaternion(sysRotation.X, sysRotation.Y, sysRotation.Z, sysRotation.W);
            Pose pose = new Pose(position, rotation);

            SimpleConsole.AddLine(trace, $"Adjusted {pose}");

            return pose;
        }

        /// <summary>
        /// Check that WorldManager is active and internal setup is cached.
        /// </summary>
        /// <returns></returns>
        private bool CheckActive()
        {
#if WINDOWS_UWP
            if (UnityEngine.XR.WSA.WorldManager.state != UnityEngine.XR.WSA.PositionalLocatorState.Active)
            {
                return false;
            }

            if (!CheckCoordinateSystem())
            {
                return false;
            }
            return true;
#else // WINDOWS_UWP
            return false;
#endif // WINDOWS_UWP
        }

        /// <summary>
        /// Cache the coordinate system for the QR code's spatial node, and the root.
        /// </summary>
        /// <returns></returns>
        private bool CheckCoordinateSystem()
        {
#if WINDOWS_UWP
            if (coordinateSystem == null)
            {
                SimpleConsole.AddLine(trace, $"Creating coord for {spatialNodeId}");
                coordinateSystem = global::Windows.Perception.Spatial.Preview.SpatialGraphInteropPreview.CreateCoordinateSystemForNode(SpatialNodeId);
            }

            if (rootCoordinateSystem == null)
            {
                rootCoordinateSystem = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(
                    UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr()
                ) as SpatialCoordinateSystem;
                SimpleConsole.AddLine(trace, $"Getting root coordinate system {(rootCoordinateSystem == null ? "null" : "succeeded")}");
            }

            return coordinateSystem != null;
#else // WINDOWS_UWP
            return false;
#endif // WINDOWS_UWP
        }
    }
}
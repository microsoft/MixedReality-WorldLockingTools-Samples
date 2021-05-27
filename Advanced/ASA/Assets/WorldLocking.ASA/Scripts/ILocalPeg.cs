// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    public interface ILocalPeg
    {
        /// <summary>
        /// The name for this peg.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Is there enough information to publish this anchor to the cloud?
        /// </summary>
        public bool IsReadyForPublish { get; }

        /// <summary>
        /// The current global pose for the blob.
        /// </summary>
        public Pose GlobalPose { get; }
    }

    public class LocalPegAndProperties
    {
        public ILocalPeg localPeg;
        public IDictionary<string, string> properties;
    };

}
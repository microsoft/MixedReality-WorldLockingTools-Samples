// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    using CloudAnchorId = System.String;

    public class LocalPegAndProperties
    {
        public ILocalPeg localPeg;
        public IDictionary<string, string> properties;
    };

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// All poses are in Spongy space, that is the space of native anchors.
    /// </remarks>
    public interface IPublisher
    {
        public void Setup();

        public bool IsReady { get; }

        public Task<ILocalPeg> CreateLocalPeg(string id, Pose lockedPose);

        public void ReleaseLocalPeg(ILocalPeg peg);

        public Task<CloudAnchorId> Create(LocalPegAndProperties peg);

        public Task<LocalPegAndProperties> Read(CloudAnchorId cloudAnchorId);

        public Task<CloudAnchorId> Modify(CloudAnchorId cloudAnchorId, LocalPegAndProperties peg);

        public Task<Dictionary<CloudAnchorId, LocalPegAndProperties>> Find(float radiusFromDevice);

        public Task PurgeArea(float radius);

        public Task Delete(CloudAnchorId cloudAnchorId);
    };

}
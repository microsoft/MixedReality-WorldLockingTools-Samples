// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    public struct SpacePinCloudBinding
    {
        public string spacePinId;
        public string cloudAnchorId;
    };

    public interface IBinder
    {
        string BinderName { get; }

        IReadOnlyList<SpacePinCloudBinding> GetBindings();

        bool CreateBinding(string spacePinId, string cloudAnchorId);

        bool RemoveBinding(string spacePinId);

        Task<bool> Publish();

        Task<bool> Download();

        Task<bool> Search();

        Task<bool> Clear();
    }
}

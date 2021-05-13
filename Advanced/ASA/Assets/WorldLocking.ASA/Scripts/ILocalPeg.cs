// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    public interface ILocalPeg
    {
        public string Name { get; }

        public bool IsReadyForPublish { get; }

        public Pose GlobalPose { get; }
    }

}
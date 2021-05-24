// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{

    public interface IBindingOracle
    {
        string Name { get; }

        bool Put(IBinder binder);

        bool Get(IBinder binder);
    }
}
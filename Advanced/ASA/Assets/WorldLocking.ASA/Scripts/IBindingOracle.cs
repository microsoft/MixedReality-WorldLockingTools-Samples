
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{

    public interface IBindingOracle
    {

        bool Put(IBinder binder);

        bool Get(IBinder binder);
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{

    public class SpacePinBinderFile : MonoBehaviour, IBindingOracle
    {

        [SerializeField] 
        private string fileName = "BinderFile.txt";

        public string Name { get { return name; } }

        public bool Get(IBinder binder)
        {
            return Load(binder);
        }

        public bool Put(IBinder binder)
        {
            return Save(binder);
        }
        private bool Save(IBinder binder)
        {
            if (binder == null)
            {
                Debug.LogError($"{name} has no binder set");
                return false;
            }
            var bindings = binder.GetBindings();
            using (StreamWriter writer = new StreamWriter(GetFullPath()))
            {
                writer.WriteLine($"BinderName - {binder.BinderName}");
                foreach (var binding in bindings)
                {
                    writer.WriteLine($"{binding.spacePinId}, {binding.cloudAnchorId}");
                }
            }
            return true;
        }

        private bool Load(IBinder binder)
        {
            if (binder == null)
            {
                Debug.LogError($"{name} has no binder set");
                return false;
            }
            string fullPath = GetFullPath();
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"{name} can't find file {fullPath}");
                return false;
            }
            using (StreamReader reader = new StreamReader(GetFullPath()))
            {
                string binderName = reader.ReadLine(); // mafinc - do something with this?

                char[] separators = new char[] { ' ', ',' };
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
                    binder.CreateBinding(tokens[0], tokens[1]);
                }
            }
            return true;
        }

        private string GetFullPath()
        {
            string path = Application.persistentDataPath;

            string fullPath = Path.Combine(path, fileName);

            path = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return fullPath;
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.WorldLocking.ASA
{
    /// <summary>
    /// Simple implementation of an IBindingOracle interface, which puts and gets binding from file locally.
    /// </summary>
    /// <remarks>
    /// Note that this implements spatial persistence locally across sessions.
    /// Also, the file can be transferred from device to device, allowing shared spaces
    /// between HoloLens, Android, and iOS.
    /// </remarks>
    public class SpacePinBinderFile : MonoBehaviour, IBindingOracle
    {
        [Tooltip("The name of the file into which bindings will be written and from which they will be read.")]
        [SerializeField] 
        private string fileName = "BinderFile.txt";

        /// <summary>
        /// Name of this oracle.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Retrieve bindings from file and apply them to the input binder.
        /// </summary>
        /// <param name="binder">Binder to apply them to.</param>
        /// <returns>True on success.</returns>
        public bool Get(IBinder binder)
        {
            return Load(binder);
        }

        /// <summary>
        /// Pull bindings from the binder and save them to file.
        /// </summary>
        /// <param name="binder">Binder to pull from.</param>
        /// <returns>True on success.</returns>
        public bool Put(IBinder binder)
        {
            return Save(binder);
        }

        /// <summary>
        /// Implement Put().
        /// </summary>
        /// <param name="binder">Binder whose bindings are to be saved.</param>
        /// <returns>True on success.</returns>
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
                writer.WriteLine($"Binder Name - {binder.Name}");
                foreach (var binding in bindings)
                {
                    writer.WriteLine($"{binding.spacePinId}, {binding.cloudAnchorId}");
                }
            }
            return true;
        }

        /// <summary>
        /// Implement Get().
        /// </summary>
        /// <param name="binder">Binder to apply bindings to.</param>
        /// <returns>True on success.</returns>
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

        /// <summary>
        /// Convert fileName to a full path name.
        /// </summary>
        /// <returns>The full path name.</returns>
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
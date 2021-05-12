using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.WorldLocking.Tools;
using Microsoft.MixedReality.WorldLocking.ASA;

namespace WorldLocking.Trash
{
    public class ActionPublish : ActionCube
    {
        public SpacePinBinder binder;

        public SpacePinBinderFile binderFile;

        public float finishSeconds = 1.0f;
        
        private bool isReady = false;

        private void Start()
        {
            SetColors(Color.grey);
        }
        private void Update()
        {
            bool nowReady = binder.IsReady;
            if (nowReady != isReady)
            {
                if (nowReady)
                {
                    RestoreColors();
                }
                else
                {
                    SetColors(Color.white);
                }
                isReady = nowReady;
            }
        }
        public async void DoPublish()
        {
            SetColors(Color.black);

            SimpleConsole.AddLine(8, $"Publish cube, binder is {(binder == null ? "null" : binder.name)}");
            if (binder != null)
            {
                await binder.Publish();

                if (binderFile != null)
                {
                    SimpleConsole.AddLine(8, $"Putting to {binderFile.name}");
                    binderFile.Put(binder);
                }
            }


            await ChangeColorForSeconds(finishSeconds, Color.green);
        }

        public async void DoDownload()
        {
            SetColors(Color.black);

            SimpleConsole.AddLine(8, $"Download cube, binder is {(binder == null ? "null" : binder.name)}");
            if (binder != null)
            {
                if (binderFile != null)
                {
                    SimpleConsole.AddLine(8, $"Getting from {binderFile.name}");
                    binderFile.Get(binder);
                }
                SimpleConsole.AddLine(8, $"Starting download from {binder.name}");
                await binder.Download();
            }
            SimpleConsole.AddLine(8, $"Finished.");

            await ChangeColorForSeconds(finishSeconds, Color.green);
        }

        public async void DoSearch()
        {
            SetColors(Color.black);

            SimpleConsole.AddLine(8, $"Search cube, binder is {(binder == null ? "null" : binder.name)}");
            if (binder != null)
            {
                SimpleConsole.AddLine(8, $"Starting search from {binder.name}");
                await binder.Search();
            }
            SimpleConsole.AddLine(8, $"Finished.");

            await ChangeColorForSeconds(finishSeconds, Color.green);
        }

        public async void DoPurge()
        {
            SetColors(Color.black);

            SimpleConsole.AddLine(8, $"Purge cube, binder is {(binder == null ? "null" : binder.name)}");
            if (binder != null)
            {
                SimpleConsole.AddLine(8, $"Starting search from {binder.name}");
                await binder.Purge();
            }
            SimpleConsole.AddLine(8, $"Finished.");

            await ChangeColorForSeconds(finishSeconds, Color.green);
        }

        public async void DoClear()
        {
            SetColors(Color.black);

            SimpleConsole.AddLine(8, $"Clear cube, binder is {(binder == null ? "null" : binder.name)}");

            if (binder != null)
            {
                if (binderFile != null)
                {
                    SimpleConsole.AddLine(8, $"Getting from {binderFile.name}");
                    binderFile.Get(binder);
                }
                await binder.Clear();
            }

            await ChangeColorForSeconds(finishSeconds, Color.green);
        }
    }
}

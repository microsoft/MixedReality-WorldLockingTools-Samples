// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using GoogleARCore;

namespace ARCoreTest
{
    public class PlaceObject : MonoBehaviour
    {
        #region Inspector fields
        public GameObject prefabToPlace = null;

        public float prefabScale = 1.0f;
        #endregion // Inspector fields

        #region MonoBehavior
        // Start is called before the first frame update
        private void Start()
        {

        }

        // Update is called once per frame
        private void Update()
        {
            if (!CheckTracking())
            {
                return;
            }

            Vector2? screenPos = GetTap();
            if (screenPos == null)
            {
                return;
            }

            HandleTap(screenPos.Value);
        }
        #endregion // MonoBehavior

        #region Handle input
        private bool CheckTracking()
        {
            if (Session.Status != SessionStatus.Tracking)
            {
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
                return false;
            }
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            return true;
        }

        private Vector2? GetTap()
        {
            if (prefabToPlace == null)
            {
                Debug.Log("Missing prefab to place.");
                return null;
            }

            if (Input.touchCount < 1)
            {
                return null;
            }
            Touch touch = Input.GetTouch(0);
            Debug.Log($"Got {Input.touchCount} touches, and phase is {touch.phase}");
            if (touch.phase != TouchPhase.Began)
            {
                return null;
            }

            //if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            //{
            //    Debug.Log($"Ignoring touch on UX");
            //    return null;
            //}

            Debug.Log($"Found touch at {touch.position.ToString("F3")}");
            return new Vector2(touch.position.x, touch.position.y);
        }

        private void HandleTap(Vector2 screenPos)
        {
            TrackableHit hit;
            bool foundHit = false;
            foundHit = Frame.RaycastInstantPlacement(
                screenPos.x, screenPos.y, 1.0f, out hit);

            if (!foundHit)
            {
                Debug.Log("No hit on raycast.");
                return;
            }

            Debug.Log($"Placing {prefabToPlace.name} at {hit.Pose.position.ToString("F3")}");
            // Instantiate prefab at the hit pose.
            var gameObject = Instantiate(prefabToPlace, hit.Pose.position, hit.Pose.rotation);

        }
        #endregion // Handle input
    }
}
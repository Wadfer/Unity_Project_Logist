// Majority of custom gizmo is inspired by and paritaly sourced from: 
// https://forum.unity3d.com/threads/in-game-gizmo-handle-control.154948/

using System;
using UnityEngine;
using TBUnityLib.Generic;

namespace RuntimeGizmo
{
    [AddComponentMenu("")]
    public class GizmoHandle : MonoBehaviour
    {
        public Gizmo gizmo;

        public GameObject positionCap;
        public Material activeMaterial;
        public GizmoAxis gizmoAxis;
        public float mouseSensitivity = 10f;
        public float rotationSensitivity = 0f;
        public float scaleSensitivity = 0f;

        private bool activeHandle;
        private Vector3 gizmoMouseOffset;
        private Vector3 gizmoStartPosition;

        protected void Awake()
        {
        }

        public void OnMouseDown()
        {
            gizmoMouseOffset = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(gizmo.transform.position).z));
            gizmoStartPosition = gizmo.transform.position;

            gizmo.DeactivateHandles();
            SetActive(true);
        }

        public void OnMouseDrag()
        {
            var distanceToScreen = Camera.main.WorldToScreenPoint(gizmo.transform.position).z;
            if (!activeHandle) return;
            var posMove = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceToScreen));
            var roadCenter = Vector3.zero;
            Transform obj = gizmo.selectedObject;
            switch (gizmoAxis)
            {
                case GizmoAxis.X:
                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                    obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, posMove.z - ((gizmoMouseOffset.z - gizmoStartPosition.z) - roadCenter.z));
                    break;
                case GizmoAxis.Y:
                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                    obj.transform.position = new Vector3(obj.transform.position.x, posMove.y - ((gizmoMouseOffset.y - gizmoStartPosition.y) - roadCenter.y), obj.transform.position.z);
                    break;
                case GizmoAxis.Z:
                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                    obj.transform.position = new Vector3((posMove.x - (gizmoMouseOffset.x - gizmoStartPosition.x) + roadCenter.x), obj.transform.position.y, obj.transform.position.z);
                    break;
                case GizmoAxis.Center:
                    if (obj.GetComponent<MagnetRoads.MagnetRoad>()) roadCenter = obj.transform.position - obj.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f);
                    obj.transform.position = new Vector3(posMove.x + roadCenter.x, obj.transform.position.y, posMove.z + roadCenter.z);
                    break;
            }
        }

        public void SetActive(bool active)
        {
            if (active)
            {
                activeHandle = true;
            }
            else
            {
                activeHandle = false;
            }
        }

    }
}
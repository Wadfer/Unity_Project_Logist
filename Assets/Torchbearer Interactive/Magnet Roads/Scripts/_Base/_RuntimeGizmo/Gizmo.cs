// Majority of custom gizmo is inspired by and paritaly sourced from: 
// https://forum.unity3d.com/threads/in-game-gizmo-handle-control.154948/

using UnityEngine;
using TBUnityLib.Generic;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RuntimeGizmo
{

    public enum GizmoAxis
    {
        Center,
        X,
        Y,
        Z
    }

    [AddComponentMenu("")]
    public class Gizmo : MonoBehaviour
    {
        public GizmoHandle axisCenter;
        public GizmoHandle axisX;
        public GizmoHandle axisY;
        public GizmoHandle axisZ;
        public Transform selectedObject;
        public Vector3 center;
        public bool visible;
        public float defaultDistance = 3.2f;
        public float scaleFactor = 0.2f;

        private Vector3 localScale;
        private Transform gizmoTransform;

        protected void Awake()
        {
            visible = false;
            Hide();

            // set the axis start type
            axisCenter.gizmoAxis = GizmoAxis.Center;
            axisCenter.gizmo = this;
            axisX.gizmoAxis = GizmoAxis.X;
            axisX.gizmo = this;
            axisY.gizmoAxis = GizmoAxis.Y;
            axisY.gizmo = this;
            axisZ.gizmoAxis = GizmoAxis.Z;
            axisZ.gizmo = this;

            gizmoTransform = transform;
            localScale = gizmoTransform.localScale;
            selectedObject = null;
        }

        protected void Update()
        {
            if (visible)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ClearSelection();
                    Hide();
                }
            }
            if (!selectedObject) return;
            // Scale based on distance from the camera
            var distance = Vector3.Distance(gizmoTransform.position, FindObjectOfType<Camera>().transform.position);
            var scale = (distance - defaultDistance) * scaleFactor;
            gizmoTransform.localScale = new Vector3(localScale.x + scale, localScale.y + scale, localScale.z + scale);

            // Move the gizmo to the center of our parent
            UpdateCenter();
            gizmoTransform.position = center;
        }

        public void ClearSelection()
        {
            if (selectedObject)
                selectedObject.gameObject.GetComponent<GizmoSelectable>().Unselect();
            selectedObject = null;
            center = Vector3.zero;
        }

        public void UpdateCenter()
        {
            if (!selectedObject) return;
                center = selectedObject.GetComponent<MagnetRoads.MagnetRoad>() ? selectedObject.GetComponent<MagnetRoads.MagnetRoad>().splineSource.GetPoint(.5f) : selectedObject.position;
        }

        public void SelectObject(Transform parent)
        {
            if(selectedObject)
                selectedObject.gameObject.GetComponent<GizmoSelectable>().Unselect();
            selectedObject = parent;
            UpdateCenter();
        }

        public void ActivateAxis(GizmoAxis axis)
        {
            switch (axis)
            {
                case GizmoAxis.Center:
                    axisCenter.SetActive(true);
                    break;
                case GizmoAxis.X:
                    axisX.SetActive(true);
                    break;
                case GizmoAxis.Y:
                    axisY.SetActive(true);
                    break;
                case GizmoAxis.Z:
                    axisZ.SetActive(true);
                    break;
            }

        }

        public void DeactivateAxis(GizmoAxis axis)
        {
            switch (axis)
            {
                case GizmoAxis.Center:
                    axisCenter.SetActive(false);
                    break;
                case GizmoAxis.X:
                    axisX.SetActive(false);
                    break;
                case GizmoAxis.Y:
                    axisY.SetActive(false);
                    break;
                case GizmoAxis.Z:
                    axisZ.SetActive(false);
                    break;
            }

        }

        public void DeactivateHandles()
        {
            axisCenter.SetActive(false);
            axisX.SetActive(false);
            axisY.SetActive(false);
            axisZ.SetActive(false);
        }

        public void Show()
        {
            Helper.SetActiveRecursively(gameObject, true);
            visible = true;
        }

        public void Hide()
        {
            Helper.SetActiveRecursively(gameObject, false);
            gameObject.SetActive(true);
            visible = false;
        }
    }
}

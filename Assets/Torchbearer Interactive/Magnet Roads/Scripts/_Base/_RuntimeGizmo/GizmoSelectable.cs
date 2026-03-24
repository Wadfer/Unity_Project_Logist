// Majority of custom gizmo is inspired by and paritally from: 
// https://forum.unity3d.com/threads/in-game-gizmo-handle-control.154948/

// Add this class to objects you want to be able to move in-game!

// WARNING! Owing to the implementation of MagnetRoads we advise that you
// do not apply this script to any objects outside of magnet roads as the
// selectabilty of objects is directly tied into the functionality of the
// runtime magnet snapping. 

using UnityEngine;
using System.Collections; 

namespace RuntimeGizmo
{
    [AddComponentMenu("")]
    public class GizmoSelectable : MonoBehaviour
    {
        private static Gizmo sGizmoControl;

        protected void Start()
        {
            if (FindObjectOfType<Gizmo>())
            {
                sGizmoControl = FindObjectOfType<Gizmo>();
            }
            else
            {
                if (GameObject.Find("__RuntimeGizmo")) DestroyImmediate(GameObject.Find("__RuntimeGizmo"));
                var temp = Instantiate(Resources.Load("_Base/_RuntimeGizmo/Gizmo") as GameObject);
                sGizmoControl = temp.GetComponent<Gizmo>();
                sGizmoControl.transform.rotation = Quaternion.Euler(0,90,0);
                sGizmoControl.name = "__RuntimeGizmo";
            }
            var collider = GetComponent<SphereCollider>();
            if (collider)
                collider.radius = 1f;
         }

        protected void OnMouseDown()
        {
            if (sGizmoControl == null) return;
            sGizmoControl.Show();
            sGizmoControl.SelectObject(transform);
            gameObject.layer = 2;
        }

        public void Unselect()
        {
            gameObject.layer = 0;
        }
    }
}
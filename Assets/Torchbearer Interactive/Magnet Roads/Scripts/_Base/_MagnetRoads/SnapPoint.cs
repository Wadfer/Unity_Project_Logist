// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// This class is used for the sole purpose of indicating, in-editor, the locations of snappable points
// on existing SplineRoads

using UnityEngine;
using System; 

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagnetRoads
{
    [ExecuteInEditMode] [SelectionBase] [AddComponentMenu("")]
    public class SnapPoint : MonoBehaviour
    {
        [HideInInspector] [SerializeField]
        private PointEnd pointEnd;
        [HideInInspector] [SerializeField]
        private float roadWidth;
        private GameObject inEditorMagnetPoint;
        private bool isSetup = false;
        public bool isEditableAtRuntime;

        public PointEnd PointType
        {
            get
            {
                return pointEnd;
            }
        }

        public enum PointEnd
        {
            Positive,
            Negative,
            Bipolar
        }

        public void SetUp(PointEnd pointType, float width)
        {
            pointEnd = pointType;
            roadWidth = width;
            isSetup = false;
        }

        protected void Update()
        {
            // Handle the spawning & clearing of the runtime snap points
            if (Application.isPlaying && isEditableAtRuntime)
            {
                if (isSetup) return;

                // Set up in-editor handle if not already done
                if (!inEditorMagnetPoint) inEditorMagnetPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (inEditorMagnetPoint.transform.parent != transform) inEditorMagnetPoint.transform.parent = transform;
                inEditorMagnetPoint.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                inEditorMagnetPoint.name = "__RuntimeSnapNode";

                // Handle node type styling
                if (!inEditorMagnetPoint.GetComponent<Renderer>().enabled) inEditorMagnetPoint.GetComponent<Renderer>().enabled = true;
                switch (PointType)
                {
                    case PointEnd.Positive:
                        inEditorMagnetPoint.transform.localScale = new Vector3(roadWidth / 1.4f, roadWidth / 24, roadWidth / 1.4f);
                        inEditorMagnetPoint.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/Positive");
                        break;

                    case PointEnd.Negative:
                        inEditorMagnetPoint.transform.localScale = new Vector3(roadWidth / 1.4f - 0.01f, roadWidth / 24, roadWidth / 1.4f - 0.01f);
                        inEditorMagnetPoint.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/Negative");
                        break;

                    case PointEnd.Bipolar:
                        inEditorMagnetPoint.transform.localScale = new Vector3(roadWidth / 1.4f - 0.02f, roadWidth / 24, roadWidth / 1.4f - 0.02f);
                        inEditorMagnetPoint.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/Bipolar");
                        break;
        
                }

                isSetup = true;
            }
            else
            {
                if (inEditorMagnetPoint)
                {
                    DestroyImmediate(inEditorMagnetPoint);
                }
            }
        }

        protected void OnDrawGizmos()
        {
            switch (PointType)
            {
                case PointEnd.Positive:
                {
                    Gizmos.color = Handles.color = new Color(1, 0.5f, 0.0f);
                    var offset = new Vector3(roadWidth / 3.5f, 0, 0);
                    Gizmos.DrawLine(transform.position - offset, transform.position + offset);
                    offset = new Vector3(0, 0, roadWidth / 3.5f);
                    Gizmos.DrawLine(transform.position - offset, transform.position + offset);
                    Gizmos.DrawCube(transform.position, new Vector3(0.05f, 0.05f, 0.05f));

#if UNITY_EDITOR
                    Handles.color = new Color(1, 0.5f, 0.0f);
                    Handles.DrawWireDisc(transform.position, Vector3.up, roadWidth / 3.5f);
                    Handles.DrawSolidDisc(transform.position, Vector3.up, roadWidth / 8f);
#endif
                    break;
                }
                case PointEnd.Negative:
                {
                    Gizmos.color = Color.blue;
                    var offset = new Vector3(roadWidth / 3.5f, 0, 0);
                    Gizmos.DrawLine(transform.position - offset, transform.position + offset);
                    Gizmos.DrawCube(transform.position, new Vector3(0.05f, 0.05f, 0.05f));

#if UNITY_EDITOR
                    Handles.color = Color.blue;
                    Handles.DrawWireDisc(transform.position, Vector3.up, roadWidth / 3.5f);
                    Handles.DrawSolidDisc(transform.position, Vector3.up, roadWidth / 8f);
#endif
                    break;
                }
                case PointEnd.Bipolar:
#if UNITY_EDITOR
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(transform.position, Vector3.up, roadWidth / 3f);
#endif
                    break;
                default:
                    break;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SnapPoint))]
    public class SnapPointInspector : Editor
    {
        private SnapPoint snapPoint;

        public override void OnInspectorGUI()
        {
            snapPoint = target as SnapPoint;
            DrawDefaultInspector();
            try
            {
                if (snapPoint.transform.parent.GetComponent<MagnetRoad>())
                {
                    EditorGUILayout.HelpBox("This is a standard (" + (snapPoint.PointType == SnapPoint.PointEnd.Positive ? "positive" : "negative") + ") Snap Point, it will accept road ends of the opposite polarity.", MessageType.Info);
                    EditorGUILayout.HelpBox("Do not use the Snap Points to manipulate the spline, click the road itself and make use of the yellow handles.", MessageType.Warning);
                    return;
                }
                if (snapPoint.transform.parent.parent.GetComponent<Intersection>())
                {
                    EditorGUILayout.HelpBox("This is a Bipolar Snap Point, it will accept road ends of any polarity.", MessageType.Info);
                }
            }
            catch (NullReferenceException)
            {
                EditorGUILayout.HelpBox("This Snap Point must be a child of a relevant game object to function properly!", MessageType.Error);
            }
        }
    }
#endif
}